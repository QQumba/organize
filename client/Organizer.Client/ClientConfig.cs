using System.Text.Json;

namespace Organizer.Client;

/// <summary>Address and secret of the Core. Each Client has its own config file.</summary>
public sealed record ClientConfig(string? Address, string? Secret)
{
    /// <summary>Loads and validates a config file. Never creates it.</summary>
    /// <param name="path">The file to read.</param>
    /// <param name="fileHint">Name of the file, used in the "create it" message (for example cli.json).</param>
    public static ClientConfig Load(string path, string fileHint)
    {
        if (!File.Exists(path))
        {
            throw new OrganizerException(
                $"Config file not found: {path}\n" +
                $"Create it ({fileHint}) with the Core address and secret, for example:\n" +
                "{ \"Address\": \"http://127.0.0.1:47821\", \"Secret\": \"<secret from the Core's core.json>\" }");
        }

        ClientConfig? config;
        try
        {
            config = JsonSerializer.Deserialize(File.ReadAllText(path), ClientJsonContext.Default.ClientConfig);
        }
        catch (JsonException e)
        {
            throw new OrganizerException($"Config file {path} is not valid JSON: {e.Message}", e);
        }
        catch (IOException e)
        {
            throw new OrganizerException($"Cannot read config file {path}: {e.Message}", e);
        }

        if (config is null || string.IsNullOrWhiteSpace(config.Address) || string.IsNullOrWhiteSpace(config.Secret))
        {
            throw new OrganizerException($"Config file {path} must contain non-empty \"Address\" and \"Secret\".");
        }
        if (!Uri.TryCreate(config.Address, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new OrganizerException($"Config file {path}: \"Address\" must be an http(s) URL, got '{config.Address}'.");
        }
        return config;
    }

    /// <summary>%APPDATA%/Organizer/&lt;fileName&gt;.</summary>
    public static string DefaultPath(string fileName) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Organizer", fileName);
}
