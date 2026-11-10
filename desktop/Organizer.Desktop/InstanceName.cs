using System.IO;
using System.Security.Cryptography;
using System.Text;
using Organizer.Client;

namespace Organizer.Desktop;

/// <summary>Name of the single-instance mutex. It is keyed by the config file, so a dev tray app and an installed one can run side by side.</summary>
public static class InstanceName
{
    public static string MutexName(string? configPath)
    {
        var full = Path.GetFullPath(configPath ?? ClientConfig.DefaultPath("desktop.json")).ToLowerInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(full));
        return @"Local\Organizer.Desktop." + Convert.ToHexString(hash, 0, 8);
    }
}
