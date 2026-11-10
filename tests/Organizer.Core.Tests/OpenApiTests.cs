using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Organizer.Core.Tests;

public class OpenApiTests
{
    private const string Url = "/openapi/v1.json";

    [Fact]
    public async Task Description_is_served_outside_Development_and_parses_as_OpenAPI()
    {
        using var core = new CoreHarness();
        using var client = core.CreateAuthenticatedClient();

        var response = await client.GetAsync(Url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = OpenApiDocument.Parse(await response.Content.ReadAsStringAsync(), "json");
        Assert.Empty(result.Diagnostic!.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-the-secret")]
    public async Task Description_rejects_a_missing_or_wrong_token(string? token)
    {
        using var core = new CoreHarness();
        using var client = core.CreateClient(token);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Url)).StatusCode);
    }

    [Fact]
    public async Task Description_needs_no_token_in_Development_and_Scalar_exists_only_there()
    {
        using var dev = new CoreHarness(environment: "Development");
        using var prod = new CoreHarness();
        using var devClient = dev.CreateClient(token: null);
        using var prodClient = prod.CreateAuthenticatedClient();

        Assert.Equal(HttpStatusCode.OK, (await devClient.GetAsync(Url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await devClient.GetAsync("/scalar/v1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await prodClient.GetAsync("/scalar/v1")).StatusCode);
    }

    [Fact]
    public async Task Every_mapped_endpoint_is_in_the_description()
    {
        using var core = new CoreHarness();
        using var client = core.CreateAuthenticatedClient();
        var paths = (await ReadDescription(client)).GetProperty("paths");

        var mapped = core.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText is { } raw && !raw.StartsWith("/openapi") && !raw.StartsWith("/scalar"))
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [])
                .Select(m => (Method: m.ToLowerInvariant(), Path: StripConstraints(e.RoutePattern.RawText!))))
            .Distinct()
            .ToList();

        Assert.NotEmpty(mapped);
        foreach (var (method, path) in mapped)
        {
            Assert.True(paths.TryGetProperty(path, out var item) && item.TryGetProperty(method, out _),
                $"{method.ToUpperInvariant()} {path} is missing from the OpenAPI description.");
        }
    }

    [Fact]
    public async Task Description_has_the_bearer_scheme_and_typed_item_responses()
    {
        using var core = new CoreHarness();
        using var client = core.CreateAuthenticatedClient();
        var doc = await ReadDescription(client);

        var scheme = doc.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
        Assert.Equal(1, doc.GetProperty("security").GetArrayLength());

        var paths = doc.GetProperty("paths");
        foreach (var (path, method, status) in new[]
                 { ("/items", "post", "201"), ("/items/{id}", "patch", "200"), ("/items/{id}/done", "post", "200") })
        {
            Assert.Equal("#/components/schemas/ItemDto", SchemaRef(paths, path, method, status));
        }
        Assert.Equal("#/components/schemas/ItemDto", ItemsRef(paths, "/inbox"));
        Assert.Equal("#/components/schemas/ProblemDetails", SchemaRef(paths, "/items/{id}", "patch", "404"));
    }

    [Fact]
    public async Task Edit_body_lists_the_editable_fields_and_query_parameters_are_described()
    {
        using var core = new CoreHarness();
        using var client = core.CreateAuthenticatedClient();
        var doc = await ReadDescription(client);

        var props = doc.GetProperty("components").GetProperty("schemas").GetProperty("EditItemRequest").GetProperty("properties");
        Assert.Equal(
            new[] { "text", "notes", "category", "tags", "dueDate", "important", "urgent" }.Order(),
            props.EnumerateObject().Select(p => p.Name).Order());
        var patch = doc.GetProperty("paths").GetProperty("/items/{id}").GetProperty("patch");
        Assert.Contains("explicit null clears", patch.GetProperty("description").GetString());

        var search = doc.GetProperty("paths").GetProperty("/search").GetProperty("get").GetProperty("parameters");
        Assert.True(search[0].GetProperty("required").GetBoolean());
        var inbox = doc.GetProperty("paths").GetProperty("/inbox").GetProperty("get").GetProperty("parameters");
        Assert.Equal(
            new[] { "category", "tag" },
            inbox.EnumerateArray().Select(p => p.GetProperty("name").GetString()!).Order());
    }

    [Fact]
    public async Task Served_description_equals_docs_openapi_json()
    {
        using var core = new CoreHarness();
        using var client = core.CreateAuthenticatedClient();
        var served = Normalize(await client.GetStringAsync(Url));
        var file = Path.Combine(RepoRoot(), "docs", "openapi.json");

        if (Environment.GetEnvironmentVariable("UPDATE_OPENAPI") == "1")
        {
            File.WriteAllText(file, served);
            return;
        }
        Assert.True(File.Exists(file) && Normalize(File.ReadAllText(file)) == served,
            "docs/openapi.json is out of date. Regenerate it: set UPDATE_OPENAPI=1 and run " +
            "`dotnet test tests/Organizer.Core.Tests --filter Served_description_equals_docs_openapi_json`, then commit it.");
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd() + "\n";

    private static string StripConstraints(string pattern) => Regex.Replace(pattern, @"\{(\w+):[^}]+\}", "{$1}");

    private static async Task<JsonElement> ReadDescription(HttpClient client) =>
        JsonDocument.Parse(await client.GetStringAsync(Url)).RootElement.Clone();

    private static string? SchemaRef(JsonElement paths, string path, string method, string status) =>
        paths.GetProperty(path).GetProperty(method).GetProperty("responses").GetProperty(status)
            .GetProperty("content").EnumerateObject().Single().Value.GetProperty("schema").GetProperty("$ref").GetString();

    private static string? ItemsRef(JsonElement paths, string path) =>
        paths.GetProperty(path).GetProperty("get").GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema")
            .GetProperty("items").GetProperty("$ref").GetString();

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Organizer.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Organizer.sln not found above the test binaries.");
    }
}
