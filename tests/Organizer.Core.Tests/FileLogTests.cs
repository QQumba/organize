using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Organizer.Core.Tests;

public sealed class FileLogWriterTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("organizer-log-").FullName;

    public void Dispose()
    {
        try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
    }

    private string[] Files() =>
        Directory.GetFiles(_folder, "core-*.log").OrderBy(f => f, StringComparer.Ordinal).ToArray();

    [Fact]
    public void File_name_has_sortable_local_timestamp()
    {
        using (var w = new LogFileWriter(_folder)) { w.Write("x\n"); }

        var name = Path.GetFileName(Assert.Single(Files()));
        Assert.Matches(@"^core-\d{8}T\d{6}\.log$", name);
    }

    [Fact]
    public void Appends_to_newest_file_under_the_cap()
    {
        using (var w = new LogFileWriter(_folder, maxBytes: 1000)) { w.Write("one\n"); }
        using (var w = new LogFileWriter(_folder, maxBytes: 1000)) { w.Write("two\n"); }

        Assert.Equal("one\ntwo\n", File.ReadAllText(Assert.Single(Files())));
    }

    [Fact]
    public void Creates_a_new_file_when_newest_is_at_or_over_the_cap()
    {
        using (var w = new LogFileWriter(_folder, maxBytes: 4)) { w.Write("abcd\n"); }
        using (var w = new LogFileWriter(_folder, maxBytes: 4)) { w.Write("next\n"); }

        var files = Files();
        Assert.Equal(2, files.Length);
        Assert.Equal("abcd\n", File.ReadAllText(files[0]));
        Assert.Equal("next\n", File.ReadAllText(files[1]));
    }

    [Fact]
    public void Rolls_during_a_run_and_keeps_only_the_newest_seven()
    {
        using (var w = new LogFileWriter(_folder, maxBytes: 5, keep: 7))
        {
            for (var i = 0; i < 20; i++) { w.Write($"line{i:00}\n"); }
        }

        var files = Files();
        Assert.Equal(7, files.Length);
        Assert.Equal("line19\n", File.ReadAllText(files[^1]));
        Assert.Equal("line13\n", File.ReadAllText(files[0]));
    }

    [Fact]
    public void Can_be_read_while_open()
    {
        using var w = new LogFileWriter(_folder);
        w.Write("hello\n");

        using var reader = new FileStream(Files()[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        Assert.Equal("hello\n", new StreamReader(reader).ReadToEnd());
    }

    [Fact]
    public void Entry_has_timestamp_level_category_message_and_exception_stack()
    {
        using (var w = new LogFileWriter(_folder))
        using (var provider = new FileLoggerProvider(w))
        {
            var log = provider.CreateLogger("My.Category");
            Exception? ex = null;
            try { throw new InvalidOperationException("boom"); } catch (Exception e) { ex = e; }
            log.LogError(ex, "It failed {Thing}", "badly");
        }

        var text = File.ReadAllText(Assert.Single(Files()));
        Assert.Matches(@"^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d\.\d{3} Error My\.Category It failed badly\n", text);
        Assert.Contains("System.InvalidOperationException: boom", text);
        Assert.Contains("   at ", text);
    }

    [Fact]
    public void Cyrillic_round_trips_without_bom()
    {
        using (var w = new LogFileWriter(_folder)) { w.Write("Купити молоко\n"); }

        var bytes = File.ReadAllBytes(Assert.Single(Files()));
        Assert.NotEqual(0xEF, bytes[0]);
        Assert.Equal("Купити молоко\n", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Many_threads_produce_whole_lines()
    {
        using (var w = new LogFileWriter(_folder, maxBytes: 20_000, keep: 1000))
        {
            Parallel.For(0, 8, t =>
            {
                for (var i = 0; i < 300; i++) { w.Write($"thread {t} line {i} {new string('x', 40)}\n"); }
            });
        }

        var lines = Files().SelectMany(File.ReadAllLines).ToList();
        Assert.Equal(2400, lines.Count);
        Assert.All(lines, l => Assert.Matches(@"^thread \d line \d+ x{40}$", l));
    }

    [Fact]
    public void Unwritable_folder_does_not_throw()
    {
        var blocker = Path.Combine(_folder, "file");
        File.WriteAllText(blocker, "");

        using var w = new LogFileWriter(Path.Combine(blocker, "sub"));
        w.Write("dropped\n");
        w.Write("dropped\n");
    }
}

public class CoreLogIntegrationTests
{
    [Fact]
    public void Running_core_writes_using_config_line_and_releases_the_file()
    {
        string folder;
        using (var core = new CoreHarness())
        {
            core.WriteConfig("{ \"Secret\": \"s\" }"); // pre-written, so the Core logs "Using" rather than "Created"
            _ = core.Server;
            folder = core.Folder;
            var log = Assert.Single(Directory.GetFiles(folder, "core-*.log"));
            using var s = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            Assert.Contains("Using config file", new StreamReader(s).ReadToEnd());
        }

        Assert.False(Directory.Exists(folder));
    }
}

public sealed partial class FailedStartTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("organizer-failstart-").FullName;

    public void Dispose()
    {
        try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
    }

    private (int ExitCode, string Log) StartCore(params string[] extraArgs)
    {
        var dir = AppContext.BaseDirectory;
        var psi = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = dir, // where appsettings.json is found
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add(Path.Combine(dir, "Organizer.Core.dll"));
        psi.ArgumentList.Add("--config");
        psi.ArgumentList.Add(Path.Combine(_folder, "core.json"));
        foreach (var a in extraArgs) { psi.ArgumentList.Add(a); }

        using var p = Process.Start(psi)!;
        p.OutputDataReceived += (_, _) => { };
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        if (!p.WaitForExit(TimeSpan.FromSeconds(30)))
        {
            p.Kill(entireProcessTree: true);
            Assert.Fail("The Core did not exit.");
        }
        p.WaitForExit();

        var logs = Directory.GetFiles(_folder, "core-*.log");
        var log = string.Concat(logs.Order(StringComparer.Ordinal).Select(File.ReadAllText));
        return (p.ExitCode, log);
    }

    [Fact]
    public void Port_in_use_is_logged_and_exits_non_zero()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var (code, log) = StartCore("--Port", port.ToString());

            Assert.NotEqual(0, code);
            Assert.Contains("config file", log);
            Assert.Matches(@"(?i)address already in use|only one usage|in use", log);
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public void Invalid_json_in_core_json_is_logged_and_exits_non_zero()
    {
        File.WriteAllText(Path.Combine(_folder, "core.json"), "{ not json");

        var (code, log) = StartCore();

        Assert.NotEqual(0, code);
        Assert.Contains("failed to start", log);
        Assert.Contains("core.json", log);
    }

    [Fact]
    public void Empty_secret_is_logged_and_exits_non_zero()
    {
        File.WriteAllText(Path.Combine(_folder, "core.json"), "{ \"Secret\": \"\" }");

        var (code, log) = StartCore();

        Assert.NotEqual(0, code);
        Assert.Contains("No Secret configured", log);
    }
}
