using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace Organizer.Core;

/// <summary>
/// Effective settings. Defaults come from appsettings.json (ships with the app);
/// core.json in the user's app-data folder overrides them.
/// </summary>
public sealed class CoreConfig
{
    public string Address { get; set; } = "";
    public int Port { get; set; }
    public string Secret { get; set; } = "";

    /// <summary>SQLite file. Empty means organizer.db next to core.json.</summary>
    public string DatabasePath { get; set; } = "";
}

public static class CoreConfigLoader
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    /// <summary>The --config value if given, otherwise ApplicationData/Organizer/core.json.</summary>
    public static string ResolvePath(string? configArg)
    {
        if (!string.IsNullOrWhiteSpace(configArg))
        {
            return Path.GetFullPath(configArg);
        }
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Organizer", "core.json");
    }

    /// <summary>
    /// Creates core.json containing only a random secret if it does not exist.
    /// Everything else falls back to the defaults in appsettings.json.
    /// </summary>
    public static bool EnsureFile(string path)
    {
        if (File.Exists(path))
        {
            return false;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new { Secret = NewSecret() }, Json));
        return true;
    }

    /// <summary>Validates the merged settings and fills in the database path default.</summary>
    public static CoreConfig Finalize(CoreConfig config, string configPath)
    {
        if (string.IsNullOrWhiteSpace(config.Secret))
        {
            throw new InvalidOperationException(
                $"No Secret configured. Set one in '{configPath}', or delete the file to have it regenerated.");
        }
        if (string.IsNullOrWhiteSpace(config.DatabasePath))
        {
            config.DatabasePath = Path.Combine(Path.GetDirectoryName(configPath)!, "organizer.db");
        }
        return config;
    }

    /// <summary>32 random bytes, URL-safe base64.</summary>
    public static string NewSecret() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
}
