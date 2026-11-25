using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Organizer.Core.Tests;

/// <summary>Tags list, get one Item, Category rename, Done/Archive dates and length limits (ATL-27).</summary>
public class WebApiGapTests
{
    private static async Task<int> Capture(HttpClient c, string text) =>
        (await (await c.PostAsJsonAsync("/api/items", new { text })).Content.ReadFromJsonAsync<JsonElement>())
        .GetProperty("id").GetInt32();

    private static Task<HttpResponseMessage> Patch(HttpClient c, string url, string json) =>
        c.PatchAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

    private static async Task<int> CreateCategory(HttpClient c, string name) =>
        (await (await c.PostAsJsonAsync("/api/categories", new { name })).Content.ReadFromJsonAsync<JsonElement>())
        .GetProperty("id").GetInt32();

    private static async Task<string[]> TagList(HttpClient c) =>
        (await c.GetFromJsonAsync<string[]>("/api/tags"))!;

    private static async Task<JsonElement> GetItem(HttpClient c, int id) =>
        await c.GetFromJsonAsync<JsonElement>($"/api/items/{id}");

    // ---- Tags list

    [Fact]
    public async Task Tag_list_is_empty_without_tags()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();

        Assert.Empty(await TagList(c));
    }

    [Fact]
    public async Task Tag_list_is_sorted_Unicode_aware_and_case_insensitive()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var id = await Capture(c, "a");

        await Patch(c, $"/api/items/{id}", """{"tags":["Бета","beta","alpha"]}""");

        Assert.Equal(["alpha", "beta", "Бета"], await TagList(c));
    }

    [Fact]
    public async Task Tag_list_drops_a_tag_removed_from_its_last_item_but_keeps_done_and_archived_use()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var a = await Capture(c, "a");
        var b = await Capture(c, "b");
        var d = await Capture(c, "d");
        await Patch(c, $"/api/items/{a}", """{"tags":["typo"]}""");
        await Patch(c, $"/api/items/{b}", """{"tags":["finished"]}""");
        await Patch(c, $"/api/items/{d}", """{"tags":["old"]}""");
        await c.PostAsync($"/api/items/{b}/done", null);
        await c.PostAsync($"/api/items/{d}/archive", null);

        await Patch(c, $"/api/items/{a}", """{"tags":[]}""");

        Assert.Equal(["finished", "old"], await TagList(c));
    }

    // ---- Get one Item

    [Fact]
    public async Task Get_item_in_each_state_and_404_for_unknown()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var open = await Capture(c, "open");
        var done = await Capture(c, "done");
        var arch = await Capture(c, "arch");
        await c.PostAsync($"/api/items/{done}/done", null);
        await c.PostAsync($"/api/items/{arch}/archive", null);

        Assert.Equal("open", (await GetItem(c, open)).GetProperty("state").GetString());
        Assert.Equal("done", (await GetItem(c, done)).GetProperty("state").GetString());
        var archived = await GetItem(c, arch);
        Assert.Equal("archived", archived.GetProperty("state").GetString());
        Assert.Equal("arch", archived.GetProperty("text").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/items/999")).StatusCode);
    }

    [Fact]
    public async Task Capture_location_header_resolves()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();

        var created = await c.PostAsJsonAsync("/api/items", new { text = "x" });
        var get = await c.GetAsync(created.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    // ---- Rename a Category

    [Fact]
    public async Task Rename_changes_the_name_on_items_and_the_inbox_filter()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var cat = await CreateCategory(c, "Wrok");
        var item = await Capture(c, "a");
        await Patch(c, $"/api/items/{item}", """{"category":"Wrok"}""");

        var response = await Patch(c, $"/api/categories/{cat}", """{"name":"  Work "}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(cat, dto.GetProperty("id").GetInt32());
        Assert.Equal("Work", dto.GetProperty("name").GetString());
        Assert.Equal("Work", (await GetItem(c, item)).GetProperty("category").GetString());
        Assert.Equal(1, (await c.GetFromJsonAsync<JsonElement>("/api/inbox?category=Work")).GetArrayLength());
        Assert.Equal(0, (await c.GetFromJsonAsync<JsonElement>("/api/inbox?category=Wrok")).GetArrayLength());
    }

    [Fact]
    public async Task Rename_to_another_categorys_name_is_409_including_a_Cyrillic_case_variant()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        await CreateCategory(c, "Дім");
        var other = await CreateCategory(c, "Other");

        Assert.Equal(HttpStatusCode.Conflict, (await Patch(c, $"/api/categories/{other}", """{"name":"ДІМ"}""")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Patch(c, $"/api/categories/{other}", """{"name":"дім"}""")).StatusCode);
    }

    [Fact]
    public async Task Rename_allows_a_case_only_change_and_its_own_name()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var cat = await CreateCategory(c, "дім");

        var upper = await Patch(c, $"/api/categories/{cat}", """{"name":"Дім"}""");
        Assert.Equal(HttpStatusCode.OK, upper.StatusCode);
        Assert.Equal("Дім", (await upper.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.OK, (await Patch(c, $"/api/categories/{cat}", """{"name":"Дім"}""")).StatusCode);
    }

    [Theory]
    [InlineData("""{"name":"   "}""")]
    [InlineData("""{"name":null}""")]
    [InlineData("""{}""")]
    public async Task Rename_to_a_blank_name_is_400(string body)
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var cat = await CreateCategory(c, "Work");

        Assert.Equal(HttpStatusCode.BadRequest, (await Patch(c, $"/api/categories/{cat}", body)).StatusCode);
    }

    [Fact]
    public async Task Rename_unknown_category_is_404()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();

        Assert.Equal(HttpStatusCode.NotFound, (await Patch(c, "/api/categories/999", """{"name":"x"}""")).StatusCode);
    }

    // ---- doneAt and archivedAt

    [Fact]
    public async Task Done_and_archived_dates_follow_the_state()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var id = await Capture(c, "a");

        var created = await GetItem(c, id);
        Assert.Equal(JsonValueKind.Null, created.GetProperty("doneAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, created.GetProperty("archivedAt").ValueKind);

        var done = await (await c.PostAsync($"/api/items/{id}/done", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(done.GetProperty("doneAt").GetDateTime() > DateTime.UtcNow.AddMinutes(-1));
        Assert.Equal(JsonValueKind.Null, done.GetProperty("archivedAt").ValueKind);

        var archived = await (await c.PostAsync($"/api/items/{id}/archive", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, archived.GetProperty("doneAt").ValueKind);
        Assert.Equal(JsonValueKind.String, archived.GetProperty("archivedAt").ValueKind);

        var restored = await (await c.PostAsync($"/api/items/{id}/restore", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, restored.GetProperty("doneAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, restored.GetProperty("archivedAt").ValueKind);
    }

    // ---- Length limits

    private static string Cyr(int length) => new('ж', length);

    private static async Task AssertLimit(HttpResponseMessage atLimit, HttpResponseMessage over, string field, int limit)
    {
        Assert.True(atLimit.IsSuccessStatusCode, $"{field} at {limit} should be accepted: {atLimit.StatusCode}");
        Assert.Equal(HttpStatusCode.BadRequest, over.StatusCode);
        var detail = (await over.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString();
        Assert.Contains(field, detail);
        Assert.Contains(limit.ToString(), detail);
    }

    [Fact]
    public async Task Capture_text_limit()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();

        await AssertLimit(
            await c.PostAsJsonAsync("/api/items", new { text = Cyr(500) }),
            await c.PostAsJsonAsync("/api/items", new { text = Cyr(501) }), "Text", 500);
    }

    [Fact]
    public async Task Edit_text_notes_and_tag_name_limits()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var id = await Capture(c, "a");
        string Json(string field, string value) => JsonSerializer.Serialize(new Dictionary<string, object> { [field] = value });
        string TagJson(string value) => JsonSerializer.Serialize(new { tags = new[] { value } });

        await AssertLimit(
            await Patch(c, $"/api/items/{id}", Json("text", Cyr(500))),
            await Patch(c, $"/api/items/{id}", Json("text", Cyr(501))), "Text", 500);
        await AssertLimit(
            await Patch(c, $"/api/items/{id}", Json("notes", Cyr(10_000))),
            await Patch(c, $"/api/items/{id}", Json("notes", Cyr(10_001))), "Notes", 10_000);
        await AssertLimit(
            await Patch(c, $"/api/items/{id}", TagJson(Cyr(50))),
            await Patch(c, $"/api/items/{id}", TagJson(Cyr(51))), "Tag name", 50);
    }

    [Fact]
    public async Task Category_create_and_rename_name_limit()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();

        await AssertLimit(
            await c.PostAsJsonAsync("/api/categories", new { name = Cyr(50) }),
            await c.PostAsJsonAsync("/api/categories", new { name = Cyr(51) }), "Name", 50);

        var cat = await CreateCategory(c, "Work");
        await AssertLimit(
            await Patch(c, $"/api/categories/{cat}", JsonSerializer.Serialize(new { name = new string('я', 50) })),
            await Patch(c, $"/api/categories/{cat}", JsonSerializer.Serialize(new { name = new string('я', 51) })), "Name", 50);
    }

    [Fact]
    public async Task An_item_with_over_long_stored_text_can_still_be_edited_in_other_fields()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var id = await Capture(c, "short");
        await using (var scope = core.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrganizerDb>();
            db.Items.Single(i => i.Id == id).Text = Cyr(600);
            await db.SaveChangesAsync();
        }

        var response = await Patch(c, $"/api/items/{id}", """{"important":true}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(600, (await GetItem(c, id)).GetProperty("text").GetString()!.Length);
    }

    // ---- OpenAPI

    [Fact]
    public async Task Description_has_integer_ids_the_state_enum_and_limits()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var schemas = JsonDocument.Parse(await c.GetStringAsync("/api/openapi/v1.json")).RootElement
            .GetProperty("components").GetProperty("schemas");

        foreach (var dto in new[] { "ItemDto", "CategoryDto" })
        {
            var id = schemas.GetProperty(dto).GetProperty("properties").GetProperty("id");
            Assert.Equal("integer", id.GetProperty("type").GetString()); // a plain string, not [integer, string]
            Assert.False(id.TryGetProperty("pattern", out _));
        }
        Assert.Equal(
            ["open", "done", "archived"],
            schemas.GetProperty("ItemDto").GetProperty("properties").GetProperty("state").GetProperty("enum")
                .EnumerateArray().Select(e => e.GetString()!));
        Assert.Equal(500, schemas.GetProperty("CaptureRequest").GetProperty("properties").GetProperty("text").GetProperty("maxLength").GetInt32());
        Assert.Equal(50, schemas.GetProperty("CategoryRequest").GetProperty("properties").GetProperty("name").GetProperty("maxLength").GetInt32());
        var edit = schemas.GetProperty("EditItemRequest").GetProperty("properties");
        Assert.Equal(500, edit.GetProperty("text").GetProperty("maxLength").GetInt32());
        Assert.Equal(10_000, edit.GetProperty("notes").GetProperty("maxLength").GetInt32());
        Assert.Equal(50, edit.GetProperty("tags").GetProperty("items").GetProperty("maxLength").GetInt32());
    }
}
