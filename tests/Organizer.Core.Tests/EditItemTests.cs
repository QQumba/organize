using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Organizer.Core.Tests;

public class EditItemTests
{
    private static async Task<JsonElement> Capture(HttpClient c, string text)
    {
        var r = await c.PostAsJsonAsync("/api/items", new { text });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<HttpResponseMessage> Patch(HttpClient c, int id, string json) =>
        await c.PatchAsync($"/api/items/{id}", new StringContent(json, Encoding.UTF8, "application/json"));

    private static async Task<JsonElement> PatchOk(HttpClient c, int id, string json)
    {
        var r = await Patch(c, id, json);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task Category(HttpClient c, string name) =>
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/categories", new { name })).StatusCode);

    private static async Task<JsonElement[]> InboxItems(HttpClient c, string query = "") =>
        (await c.GetFromJsonAsync<JsonElement>("/api/inbox" + query)).EnumerateArray().ToArray();

    private static string[] Tags(JsonElement item) =>
        item.GetProperty("tags").EnumerateArray().Select(t => t.GetString()!).ToArray();

    [Fact]
    public async Task New_item_has_flags_unset_and_no_optional_fields()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();

        var item = await Capture(c, "x");

        Assert.False(item.GetProperty("important").GetBoolean());
        Assert.False(item.GetProperty("urgent").GetBoolean());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("dueDate").ValueKind);
        Assert.Equal(JsonValueKind.Null, item.GetProperty("category").ValueKind);
        Assert.Empty(Tags(item));
    }

    [Fact]
    public async Task Each_field_can_be_set_changed_and_cleared()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        await Category(c, "Work");
        await Category(c, "Home");
        var id = (await Capture(c, "task")).GetProperty("id").GetInt32();

        var set = await PatchOk(c, id,
            """{"text":" new text ","notes":"n","category":"Work","tags":["b","a"],"dueDate":"2027-03-04","important":true,"urgent":true}""");
        Assert.Equal("new text", set.GetProperty("text").GetString());
        Assert.Equal("n", set.GetProperty("notes").GetString());
        Assert.Equal("Work", set.GetProperty("category").GetString());
        Assert.Equal(["a", "b"], Tags(set));
        Assert.Equal("2027-03-04", set.GetProperty("dueDate").GetString());
        Assert.True(set.GetProperty("important").GetBoolean());
        Assert.True(set.GetProperty("urgent").GetBoolean());

        // Omitted fields are unchanged.
        var same = await PatchOk(c, id, """{"notes":"n2"}""");
        Assert.Equal("n2", same.GetProperty("notes").GetString());
        Assert.Equal("Work", same.GetProperty("category").GetString());
        Assert.Equal(["a", "b"], Tags(same));
        Assert.True(same.GetProperty("important").GetBoolean());

        // Change: Category (case-insensitive), Tags replaced, flag unset, due date moved.
        var changed = await PatchOk(c, id,
            """{"category":"home","tags":["A","c"],"urgent":false,"dueDate":"2027-05-06"}""");
        Assert.Equal("Home", changed.GetProperty("category").GetString());
        Assert.Equal(["a", "c"], Tags(changed));
        Assert.False(changed.GetProperty("urgent").GetBoolean());
        Assert.Equal("2027-05-06", changed.GetProperty("dueDate").GetString());

        // Clear with explicit null / empty list.
        var cleared = await PatchOk(c, id,
            """{"notes":null,"category":null,"tags":[],"dueDate":null,"important":null}""");
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("notes").ValueKind);
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("category").ValueKind);
        Assert.Empty(Tags(cleared));
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("dueDate").ValueKind);
        Assert.False(cleared.GetProperty("important").GetBoolean());

        // The Inbox shows the stored result.
        var stored = Assert.Single(await InboxItems(c));
        Assert.Equal("new text", stored.GetProperty("text").GetString());
    }

    [Fact]
    public async Task Flags_are_independent_of_the_due_date()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var id = (await Capture(c, "t")).GetProperty("id").GetInt32();

        var dated = await PatchOk(c, id, """{"dueDate":"2020-01-01"}""");
        Assert.False(dated.GetProperty("important").GetBoolean());
        Assert.False(dated.GetProperty("urgent").GetBoolean());

        var flagged = await PatchOk(c, id, """{"important":true,"dueDate":null}""");
        Assert.True(flagged.GetProperty("important").GetBoolean());
        Assert.False(flagged.GetProperty("urgent").GetBoolean());
    }

    [Fact]
    public async Task Inbox_filter_sees_edited_category_and_tags()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        await Category(c, "Work");
        var one = (await Capture(c, "one")).GetProperty("id").GetInt32();
        var two = (await Capture(c, "two")).GetProperty("id").GetInt32();
        await Capture(c, "three");
        await PatchOk(c, one, """{"category":"Work","tags":["x","y"]}""");
        await PatchOk(c, two, """{"category":"Work","tags":["x"]}""");

        string[] Texts(JsonElement[] items) => items.Select(i => i.GetProperty("text").GetString()!).Order().ToArray();

        Assert.Equal(["one", "two"], Texts(await InboxItems(c, "?category=work")));
        Assert.Equal(["one"], Texts(await InboxItems(c, "?tag=x&tag=Y")));
        Assert.Equal(["one", "two"], Texts(await InboxItems(c, "?tag=X")));

        await PatchOk(c, one, """{"category":null,"tags":["z"]}""");
        Assert.Equal(["two"], Texts(await InboxItems(c, "?category=Work")));
        Assert.Equal(["one"], Texts(await InboxItems(c, "?tag=z")));
        Assert.Equal(["two"], Texts(await InboxItems(c, "?tag=x")));
    }

    [Fact]
    public async Task Categories_can_be_created_and_listed()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        await Category(c, "Work");
        await Category(c, "Home");

        var list = await c.GetFromJsonAsync<JsonElement>("/api/categories");
        Assert.Equal(["Home", "Work"], list.EnumerateArray().Select(e => e.GetProperty("name").GetString()!).ToArray());

        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/categories", new { name = "work" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/categories", new { name = "  " })).StatusCode);
    }

    [Fact]
    public async Task Unknown_item_returns_404_problem_details()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();

        var r = await Patch(c, 999, """{"notes":"x"}""");

        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        var body = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Contains("999", body.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("""{"category":"Nope"}""")]
    [InlineData("""{"text":null}""")]
    [InlineData("""{"text":"  "}""")]
    [InlineData("""{"dueDate":"not a date"}""")]
    [InlineData("""{"tags":"a"}""")]
    [InlineData("""{"important":"yes"}""")]
    [InlineData("""{"state":"done"}""")]
    public async Task Invalid_edits_return_400_and_change_nothing(string json)
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var id = (await Capture(c, "keep")).GetProperty("id").GetInt32();

        var r = await Patch(c, id, json);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var item = Assert.Single(await InboxItems(c));
        Assert.Equal("keep", item.GetProperty("text").GetString());
    }

    [Fact]
    public async Task Edit_requires_the_token()
    {
        using var core = new CoreHarness();
        using var c = core.CreateClient(token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Patch(c, 1, "{}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/categories")).StatusCode);
    }
}
