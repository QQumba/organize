using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Organizer.Client;

namespace Organizer.Client.Tests;

/// <summary>Runs install.ps1 and dev.ps1 with -ConfigOnly against a temp folder and loads the files like the real programs do.</summary>
public sealed class ConfigScriptTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("organizer-config-").FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    private static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Organizer.sln"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("Organizer.sln not found above " + AppContext.BaseDirectory);
    }

    private void Run(string script)
    {
        var psi = new ProcessStartInfo("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{Path.Combine(RepoRoot(), script)}\" -ConfigOnly -ConfigDir \"{_dir}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit();
        Assert.True(p.ExitCode == 0, output);
    }

    private string P(string name) => Path.Combine(_dir, name);

    private (string Secret, int Port, ClientConfig Cli, ClientConfig Desktop) Load()
    {
        // The Core itself loads core.json (defaults from appsettings.json underneath).
        using var core = new CoreHarness();
        core.WriteConfig(File.ReadAllText(P("core.json")));
        var cli = ClientConfig.Load(P("cli.json"), "cli.json");
        var desktop = ClientConfig.Load(P("desktop.json"), "desktop.json");
        return (core.Secret, core.Effective.Port, cli, desktop);
    }

    [Fact]
    public void Install_config_shares_one_secret_and_the_default_port()
    {
        Run("install.ps1");
        var (secret, port, cli, desktop) = Load();

        Assert.Equal(secret, cli.Secret);
        Assert.Equal(secret, desktop.Secret);
        Assert.Equal($"http://127.0.0.1:{port}", cli.Address);
        Assert.Equal(cli.Address, desktop.Address);
        Assert.Equal(43, secret.Length); // 32 bytes, unpadded URL-safe base64
        Assert.DoesNotContain("Port", File.ReadAllText(P("core.json")));
    }

    [Fact]
    public void Install_config_second_run_changes_nothing()
    {
        Run("install.ps1");
        var before = new[] { "core.json", "cli.json", "desktop.json" }.Select(n => File.ReadAllText(P(n))).ToArray();
        Run("install.ps1");
        var after = new[] { "core.json", "cli.json", "desktop.json" }.Select(n => File.ReadAllText(P(n))).ToArray();
        Assert.Equal(before, after);
    }

    [Fact]
    public void Install_config_reuses_an_existing_core_secret()
    {
        File.WriteAllText(P("core.json"), "{ \"Secret\": \"existing-secret\" }");
        Run("install.ps1");
        var (secret, _, cli, desktop) = Load();
        Assert.Equal("existing-secret", secret);
        Assert.Equal("existing-secret", cli.Secret);
        Assert.Equal("existing-secret", desktop.Secret);
    }

    [Fact]
    public void Install_config_leaves_a_client_config_with_another_secret_alone()
    {
        var mine = "{ \"Address\": \"http://example:1\", \"Secret\": \"other\" }";
        File.WriteAllText(P("cli.json"), mine);
        Run("install.ps1");
        Assert.Equal(mine, File.ReadAllText(P("cli.json")));
    }

    [Fact]
    public void Dev_config_uses_the_dev_port_and_hotkey()
    {
        Run("dev.ps1");
        var (secret, port, cli, desktop) = Load();

        Assert.Equal(47822, port);
        Assert.Equal(secret, cli.Secret);
        Assert.Equal(secret, desktop.Secret);
        Assert.Equal("http://127.0.0.1:47822", cli.Address);
        Assert.Equal("http://127.0.0.1:47822", desktop.Address);
        Assert.Equal("Alt+Shift+Q", JsonNode.Parse(File.ReadAllText(P("desktop.json")))!["Hotkey"]!.GetValue<string>());
    }

    [Fact]
    public void Dev_config_adds_a_missing_port_and_keeps_the_secret()
    {
        File.WriteAllText(P("core.json"), "{ \"Secret\": \"old-secret\" }");
        File.WriteAllText(P("cli.json"), "{ \"Address\": \"http://127.0.0.1:47821\", \"Secret\": \"old-secret\" }");
        File.WriteAllText(P("desktop.json"), "{ \"Address\": \"http://127.0.0.1:47821\", \"Secret\": \"old-secret\" }");
        Run("dev.ps1");
        var (secret, port, cli, desktop) = Load();

        Assert.Equal("old-secret", secret);
        Assert.Equal(47822, port);
        Assert.Equal("old-secret", cli.Secret);
        Assert.Equal("http://127.0.0.1:47822", cli.Address);
        Assert.Equal("http://127.0.0.1:47822", desktop.Address);
        Assert.Equal("Alt+Shift+Q", JsonNode.Parse(File.ReadAllText(P("desktop.json")))!["Hotkey"]!.GetValue<string>());
    }

    [Fact]
    public void Dev_config_second_run_changes_nothing()
    {
        Run("dev.ps1");
        var before = new[] { "core.json", "cli.json", "desktop.json" }.Select(n => File.ReadAllText(P(n))).ToArray();
        Run("dev.ps1");
        Assert.Equal(before, new[] { "core.json", "cli.json", "desktop.json" }.Select(n => File.ReadAllText(P(n))).ToArray());
    }
}
