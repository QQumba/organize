using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Organizer.Core.Tests;

public class ItemTests
{
    private static async Task<JsonElement> Capture(HttpClient c, string text)
    {
        var r = await c.PostAsJsonAsync("/api/items", new { text });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<string[]> Inbox(HttpClient c, string query = "")
    {
        var arr = await c.GetFromJsonAsync<JsonElement>("/api/inbox" + query);
        return arr.EnumerateArray().Select(e => e.GetProperty("text").GetString()!).ToArray();
    }

    private static async Task Seed(CoreHarness core, Action<OrganizerDb> seed)
    {
        _ = core.Server;
        using var scope = core.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizerDb>();
        seed(db);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Capture_then_inbox_returns_the_open_item_with_trimmed_text()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();

        var item = await Capture(c, "  buy milk  ");

        Assert.Equal("buy milk", item.GetProperty("text").GetString());
        Assert.Equal("open", item.GetProperty("state").GetString());
        Assert.Equal(["buy milk"], await Inbox(c));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Blank_text_is_rejected_with_problem_details(string? text)
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();

        var r = await c.PostAsJsonAsync("/api/items", new { text });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var body = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("title").GetString()));
        Assert.Empty(await Inbox(c));
    }

    [Fact]
    public async Task Inbox_orders_by_due_date_then_newest()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var t = DateTime.UtcNow;
        await Seed(core, db => db.Items.AddRange(
            new Item { Text = "old-nodue", CreatedAt = t.AddMinutes(-3) },
            new Item { Text = "late", DueDate = new DateOnly(2027, 1, 10), CreatedAt = t.AddMinutes(-2) },
            new Item { Text = "soon-old", DueDate = new DateOnly(2027, 1, 1), CreatedAt = t.AddMinutes(-5) },
            new Item { Text = "soon-new", DueDate = new DateOnly(2027, 1, 1), CreatedAt = t.AddMinutes(-4) },
            new Item { Text = "new-nodue", CreatedAt = t.AddMinutes(-1) }));

        Assert.Equal(["soon-new", "soon-old", "late", "new-nodue", "old-nodue"], await Inbox(c));
    }

    [Fact]
    public async Task Inbox_hides_done_and_archived_items()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        await Seed(core, db => db.Items.AddRange(
            new Item { Text = "open", CreatedAt = DateTime.UtcNow },
            new Item { Text = "done", State = ItemState.Done, CreatedAt = DateTime.UtcNow },
            new Item { Text = "archived", State = ItemState.Archived, CreatedAt = DateTime.UtcNow }));

        Assert.Equal(["open"], await Inbox(c));
    }

    [Fact]
    public async Task Inbox_filters_by_category_and_all_tags_case_insensitively()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var a = new Tag { Name = "a" };
        var b = new Tag { Name = "b" };
        var work = new Category { Name = "Work" };
        var home = new Category { Name = "Home" };
        var now = DateTime.UtcNow;
        await Seed(core, db => db.Items.AddRange(
            new Item { Text = "work-ab", Category = work, Tags = [a, b], CreatedAt = now },
            new Item { Text = "work-a", Category = work, Tags = [a], CreatedAt = now },
            new Item { Text = "home-ab", Category = home, Tags = [a, b], CreatedAt = now },
            new Item { Text = "plain", CreatedAt = now }));

        Assert.Equal(new[] { "work-a", "work-ab" }, (await Inbox(c, "?category=work")).Order());
        Assert.Equal(new[] { "home-ab", "work-a", "work-ab" }, (await Inbox(c, "?tag=A")).Order());
        Assert.Equal(new[] { "home-ab", "work-ab" }, (await Inbox(c, "?tag=a&tag=B")).Order());
        Assert.Equal(["work-ab"], await Inbox(c, "?category=WORK&tag=a&tag=b"));
        Assert.Empty(await Inbox(c, "?tag=missing"));
    }

    [Fact]
    public async Task Items_endpoints_require_the_token()
    {
        using var core = new CoreHarness();
        using var c = core.CreateClient(token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/inbox")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/items", new { text = "x" })).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    public async Task Old_unprefixed_paths_are_404_and_unknown_api_paths_are_404(bool? withToken)
    {
        using var core = new CoreHarness();
        using var c = withToken is null ? core.CreateClient(token: null) : core.CreateAuthenticatedClient();

        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/inbox")).StatusCode);
        if (withToken is true)
        {
            Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/nope")).StatusCode);
        }
    }
}
