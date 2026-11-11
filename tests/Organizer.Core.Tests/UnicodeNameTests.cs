using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Organizer.Core.Tests;

public class UnicodeNameTests
{
    private static async Task<HttpResponseMessage> PostCategory(HttpClient c, string name) =>
        await c.PostAsJsonAsync("/api/categories", new { name });

    private static async Task<int> Capture(HttpClient c, string text) =>
        (await (await c.PostAsJsonAsync("/api/items", new { text })).Content.ReadFromJsonAsync<JsonElement>())
        .GetProperty("id").GetInt32();

    private static async Task<JsonElement> Patch(HttpClient c, int id, string json)
    {
        var r = await c.PatchAsync($"/api/items/{id}", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static string[] Tags(JsonElement item) =>
        item.GetProperty("tags").EnumerateArray().Select(t => t.GetString()!).ToArray();

    private static async Task<JsonElement[]> Inbox(HttpClient c, string query) =>
        (await c.GetFromJsonAsync<JsonElement>("/api/inbox" + query)).EnumerateArray().ToArray();

    [Fact]
    public async Task Duplicate_Cyrillic_category_in_another_case_is_409()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();

        Assert.Equal(HttpStatusCode.Created, (await PostCategory(c, "Дім")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await PostCategory(c, "дім")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await PostCategory(c, "ДІМ")).StatusCode);
    }

    [Fact]
    public async Task Canonically_equivalent_forms_are_the_same_name()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();

        Assert.Equal(HttpStatusCode.Created, (await PostCategory(c, "Йога")).StatusCode); // Й, one character
        // "й" as "и" plus combining breve.
        Assert.Equal(HttpStatusCode.Conflict, (await PostCategory(c, "йога")).StatusCode);
    }

    [Fact]
    public async Task Cyrillic_tags_in_different_cases_resolve_to_one_tag()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var a = await Capture(c, "a");
        var b = await Capture(c, "b");

        // One request: the first spelling wins.
        var first = await Patch(c, a, """{"tags":["Робота","робота"]}""");
        Assert.Equal(["Робота"], Tags(first));

        // Second request: the existing Tag keeps its stored spelling and is reused.
        var second = await Patch(c, b, """{"tags":["РОБОТА"]}""");
        Assert.Equal(["Робота"], Tags(second));

        var inbox = await Inbox(c, "?tag=" + Uri.EscapeDataString("робота"));
        Assert.Equal(2, inbox.Length);
    }

    [Fact]
    public async Task Categories_sort_in_Ukrainian_order_regardless_of_case()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        string[] names = ["Їх", "ґх", "Жх", "дх", "Йх", "єх", "Гх", "іх", "Их", "Ех"];
        foreach (var n in names)
        {
            Assert.Equal(HttpStatusCode.Created, (await PostCategory(c, n)).StatusCode);
        }

        var listed = (await c.GetFromJsonAsync<JsonElement>("/api/categories")).EnumerateArray()
            .Select(e => e.GetProperty("name").GetString()!).ToArray();

        // г < ґ < д, е < є < ж, и < і < ї < й
        Assert.Equal(["Гх", "ґх", "дх", "Ех", "єх", "Жх", "Их", "іх", "Їх", "Йх"], listed);
    }

    [Fact]
    public async Task Item_tags_are_sorted_in_Ukrainian_order()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var id = await Capture(c, "x");

        var item = await Patch(c, id, """{"tags":["їх","Ґх","дх","Гх","Іх"]}""");

        Assert.Equal(["Гх", "Ґх", "дх", "Іх", "їх"], Tags(item));
    }

    [Fact]
    public async Task Inbox_filters_match_Cyrillic_names_in_another_case()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        await PostCategory(c, "Дім");
        var id = await Capture(c, "полагодити");
        await Capture(c, "other");
        await Patch(c, id, """{"category":"дім","tags":["Робота"]}""");

        Assert.Single(await Inbox(c, "?category=" + Uri.EscapeDataString("ДІМ")));
        Assert.Single(await Inbox(c, "?tag=" + Uri.EscapeDataString("робота")));
        Assert.Empty(await Inbox(c, "?category=" + Uri.EscapeDataString("Гараж")));
        Assert.Empty(await Inbox(c, "?tag=" + Uri.EscapeDataString("Невідомий")));
    }

    [Fact]
    public async Task Patch_category_finds_existing_Cyrillic_category_in_another_case()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        await PostCategory(c, "Дім");
        var id = await Capture(c, "x");

        var item = await Patch(c, id, """{"category":"дім"}""");

        Assert.Equal("Дім", item.GetProperty("category").GetString());
    }
}
