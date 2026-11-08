using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Organizer.Core.Tests;

public class HealthTests
{
    [Fact]
    public async Task Health_with_valid_token_is_ok()
    {
        using var core = new CoreHarness();
        using var client = core.CreateAuthenticatedClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Health_with_wrong_token_is_401()
    {
        using var core = new CoreHarness();
        using var client = core.CreateClient("not-the-secret");

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Health_with_no_token_is_401()
    {
        using var core = new CoreHarness();
        using var client = core.CreateClient(token: null);

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Secret_is_never_returned()
    {
        using var core = new CoreHarness();
        using var client = core.CreateAuthenticatedClient();

        var body = await client.GetStringAsync("/health");

        Assert.DoesNotContain(core.Secret, body);
    }
}

public class ConfigTests
{
    [Fact]
    public async Task Missing_config_is_created_with_random_secret_and_core_starts()
    {
        using var core = new CoreHarness();
        Assert.False(File.Exists(core.ConfigPath));

        using var client = core.CreateAuthenticatedClient(); // starts the Core

        Assert.True(File.Exists(core.ConfigPath));
        var config = core.ReadConfig();
        Assert.True(config.GetProperty("Secret").GetString()!.Length >= 43); // 32 bytes, base64url
        Assert.Equal("127.0.0.1", config.GetProperty("Address").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public void Each_created_config_gets_a_different_secret()
    {
        using var a = new CoreHarness();
        using var b = new CoreHarness();

        Assert.NotEqual(a.Secret, b.Secret);
    }

    [Fact]
    public async Task Config_override_is_honoured()
    {
        using var core = new CoreHarness("custom-name.json");

        using var client = core.CreateAuthenticatedClient();
        await client.GetAsync("/health");

        Assert.True(File.Exists(Path.Combine(core.Folder, "custom-name.json")));
        // Default database path is next to the config file.
        Assert.Equal(core.Folder, Path.GetDirectoryName(core.DatabasePath));
    }

    [Fact]
    public void Config_path_argument_wins_over_default()
    {
        Assert.Equal(Path.GetFullPath("x.json"), CoreConfigLoader.ResolvePath("x.json"));
        var def = CoreConfigLoader.ResolvePath(null);
        Assert.EndsWith(Path.Combine("Organizer", "core.json"), def);
    }
}

public class DatabaseTests
{
    [Fact]
    public async Task Each_harness_has_an_isolated_database_created_by_migrations()
    {
        using var a = new CoreHarness();
        using var b = new CoreHarness();
        await a.CreateAuthenticatedClient().GetAsync("/health");
        await b.CreateAuthenticatedClient().GetAsync("/health");

        Assert.NotEqual(a.DatabasePath, b.DatabasePath);
        Assert.True(File.Exists(a.DatabasePath));
        Assert.True(File.Exists(b.DatabasePath));
        Assert.Equal(1, MigrationCount(a.DatabasePath));
        Assert.Equal(1, MigrationCount(b.DatabasePath));
    }

    [Fact]
    public async Task Temp_folder_is_deleted_after_run()
    {
        string folder;
        using (var core = new CoreHarness())
        {
            await core.CreateAuthenticatedClient().GetAsync("/health");
            folder = core.Folder;
            Assert.True(Directory.Exists(folder));
        }
        Assert.False(Directory.Exists(folder));
    }

    private static long MigrationCount(string dbPath)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory";
        return (long)cmd.ExecuteScalar()!;
    }
}
