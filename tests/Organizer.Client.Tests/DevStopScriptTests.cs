using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Organizer.Client.Tests;

/// <summary>Runs dev.ps1 -Stop against a temp pids.json and dummy processes. Never starts the real Core or Vite.</summary>
public sealed class DevStopScriptTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("organizer-devstop-").FullName;
    private readonly List<Process> _dummies = [];

    public void Dispose()
    {
        foreach (var p in _dummies) { try { if (!p.HasExited) p.Kill(true); } catch { } p.Dispose(); }
        Directory.Delete(_dir, true);
    }

    private static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Organizer.sln"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("Organizer.sln not found above " + AppContext.BaseDirectory);
    }

    private string PidFile => Path.Combine(_dir, "pids.json");

    private Process Dummy()
    {
        var p = Process.Start(new ProcessStartInfo("powershell", "-NoProfile -Command Start-Sleep 300")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
        })!;
        _dummies.Add(p);
        // Wait until Path and StartTime are readable.
        for (var i = 0; i < 50; i++)
        {
            try { _ = p.MainModule!.FileName; _ = p.StartTime; break; } catch { Thread.Sleep(100); }
        }
        return p;
    }

    private static object Entry(Process p, DateTime? start = null, string? path = null) => new
    {
        pid = p.Id,
        startTime = (start ?? p.StartTime.ToUniversalTime()).ToString("o", CultureInfo.InvariantCulture),
        path = path ?? p.MainModule!.FileName,
    };

    private void WritePids(object entries) => File.WriteAllText(PidFile, JsonSerializer.Serialize(entries));

    private int Run(string args, out string output)
    {
        var psi = new ProcessStartInfo("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{Path.Combine(RepoRoot(), "dev.ps1")}\" {args}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var p = Process.Start(psi)!;
        output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit();
        return p.ExitCode;
    }

    private int Stop(out string output) => Run($"-Stop -ConfigDir \"{_dir}\"", out output);

    private static bool Alive(Process p) { p.Refresh(); return !p.HasExited; }

    [Fact]
    public void Stop_kills_a_recorded_process_leaves_others_alone_and_deletes_the_file()
    {
        var recorded = Dummy();
        var other = Dummy();
        WritePids(new { core = Entry(recorded) });

        Assert.Equal(0, Stop(out var output));

        Assert.True(recorded.WaitForExit(10_000), output);
        Assert.True(Alive(other));
        Assert.False(File.Exists(PidFile));
    }

    [Fact]
    public void Stop_leaves_a_process_whose_start_time_differs()
    {
        var p = Dummy();
        WritePids(new { core = Entry(p, start: p.StartTime.ToUniversalTime().AddSeconds(-30)) });

        Assert.Equal(0, Stop(out var output));

        Assert.True(Alive(p), output);
        Assert.Contains("skipped", output);
        Assert.False(File.Exists(PidFile));
    }

    [Fact]
    public void Stop_leaves_a_process_whose_path_differs()
    {
        var p = Dummy();
        WritePids(new { core = Entry(p, path: @"C:\somewhere\else\dotnet.exe") });

        Assert.Equal(0, Stop(out var output));

        Assert.True(Alive(p), output);
        Assert.Contains("skipped", output);
        Assert.False(File.Exists(PidFile));
    }

    [Fact]
    public void Stop_without_a_file_exits_zero() { var code = Stop(out var output); Assert.True(code == 0, output); }

    [Fact]
    public void Stop_with_invalid_json_exits_zero_and_deletes_the_file()
    {
        File.WriteAllText(PidFile, "{ not json");
        Assert.Equal(0, Stop(out var output));
        Assert.False(File.Exists(PidFile), output);
    }

    [Fact]
    public void Start_and_Stop_together_is_a_usage_error()
    {
        var code = Run($"-Start -Stop -ConfigDir \"{_dir}\"", out var output);
        Assert.NotEqual(0, code);
        Assert.Contains("cannot be used together", output);
    }

    [Fact]
    public void ConfigOnly_with_Start_is_a_usage_error()
    {
        var code = Run($"-Start -ConfigOnly -ConfigDir \"{_dir}\"", out var output);
        Assert.NotEqual(0, code);
        Assert.False(File.Exists(Path.Combine(_dir, "core.json")), output);
    }
}
