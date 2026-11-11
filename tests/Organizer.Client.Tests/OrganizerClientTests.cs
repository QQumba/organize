using System.Net.Http.Json;

namespace Organizer.Client.Tests;

public class OrganizerClientTests
{
    [Fact]
    public async Task Capture_inbox_done_roundtrip_against_a_real_Core()
    {
        using var core = new CoreHarness();
        using var client = new OrganizerClient(core.CreateAuthenticatedClient());

        var a = await client.CaptureAsync("  Купити хліб ");
        var b = await client.CaptureAsync("call mum");
        Assert.Equal("Купити хліб", a.Text);
        Assert.Equal("open", a.State);

        var inbox = await client.InboxAsync();
        Assert.Equal(new[] { b.Id, a.Id }, inbox.Select(i => i.Id));

        var done = await client.DoneAsync(a.Id);
        Assert.Equal("done", done.State);
        Assert.Equal(new[] { b.Id }, (await client.InboxAsync()).Select(i => i.Id));
    }

    [Fact]
    public async Task Inbox_filters_by_category_and_tags()
    {
        using var core = new CoreHarness();
        using var http = core.CreateAuthenticatedClient();
        using var client = new OrganizerClient(http);
        var item = await client.CaptureAsync("tagged");
        await client.CaptureAsync("plain");
        (await http.PostAsJsonAsync("/api/categories", new { name = "Work" })).EnsureSuccessStatusCode();
        (await http.PatchAsJsonAsync($"/api/items/{item.Id}", new { category = "Work", tags = new[] { "a", "b" } })).EnsureSuccessStatusCode();

        Assert.Equal(new[] { item.Id }, (await client.InboxAsync("Work", ["a", "b"])).Select(i => i.Id));
        Assert.Empty(await client.InboxAsync("Work", ["missing"]));
    }

    [Fact]
    public async Task Inbox_filters_by_Ukrainian_category_and_tag_in_another_case()
    {
        using var core = new CoreHarness();
        using var http = core.CreateAuthenticatedClient();
        using var client = new OrganizerClient(http);
        var item = await client.CaptureAsync("полагодити");
        await client.CaptureAsync("інше");
        (await http.PostAsJsonAsync("/api/categories", new { name = "Дім" })).EnsureSuccessStatusCode();
        (await http.PatchAsJsonAsync($"/api/items/{item.Id}", new { category = "Дім", tags = new[] { "Робота" } })).EnsureSuccessStatusCode();

        Assert.Equal(new[] { item.Id }, (await client.InboxAsync("ДІМ")).Select(i => i.Id));
        Assert.Equal(new[] { item.Id }, (await client.InboxAsync("дім")).Select(i => i.Id));
        Assert.Equal(new[] { item.Id }, (await client.InboxAsync(tags: ["РОБОТА"])).Select(i => i.Id));
        Assert.Equal(new[] { item.Id }, (await client.InboxAsync("дім", ["робота"])).Select(i => i.Id));
    }

    [Fact]
    public async Task Wrong_secret_gives_a_clear_error()
    {
        using var core = new CoreHarness();
        using var client = new OrganizerClient(core.CreateClient("wrong"));

        var e = await Assert.ThrowsAsync<OrganizerException>(() => client.InboxAsync());
        Assert.Contains("rejected the secret", e.Message);
    }

    [Fact]
    public async Task Unknown_item_and_blank_text_surface_the_Core_message()
    {
        using var core = new CoreHarness();
        using var client = new OrganizerClient(core.CreateAuthenticatedClient());

        var notFound = await Assert.ThrowsAsync<OrganizerException>(() => client.DoneAsync(12345));
        Assert.Contains("12345", notFound.Message);
        await Assert.ThrowsAsync<OrganizerException>(() => client.CaptureAsync("   "));
    }

    [Fact]
    public async Task Unreachable_Core_gives_a_clear_error()
    {
        using var client = new OrganizerClient(new ClientConfig("http://127.0.0.1:1", "x"));

        var e = await Assert.ThrowsAsync<OrganizerException>(() => client.InboxAsync());
        Assert.Contains("Cannot reach the Core", e.Message);
    }

    [Fact]
    public void Config_loads_and_reports_problems()
    {
        var dir = Directory.CreateTempSubdirectory("organizer-cfg-").FullName;
        try
        {
            var path = Path.Combine(dir, "cli.json");
            Assert.Contains("not found", Assert.Throws<OrganizerException>(() => ClientConfig.Load(path, "cli.json")).Message);

            File.WriteAllText(path, "{ not json");
            Assert.Contains("not valid JSON", Assert.Throws<OrganizerException>(() => ClientConfig.Load(path, "cli.json")).Message);

            File.WriteAllText(path, """{ "Address": "http://localhost:1" }""");
            Assert.Contains("\"Secret\"", Assert.Throws<OrganizerException>(() => ClientConfig.Load(path, "cli.json")).Message);

            File.WriteAllText(path, """{ "Address": "localhost", "Secret": "s" }""");
            Assert.Contains("http(s) URL", Assert.Throws<OrganizerException>(() => ClientConfig.Load(path, "cli.json")).Message);

            File.WriteAllText(path, """{ "Address": "http://localhost:5000", "Secret": "s" }""");
            Assert.Equal(new ClientConfig("http://localhost:5000", "s"), ClientConfig.Load(path, "cli.json"));
        }
        finally { Directory.Delete(dir, true); }
    }
}
