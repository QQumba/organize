using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Organizer.Core.Tests;

/// <summary>
/// Starts the Core in-process with a temporary config file and SQLite database.
/// Nothing is pre-written: the Core creates the config itself, like on a first start.
/// The temp folder is deleted on dispose.
/// </summary>
public sealed class CoreHarness : WebApplicationFactory<Program>
{
    public string Folder { get; } = Directory.CreateTempSubdirectory("organizer-test-").FullName;

    public string ConfigPath { get; }

    public CoreHarness(string configFileName = "core.json")
    {
        ConfigPath = Path.Combine(Folder, configFileName);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Same configuration key that `--config <path>` sets on the command line.
        builder.UseSetting("config", ConfigPath);
    }

    /// <summary>Reads the config file the Core wrote.</summary>
    public JsonElement ReadConfig()
    {
        _ = Server; // make sure the Core has started
        return JsonDocument.Parse(File.ReadAllText(ConfigPath)).RootElement.Clone();
    }

    public string Secret => ReadConfig().GetProperty("Secret").GetString()!;

    public string DatabasePath => ReadConfig().GetProperty("DatabasePath").GetString()!;

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
