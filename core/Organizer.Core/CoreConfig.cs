using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace Organizer.Core;

/// <summary>Contents of core.json.</summary>
public sealed class CoreConfig
{
    public string Address { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 47821;
    public string Secret { get; set; } = "";

    /// <summary>SQLite file. Empty means next to the config file.</summary>
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

    /// <summary>Loads the config, creating it with defaults and a random secret if missing.</summary>
    public static (CoreConfig Config, bool Created) LoadOrCreate(string path)
    {
        var folder = Path.GetDirectoryName(path)!;
        var created = false;
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(folder);
            var fresh = new CoreConfig
            {
                Secret = NewSecret(),
                DatabasePath = Path.Combine(folder, "organizer.db"),
            };
            File.WriteAllText(path, JsonSerializer.Serialize(fresh, Json));
            created = true;
        }

        var config = JsonSerializer.Deserialize<CoreConfig>(File.ReadAllText(path))
            ?? throw new InvalidOperationException($"Config file '{path}' is empty.");
        if (string.IsNullOrWhiteSpace(config.Secret))
        {
            throw new InvalidOperationException(
                $"Config file '{path}' has no Secret. Set one, or delete the file to have it regenerated.");
        }
        if (string.IsNullOrWhiteSpace(config.DatabasePath))
        {
            config.DatabasePath = Path.Combine(folder, "organizer.db");
        }
        return (config, created);
    }

    /// <summary>32 random bytes, URL-safe base64.</summary>
    public static string NewSecret() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
}
