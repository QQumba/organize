using Organizer.Client;
using Organizer.Contracts;

namespace Organizer.Desktop.Tests;

public class CapturePopupModelTests
{
    private static CancellationToken Ct => CancellationToken.None;

    [Fact]
    public async Task Capture_trims_clears_text_and_succeeds()
    {
        using var core = new CoreHarness();
        using var client = new OrganizerClient(core.CreateAuthenticatedClient());
        var model = new CapturePopupModel(new ClientInboxApi(client), null) { Text = "  Купити хліб " };

        Assert.True(await model.CaptureAsync(Ct));
        Assert.Equal("", model.Text);
        Assert.Null(model.Error);
        Assert.Equal(["Купити хліб"], (await client.InboxAsync()).Select(i => i.Text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_text_does_nothing(string text)
    {
        var api = new FakeApi();
        var model = new CapturePopupModel(api, null) { Text = text };

        Assert.False(await model.CaptureAsync(Ct));
        Assert.Equal(0, api.Captures);
        Assert.Null(model.Error);
    }

    [Fact]
    public async Task Failed_capture_keeps_text_and_shows_the_error()
    {
        var api = new FakeApi { Failure = new OrganizerException("Cannot reach the Core") };
        var model = new CapturePopupModel(api, null) { Text = "important" };

        Assert.False(await model.CaptureAsync(Ct));
        Assert.Equal("important", model.Text);
        Assert.Equal("Cannot reach the Core", model.Error);

        model.Dismissed(); // dismissing with unsent text keeps it
        Assert.Equal("important", model.Text);

        api.Failure = null; // the next Capture works and clears
        Assert.True(await model.CaptureAsync(Ct));
        Assert.Equal("", model.Text);
        Assert.Null(model.Error);
    }

    [Fact]
    public async Task Open_loads_the_list_in_the_Core_order_with_due_dates()
    {
        var api = new FakeApi();
        api.Items.Add(Item(1, "first", new DateOnly(2026, 11, 12)));
        api.Items.Add(Item(2, "second", null));
        var model = new CapturePopupModel(api, null);

        await model.OpenAsync(Ct);

        Assert.Equal(["first  -  2026-11-12", "second"], model.Rows.Select(r => r.Line));
    }

    [Fact]
    public async Task Completing_an_Item_removes_it_from_the_list()
    {
        using var core = new CoreHarness();
        using var client = new OrganizerClient(core.CreateAuthenticatedClient());
        var a = await client.CaptureAsync("a");
        var b = await client.CaptureAsync("b");
        var model = new CapturePopupModel(new ClientInboxApi(client), null);
        await model.OpenAsync(Ct);
        Assert.Equal(2, model.Rows.Count);

        await model.CompleteAsync(a.Id, Ct);

        Assert.Equal([b.Id], model.Rows.Select(r => r.Id));
        Assert.Equal([b.Id], (await client.InboxAsync()).Select(i => i.Id));
    }

    [Fact]
    public async Task Failed_completion_keeps_the_row_and_shows_the_error()
    {
        var api = new FakeApi();
        api.Items.Add(Item(1, "x", null));
        var model = new CapturePopupModel(api, null);
        await model.OpenAsync(Ct);
        api.Failure = new OrganizerException("boom");

        await model.CompleteAsync(1, Ct);

        Assert.Single(model.Rows);
        Assert.Equal("boom", model.Error);
    }

    [Fact]
    public async Task Open_shows_a_Core_error_instead_of_the_list()
    {
        var model = new CapturePopupModel(new FakeApi { Failure = new OrganizerException("rejected secret") }, null);

        await model.OpenAsync(Ct);

        Assert.Empty(model.Rows);
        Assert.Equal("rejected secret", model.Error);
    }

    [Fact]
    public async Task Config_error_is_shown_in_place_of_the_list_and_blocks_capture()
    {
        var model = new CapturePopupModel(null, "Config file not found") { Text = "hello" };

        await model.OpenAsync(Ct);
        Assert.Empty(model.Rows);
        Assert.Equal("Config file not found", model.Error);

        Assert.False(await model.CaptureAsync(Ct));
        Assert.Equal("hello", model.Text);
    }

    [Fact]
    public async Task Dismissed_drops_the_list()
    {
        var api = new FakeApi();
        api.Items.Add(Item(1, "x", null));
        var model = new CapturePopupModel(api, null);
        await model.OpenAsync(Ct);

        model.Dismissed();

        Assert.Empty(model.Rows);
    }

    private static ItemDto Item(int id, string text, DateOnly? due) =>
        new(id, text, null, null, [], due, false, false, "open", DateTime.UtcNow);

    private sealed class FakeApi : IInboxApi
    {
        public List<ItemDto> Items { get; } = [];
        public Exception? Failure { get; set; }
        public int Captures { get; private set; }

        public Task<ItemDto> CaptureAsync(string text, CancellationToken ct)
        {
            Captures++;
            if (Failure is not null) throw Failure;
            return Task.FromResult(Item(Captures, text, null));
        }

        public Task<IReadOnlyList<ItemDto>> InboxAsync(CancellationToken ct) =>
            Failure is null ? Task.FromResult<IReadOnlyList<ItemDto>>(Items.ToArray()) : throw Failure;

        public Task DoneAsync(int id, CancellationToken ct) =>
            Failure is null ? Task.CompletedTask : throw Failure;
    }
}
