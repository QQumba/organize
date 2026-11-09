using System.Globalization;
using System.Text;
using Organizer.Client;
using Organizer.Contracts;

namespace Organizer.Cli;

/// <summary>Argument parsing, output and exit codes. Everything else is the client library and the Core.</summary>
public static class CliApp
{
    public const int Ok = 0;
    public const int Failure = 1;
    public const int Usage = 2;

    public const string UsageText =
        """
        Usage:
          org add <text...>                         Capture an Item
          org ls [--category <name>] [--tag <name>]...   List the Inbox
          org done <id>                             Mark an Item done
        Options:
          --config <path>   Config file (default: %APPDATA%\Organizer\cli.json)
        """;

    private const string ConfigFileName = "cli.json";

    public static Task<int> RunAsync(string[] args, TextWriter output, TextWriter error) =>
        RunAsync(args, output, error, config => new OrganizerClient(config));

    /// <param name="createClient">Builds the client from the loaded config; tests replace it.</param>
    public static async Task<int> RunAsync(
        string[] args, TextWriter output, TextWriter error, Func<ClientConfig, OrganizerClient> createClient)
    {
        string? configPath = null;
        string? category = null;
        var tags = new List<string>();
        var positional = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--config" or "--category" or "--tag":
                    if (i + 1 >= args.Length)
                    {
                        return UsageError(error, $"{args[i]} needs a value.");
                    }
                    var value = args[++i];
                    if (args[i - 1] == "--config") configPath = value;
                    else if (args[i - 1] == "--category") category = value;
                    else tags.Add(value);
                    break;
                case "-h" or "--help":
                    output.WriteLine(UsageText);
                    return Ok;
                default:
                    if (args[i].StartsWith("--", StringComparison.Ordinal))
                    {
                        return UsageError(error, $"Unknown option {args[i]}.");
                    }
                    positional.Add(args[i]);
                    break;
            }
        }

        if (positional.Count == 0)
        {
            return UsageError(error, "Missing command.");
        }
        var command = positional[0];
        var rest = positional.Skip(1).ToArray();

        int? doneId = null;
        string? text = null;
        switch (command)
        {
            case "add":
                text = string.Join(' ', rest).Trim();
                if (text.Length == 0) return UsageError(error, "add needs the text to capture.");
                break;
            case "ls":
                if (rest.Length > 0) return UsageError(error, "ls takes no arguments, only --category and --tag.");
                break;
            case "done":
                if (rest.Length != 1 || !int.TryParse(rest[0], NumberStyles.None, CultureInfo.InvariantCulture, out var id))
                {
                    return UsageError(error, "done needs one numeric Item id.");
                }
                doneId = id;
                break;
            default:
                return UsageError(error, $"Unknown command '{command}'.");
        }
        if (command != "ls" && (category is not null || tags.Count > 0))
        {
            return UsageError(error, "--category and --tag only apply to ls.");
        }

        try
        {
            var config = ClientConfig.Load(configPath ?? ClientConfig.DefaultPath(ConfigFileName), ConfigFileName);
            using var client = createClient(config);
            switch (command)
            {
                case "add":
                    var added = await client.CaptureAsync(text!);
                    output.WriteLine($"Captured #{added.Id}: {added.Text}");
                    break;
                case "ls":
                    var items = await client.InboxAsync(category, tags);
                    if (items.Count == 0) output.WriteLine("Inbox is empty.");
                    foreach (var item in items) output.WriteLine(Format(item));
                    break;
                case "done":
                    var done = await client.DoneAsync(doneId!.Value);
                    output.WriteLine($"Done #{done.Id}: {done.Text}");
                    break;
            }
            return Ok;
        }
        catch (OrganizerException e)
        {
            error.WriteLine($"Error: {e.Message}");
            return Failure;
        }
    }

    /// <summary>"id  text  [Category] #tag #tag  due yyyy-MM-dd  !  U"</summary>
    public static string Format(ItemDto item)
    {
        var line = new StringBuilder();
        line.Append(item.Id.ToString(CultureInfo.InvariantCulture).PadLeft(4)).Append("  ").Append(item.Text);
        if (item.Category is not null) line.Append("  [").Append(item.Category).Append(']');
        foreach (var tag in item.Tags) line.Append("  #").Append(tag);
        if (item.DueDate is { } due) line.Append("  due ").Append(due.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        if (item.Important) line.Append("  !");
        if (item.Urgent) line.Append("  U");
        return line.ToString();
    }

    private static int UsageError(TextWriter error, string message)
    {
        error.WriteLine($"Error: {message}");
        error.WriteLine(UsageText);
        return Usage;
    }
}
