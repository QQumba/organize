using System.IO;
using System.Text.Json;

namespace Organizer.Desktop;

/// <summary>Reads the optional Hotkey key of desktop.json. ClientConfig only knows Address and Secret.</summary>
public static class DesktopSettings
{
    /// <summary>The raw Hotkey text, or null when the file or key is absent or unreadable (ClientConfig reports config errors).</summary>
    public static string? ReadHotkey(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                if (string.Equals(p.Name, "Hotkey", StringComparison.OrdinalIgnoreCase))
                {
                    return p.Value.ValueKind == JsonValueKind.Null ? null : p.Value.ToString();
                }
            }
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
        }
        return null;
    }
}
