namespace Organizer.Desktop;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Optional: --config <path> overrides %APPDATA%/Organizer/desktop.json.
        var i = Array.IndexOf(args, "--config");
        var configPath = i >= 0 && i + 1 < args.Length ? args[i + 1] : null;

        // Single instance: a second launch exits quietly.
        using var mutex = new Mutex(true, @"Local\Organizer.Desktop", out var first);
        if (!first) return;

        var app = new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
        using var tray = new TrayApp(configPath);
        app.Run();
    }
}
