using System.Net;

namespace Organizer.Core.Tests;

public class WebUiTests : IDisposable
{
    private const string IndexHtml = "<html>the app</html>";
    private readonly string _root = Directory.CreateTempSubdirectory("organizer-webroot-").FullName;

    public WebUiTests()
    {
        File.WriteAllText(Path.Combine(_root, "index.html"), IndexHtml);
        File.WriteAllText(Path.Combine(_root, "robots.txt"), "plain");
        Directory.CreateDirectory(Path.Combine(_root, "_app", "immutable"));
        File.WriteAllText(Path.Combine(_root, "_app", "immutable", "app.abc.js"), "console.log(1)");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Theory]
    [InlineData("/")]
    [InlineData("/done")]
    [InlineData("/items/5")]
    public async Task Page_paths_return_index_html_without_auth(string path)
    {
        using var core = new CoreHarness(webRoot: _root);
        using var client = core.CreateClient(token: null);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(IndexHtml, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Asset_returns_itself()
    {
        using var core = new CoreHarness(webRoot: _root);
        using var client = core.CreateClient(token: null);

        var response = await client.GetAsync("/robots.txt");

        Assert.Equal("plain", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/done")]
    public async Task Head_matches_get_without_body(string path)
    {
        using var core = new CoreHarness(webRoot: _root);
        using var client = core.CreateClient(token: null);

        var get = await client.GetAsync(path);
        var head = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, path));

        Assert.Equal(get.StatusCode, head.StatusCode);
        Assert.Equal(get.Content.Headers.ContentType, head.Content.Headers.ContentType);
        Assert.Equal(get.Content.Headers.ContentLength, head.Content.Headers.ContentLength);
        Assert.Equal(get.Headers.CacheControl?.ToString(), head.Headers.CacheControl?.ToString());
        Assert.Equal(get.Headers.ETag, head.Headers.ETag);
        Assert.Equal("", await head.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_to_a_page_path_is_404()
    {
        using var core = new CoreHarness(webRoot: _root);
        using var client = core.CreateClient(token: null);

        var response = await client.PostAsync("/done", new StringContent(""));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/_app/immutable/nope.js")]
    [InlineData("/favicon.ico")]
    public async Task Missing_file_is_404_not_index_html(string path)
    {
        using var core = new CoreHarness(webRoot: _root);
        using var client = core.CreateClient(token: null);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("the app", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/nope")]
    [InlineData("/API/nope")]
    [InlineData("/api")]
    public async Task Unknown_api_path_is_never_index_html(string path)
    {
        using var core = new CoreHarness(webRoot: _root);
        using var authed = core.CreateAuthenticatedClient();
        using var anonymous = core.CreateClient(token: null);

        var withToken = await authed.GetAsync(path);
        var without = await anonymous.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, withToken.StatusCode);
        Assert.Equal("", await withToken.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, without.StatusCode);
    }

    [Theory]
    [InlineData("/", "no-cache")]
    [InlineData("/index.html", "no-cache")]
    [InlineData("/robots.txt", "no-cache")]
    [InlineData("/_app/immutable/app.abc.js", "public, max-age=31536000, immutable")]
    public async Task Cache_control_by_path(string path, string expected)
    {
        using var core = new CoreHarness(webRoot: _root);
        using var client = core.CreateClient(token: null);

        var response = await client.GetAsync(path);

        Assert.Equal(expected, response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Web_root_without_index_html_gives_404_for_pages()
    {
        File.Delete(Path.Combine(_root, "index.html"));
        using var core = new CoreHarness(webRoot: _root);
        using var client = core.CreateClient(token: null);

        var response = await client.GetAsync("/done");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Without_web_root_the_core_is_api_only()
    {
        using var core = new CoreHarness();
        using var client = core.CreateClient(token: null);
        using var authed = core.CreateAuthenticatedClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await authed.GetAsync("/api/health")).StatusCode);
    }

    [Fact]
    public async Task Development_docs_still_work_with_a_web_root()
    {
        using var core = new CoreHarness(environment: "Development", webRoot: _root);
        using var client = core.CreateClient(token: null);

        var spec = await client.GetAsync("/api/openapi/v1.json");
        var scalar = await client.GetAsync("/scalar");

        Assert.Equal(HttpStatusCode.OK, spec.StatusCode);
        Assert.NotEqual(IndexHtml, await scalar.Content.ReadAsStringAsync());
    }
}
