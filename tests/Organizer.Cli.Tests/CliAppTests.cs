using Organizer.Client;

namespace Organizer.Cli.Tests;

public class CliAppTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("organizer-cli-").FullName;
    private string ConfigPath => Path.Combine(_dir, "cli.json");

    public void Dispose() => Directory.Delete(_dir, true);

    private static async Task<(int Code, string Out, string Err)> Run(
        Func<ClientConfig, OrganizerClient> factory, params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        var code = await CliApp.RunAsync(args, o, e, factory);
        return (code, o.ToString(), e.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("bogus")]
    [InlineData("add")]
    [InlineData("done")]
    [InlineData("done abc")]
    [InlineData("done 1 2")]
    [InlineData("ls extra")]
    [InlineData("ls --nope")]
    [InlineData("ls --tag")]
    [InlineData("add hello --category x")]
    public async Task Bad_arguments_print_usage_and_exit_2(string line)
    {
        var args = line.Length == 0 ? [] : line.Split(' ');
        var r = await Run(_ => throw new InvalidOperationException("must not connect"), args);
        Assert.Equal(CliApp.Usage, r.Code);
        Assert.Contains("Usage:", r.Err);
    }

    [Fact]
    public async Task Missing_config_file_says_what_to_create_and_exits_1()
    {
        var r = await Run(_ => throw new InvalidOperationException(), "ls", "--config", ConfigPath);
        Assert.Equal(CliApp.Failure, r.Code);
        Assert.Contains("Config file not found", r.Err);
        Assert.Contains("\"Secret\"", r.Err);
    }

    [Fact]
    public async Task Unreachable_Core_exits_1_with_a_clear_error()
    {
        File.WriteAllText(ConfigPath, """{ "Address": "http://127.0.0.1:1", "Secret": "x" }""");
        var r = await Run(c => new OrganizerClient(c), "ls", "--config", ConfigPath);
        Assert.Equal(CliApp.Failure, r.Code);
        Assert.StartsWith("Error: Cannot reach the Core", r.Err);
    }

    [Fact]
    public async Task Wrong_secret_exits_1_with_a_clear_error()
    {
        using var core = new CoreHarness();
        File.WriteAllText(ConfigPath, """{ "Address": "http://x", "Secret": "x" }""");
        var r = await Run(_ => new OrganizerClient(core.CreateClient("wrong")), "add", "hi", "--config", ConfigPath);
        Assert.Equal(CliApp.Failure, r.Code);
        Assert.Contains("rejected the secret", r.Err);
    }

    [Fact]
    public async Task Add_ls_done_print_plain_text_and_exit_0()
    {
        using var core = new CoreHarness();
        File.WriteAllText(ConfigPath, """{ "Address": "http://x", "Secret": "x" }""");
        OrganizerClient Factory(ClientConfig _) => new(core.CreateAuthenticatedClient());

        var add = await Run(Factory, "add", "Купити", "хліб", "--config", ConfigPath);
        Assert.Equal(0, add.Code);
        Assert.Equal("Captured #1: Купити хліб" + Environment.NewLine, add.Out);

        var ls = await Run(Factory, "ls", "--config", ConfigPath);
        Assert.Equal("   1  Купити хліб" + Environment.NewLine, ls.Out);

        var done = await Run(Factory, "done", "1", "--config", ConfigPath);
        Assert.Equal(0, done.Code);

        var empty = await Run(Factory, "ls", "--config", ConfigPath);
        Assert.Equal("Inbox is empty." + Environment.NewLine, empty.Out);

        var missing = await Run(Factory, "done", "99", "--config", ConfigPath);
        Assert.Equal(CliApp.Failure, missing.Code);
        Assert.Contains("99", missing.Err);
    }
}
