using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Organizer.Core.Tests;

public class SessionTests
{
    private static HttpClient NoAuth(CoreHarness core) => core.CreateClient(token: null);

    private static Task<HttpResponseMessage> Login(HttpClient c, string? secret) =>
        c.PostAsJsonAsync("/api/session", new { secret });

    private static string CookieHeader(HttpResponseMessage r) =>
        r.Headers.GetValues("Set-Cookie").Single();

    private static async Task<int> Capture(HttpClient c, string text)
    {
        var r = await c.PostAsJsonAsync("/api/items", new { text });
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static HttpRequestMessage Done(int id, string? origin)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/items/{id}/done");
        if (origin is not null) req.Headers.TryAddWithoutValidation("Origin", origin);
        return req;
    }

    [Fact]
    public async Task Login_then_cookie_alone_works_and_logout_ends_it()
    {
        using var core = new CoreHarness();
        using var c = NoAuth(core);

        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/inbox")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Login(c, core.Secret)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/inbox")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.GetAsync("/api/session")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync("/api/session")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/inbox")).StatusCode);
    }

    [Fact]
    public async Task Wrong_blank_or_malformed_login_sets_no_cookie()
    {
        using var core = new CoreHarness();
        using var c = NoAuth(core);

        var wrong = await Login(c, "nope");
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.False(wrong.Headers.Contains("Set-Cookie"));
        Assert.Equal(HttpStatusCode.BadRequest, (await Login(c, "  ")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Login(c, null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/api/session",
            new StringContent("{", Encoding.UTF8, "application/json"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/api/session",
            new StringContent("", Encoding.UTF8, "application/json"))).StatusCode);
    }

    [Fact]
    public async Task Tampered_cookie_and_bearer()
    {
        using var core = new CoreHarness();
        using var c = NoAuth(core);
        var name = CookieHeader(await Login(c, core.Secret)).Split('=')[0];

        foreach (var value in new[] { "AAAA", "not base64 !", "" })
        {
            using var raw = core.CreateClient(token: null);
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/inbox");
            req.Headers.Add("Cookie", $"{name}={value}");
            Assert.Equal(HttpStatusCode.Unauthorized, (await raw.SendAsync(req)).StatusCode);
        }
        using var bearer = core.CreateAuthenticatedClient();
        Assert.Equal(HttpStatusCode.OK, (await bearer.GetAsync("/api/inbox")).StatusCode);
    }

    [Fact]
    public async Task Cookie_made_with_an_old_secret_fails_after_the_secret_changes()
    {
        using var first = new CoreHarness();
        using var second = new CoreHarness();
        second.WriteConfig("{\"Secret\":\"a-different-secret\"}");
        using var c = NoAuth(first);
        var pair = CookieHeader(await Login(c, first.Secret)).Split(';')[0];
        Assert.Equal(first.Effective.Port, second.Effective.Port);

        using var other = NoAuth(second);
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/inbox");
        req.Headers.Add("Cookie", pair);

        Assert.Equal(HttpStatusCode.Unauthorized, (await other.SendAsync(req)).StatusCode);
    }

    [Fact]
    public async Task Cookie_attributes_over_http()
    {
        using var core = new CoreHarness();
        using var c = NoAuth(core);

        var header = CookieHeader(await Login(c, core.Secret));

        Assert.StartsWith($"organizer_session_{core.Effective.Port}=", header);
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("max-age=31536000", header, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secure", header.Replace("samesite", ""), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cookie_name_follows_a_configured_port()
    {
        using var core = new CoreHarness();
        core.WriteConfig("{\"Secret\":\"s\",\"Port\":51234}");
        using var c = NoAuth(core);

        Assert.StartsWith("organizer_session_51234=", CookieHeader(await Login(c, "s")));
    }

    [Fact]
    public async Task Only_login_and_logout_are_open()
    {
        using var core = new CoreHarness();
        using var c = NoAuth(core);

        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/session")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/health")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync("/api/session")).StatusCode);
        using var bearer = core.CreateAuthenticatedClient();
        Assert.Equal(HttpStatusCode.NoContent, (await bearer.GetAsync("/api/session")).StatusCode);
    }

    [Theory]
    [InlineData("http://127.0.0.1:9999", false)]
    [InlineData("null", false)]
    [InlineData("not a url", false)]
    [InlineData("http://localhost", true)]
    [InlineData("HTTP://LOCALHOST:80", true)]
    [InlineData(null, true)]
    public async Task Cookie_writes_are_checked_against_Origin(string? origin, bool allowed)
    {
        using var core = new CoreHarness(); // test server Host is localhost, port omitted
        using var bearer = core.CreateAuthenticatedClient();
        var id = await Capture(bearer, "a");
        using var c = NoAuth(core);
        await Login(c, core.Secret);

        var response = await c.SendAsync(Done(id, origin));

        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, response.StatusCode);
        var done = await bearer.GetFromJsonAsync<JsonElement>("/api/done");
        Assert.Equal(allowed ? 1 : 0, done.GetArrayLength());
    }

    [Fact]
    public async Task Bearer_ignores_Origin_and_cookie_reads_ignore_it_too()
    {
        using var core = new CoreHarness();
        using var bearer = core.CreateAuthenticatedClient();
        var id = await Capture(bearer, "a");
        using var c = NoAuth(core);
        await Login(c, core.Secret);

        var get = new HttpRequestMessage(HttpMethod.Get, "/api/inbox");
        get.Headers.Add("Origin", "http://evil.example");
        Assert.Equal(HttpStatusCode.OK, (await c.SendAsync(get)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await bearer.SendAsync(Done(id, "http://evil.example"))).StatusCode);
    }
}
