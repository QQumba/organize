using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Organizer.Core.Tests;

public class ItemStateTests
{
    private static async Task<int> Capture(HttpClient c, string text)
    {
        var r = await c.PostAsJsonAsync("/items", new { text });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static async Task<JsonElement> Post(HttpClient c, int id, string action)
    {
        var r = await c.PostAsync($"/items/{id}/{action}", null);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<string[]> View(HttpClient c, string view)
    {
        var arr = await c.GetFromJsonAsync<JsonElement>("/" + view);
        return arr.EnumerateArray().Select(e => e.GetProperty("text").GetString()!).ToArray();
    }

    [Fact]
    public async Task Open_to_done_to_open()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var id = await Capture(c, "a");

        Assert.Equal("done", (await Post(c, id, "done")).GetProperty("state").GetString());
        Assert.Empty(await View(c, "inbox"));
        Assert.Equal(["a"], await View(c, "done"));

        Assert.Equal("open", (await Post(c, id, "restore")).GetProperty("state").GetString());
        Assert.Equal(["a"], await View(c, "inbox"));
        Assert.Empty(await View(c, "done"));
    }

    [Fact]
    public async Task Open_to_archived_to_open()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var id = await Capture(c, "a");

        Assert.Equal("archived", (await Post(c, id, "archive")).GetProperty("state").GetString());
        Assert.Empty(await View(c, "inbox"));
        Assert.Equal(["a"], await View(c, "archive"));

        await Post(c, id, "restore");
        Assert.Equal(["a"], await View(c, "inbox"));
        Assert.Empty(await View(c, "archive"));
    }

    [Fact]
    public async Task Each_view_contains_only_items_in_its_state()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var open = await Capture(c, "open");
        var done = await Capture(c, "done");
        var arch = await Capture(c, "arch");
        await Post(c, done, "done");
        await Post(c, arch, "archive");

        Assert.Equal(["open"], await View(c, "inbox"));
        Assert.Equal(["done"], await View(c, "done"));
        Assert.Equal(["arch"], await View(c, "archive"));
        Assert.NotEqual(open, done);
    }

    [Fact]
    public async Task Done_and_archive_views_are_newest_change_first()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var a = await Capture(c, "a");
        var b = await Capture(c, "b");
        var d = await Capture(c, "d");
        await Post(c, b, "done");
        await Task.Delay(20);
        await Post(c, a, "done");
        await Task.Delay(20);
        await Post(c, d, "archive");
        await Task.Delay(20);
        await Post(c, a, "archive");

        Assert.Equal(["b"], await View(c, "done"));
        Assert.Equal(["a", "d"], await View(c, "archive"));
    }

    [Fact]
    public async Task Moving_to_the_current_state_is_a_noop_and_any_state_can_move_to_any_other()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var id = await Capture(c, "a");
        await Post(c, id, "done");
        await Task.Delay(20);
        await Post(c, id, "done");
        using (var scope = core.Services.CreateScope())
        {
            var item = scope.ServiceProvider.GetRequiredService<OrganizerDb>().Items.Single();
            var first = item.DoneAt;
            await Task.Delay(20);
            await Post(c, id, "done");
            using var scope2 = core.Services.CreateScope();
            Assert.Equal(first, scope2.ServiceProvider.GetRequiredService<OrganizerDb>().Items.Single().DoneAt);
        }

        Assert.Equal("archived", (await Post(c, id, "archive")).GetProperty("state").GetString());
        Assert.Equal("done", (await Post(c, id, "done")).GetProperty("state").GetString());
        await Post(c, id, "restore");
        Assert.Equal("open", (await Post(c, id, "restore")).GetProperty("state").GetString());
    }

    [Theory]
    [InlineData("done")]
    [InlineData("archive")]
    [InlineData("restore")]
    public async Task Unknown_item_is_404(string action)
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();

        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync($"/items/999/{action}", null)).StatusCode);
    }

    [Fact]
    public async Task No_delete_route_exists()
    {
        using var core = new CoreHarness();
        using var c = core.CreateAuthenticatedClient();
        var id = await Capture(c, "a");

        var methods = core.Services.GetServices<EndpointDataSource>()
            .SelectMany(s => s.Endpoints).OfType<RouteEndpoint>()
            .SelectMany(e => e.Metadata.OfType<HttpMethodMetadata>()).SelectMany(m => m.HttpMethods);
        Assert.DoesNotContain("DELETE", methods, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await c.DeleteAsync($"/items/{id}")).StatusCode);
    }
}
