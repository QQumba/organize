using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Organizer.Core.Tests;

public class SearchTests
{
    private static async Task<int> Capture(HttpClient c, string text, string? notes = null)
    {
        var r = await c.PostAsJsonAsync("/api/items", new { text });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var id = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        if (notes is not null)
        {
            Assert.Equal(HttpStatusCode.OK, (await c.PatchAsJsonAsync($"/api/items/{id}", new { notes })).StatusCode);
        }
        return id;
    }

    private static async Task<(string Text, string State)[]> Search(HttpClient c, string q)
    {
        var arr = await c.GetFromJsonAsync<JsonElement>("/api/search?q=" + Uri.EscapeDataString(q));
        return arr.EnumerateArray()
            .Select(e => (e.GetProperty("text").GetString()!, e.GetProperty("state").GetString()!)).ToArray();
    }

    [Fact]
    public async Task Matches_text_and_notes_and_misses_otherwise()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        await Capture(c, "buy milk");
        await Capture(c, "call bank", "ask about the mortgage");
        await Capture(c, "other");

        Assert.Equal(["buy milk"], (await Search(c, "milk")).Select(r => r.Text));
        Assert.Equal(["call bank"], (await Search(c, "mortgage")).Select(r => r.Text));
        Assert.Equal(["call bank"], (await Search(c, "ortga")).Select(r => r.Text));
        Assert.Empty(await Search(c, "zebra"));
    }

    [Fact]
    public async Task Results_include_done_and_archived_with_state()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        await Capture(c, "task open");
        var done = await Capture(c, "task done");
        var arch = await Capture(c, "task arch");
        await c.PostAsync($"/api/items/{done}/done", null);
        await c.PostAsync($"/api/items/{arch}/archive", null);

        var results = await Search(c, "task");
        Assert.Equal(3, results.Length);
        Assert.Contains(("task open", "open"), results);
        Assert.Contains(("task done", "done"), results);
        Assert.Contains(("task arch", "archived"), results);
    }

    [Fact]
    public async Task Is_case_insensitive()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        await Capture(c, "Buy MILK", "Remember The Receipt");

        Assert.Single(await Search(c, "milk"));
        Assert.Single(await Search(c, "BUY"));
        Assert.Single(await Search(c, "the receipt"));
    }

    [Fact]
    public async Task Is_case_insensitive_for_cyrillic()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        await Capture(c, "Купити МОЛОКО", "Не забути Чек");
        await Capture(c, "інше");

        Assert.Equal(["Купити МОЛОКО"], (await Search(c, "молоко")).Select(r => r.Text));
        Assert.Equal(["Купити МОЛОКО"], (await Search(c, "КУПИТИ")).Select(r => r.Text));
        Assert.Single(await Search(c, "забути чек"));
        Assert.Single(await Search(c, "ЧЕК"));
        Assert.Empty(await Search(c, "хліб"));
    }

    [Fact]
    public async Task Wildcard_characters_are_literal()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        await Capture(c, "100% done");
        await Capture(c, "snake_case");
        await Capture(c, "plain");

        Assert.Equal(["100% done"], (await Search(c, "%")).Select(r => r.Text));
        Assert.Equal(["snake_case"], (await Search(c, "_")).Select(r => r.Text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_query_is_400(string q)
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();

        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/search?q=" + q)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/search")).StatusCode);
    }
}
