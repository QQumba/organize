using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Organizer.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Organizer.TestSupport;

/// <summary>
/// Starts the Core in-process with a temporary config file and SQLite database.
/// Nothing is pre-written: the Core creates the config itself, like on a first start.
/// The temp folder is deleted on dispose.
/// </summary>
public sealed class CoreHarness : WebApplicationFactory<Program>
{
    public string Folder { get; } = Directory.CreateTempSubdirectory("organizer-test-").FullName;

    public string ConfigPath { get; }

    private readonly string _environment;

    /// <param name="environment">Hosting environment; the test host's own default is Development.</param>
    public CoreHarness(string configFileName = "core.json", string environment = "Production")
    {
        ConfigPath = Path.Combine(Folder, configFileName);
        _environment = environment;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Same configuration key that `--config <path>` sets on the command line.
        builder.UseSetting("config", ConfigPath);
        builder.UseEnvironment(_environment);
    }

    /// <summary>Pre-writes core.json (user overrides) before the Core starts.</summary>
    public void WriteConfig(string json) => File.WriteAllText(ConfigPath, json);

    /// <summary>Reads the core.json the Core wrote (user overrides only).</summary>
    public JsonElement ReadConfigFile()
    {
        _ = Server; // make sure the Core has started
        return JsonDocument.Parse(File.ReadAllText(ConfigPath)).RootElement.Clone();
    }

    /// <summary>The merged settings the running Core uses.</summary>
    public CoreConfig Effective => Services.GetRequiredService<CoreConfig>();

    public string Secret => Effective.Secret;

    public string DatabasePath => Effective.DatabasePath;

    public HttpClient CreateClient(string? token)
    {
        var client = CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return client;
    }

    public HttpClient CreateAuthenticatedClient() => CreateClient(Secret);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            SqliteConnection.ClearAllPools(); // release the file lock on Windows
            try { Directory.Delete(Folder, recursive: true); } catch (IOException) { }
        }
    }
}
