using System.Globalization;
using System.Text;

namespace Organizer.Core;

/// <summary>
/// Appends text to size-capped log files <c>core-yyyyMMddTHHmmss.log</c> in one folder.
/// Appends to the newest file while it is under the cap, else starts a new one, and keeps
/// only the newest few. Every write is flushed. Any I/O problem drops that write; it never throws.
/// </summary>
public sealed class LogFileWriter : IDisposable
{
    public const long DefaultMaxBytes = 1024 * 1024;
    public const int DefaultKeep = 7;
    private const string Prefix = "core-";
    private const string Suffix = ".log";
    private const string Stamp = "yyyyMMddTHHmmss";

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private readonly object _lock = new();
    private readonly string _folder;
    private readonly long _maxBytes;
    private readonly int _keep;
    private FileStream? _stream;
    private bool _disposed;

    public LogFileWriter(string folder, long maxBytes = DefaultMaxBytes, int keep = DefaultKeep)
    {
        _folder = folder;
        _maxBytes = maxBytes;
        _keep = keep;
    }

    public void Write(string text)
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }
            try
            {
                var bytes = Utf8.GetBytes(text);
                if (_stream is { } s && s.Length >= _maxBytes)
                {
                    Close();
                }
                _stream ??= Open();
                _stream.Write(bytes);
                _stream.Flush();
            }
            catch (Exception)
            {
                Close(); // drop this line; try to reopen on the next one
            }
        }
    }

    private FileStream Open()
    {
        Directory.CreateDirectory(_folder);
        var newest = Files().LastOrDefault();
        if (newest is not null && new FileInfo(newest).Length < _maxBytes)
        {
            return Create(newest, FileMode.Append);
        }

        // Names must sort chronologically, so never go below the newest existing name.
        var stamp = DateTime.Now;
        if (newest is not null && TryParse(newest, out var last) && last >= Truncate(stamp))
        {
            stamp = last.AddSeconds(1);
        }
        var stream = Create(Path.Combine(_folder, Prefix + stamp.ToString(Stamp, CultureInfo.InvariantCulture) + Suffix),
            FileMode.CreateNew);
        Prune();
        return stream;
    }

    private static FileStream Create(string path, FileMode mode) =>
        new(path, mode, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1, FileOptions.WriteThrough);

    private List<string> Files()
    {
        var files = Directory.GetFiles(_folder, Prefix + "*" + Suffix).ToList();
        files.Sort(StringComparer.Ordinal);
        return files;
    }

    private void Prune()
    {
        try
        {
            var files = Files();
            for (var i = 0; i < files.Count - _keep; i++)
            {
                try { File.Delete(files[i]); } catch (Exception) { }
            }
        }
        catch (Exception) { }
    }

    private static DateTime Truncate(DateTime t) => new(t.Ticks - t.Ticks % TimeSpan.TicksPerSecond);

    private static bool TryParse(string path, out DateTime stamp)
    {
        var name = Path.GetFileNameWithoutExtension(path)[Prefix.Length..];
        return DateTime.TryParseExact(name, Stamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out stamp);
    }

    private void Close()
    {
        try { _stream?.Dispose(); } catch (Exception) { }
        _stream = null;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            Close();
        }
    }
}

/// <summary>Formats entries as <c>yyyy-MM-dd HH:mm:ss.fff Level Category Message</c>, exception text on the lines after.</summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly LogFileWriter _writer;

    public FileLoggerProvider(LogFileWriter writer) => _writer = writer;

    public ILogger CreateLogger(string categoryName) => new FileLogger(_writer, categoryName);

    public void Dispose() => _writer.Dispose();

    internal static string Format(string level, string category, string message, Exception? exception)
    {
        var sb = new StringBuilder();
        sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
            .Append(' ').Append(level).Append(' ').Append(category).Append(' ').Append(message).Append('\n');
        if (exception is not null)
        {
            sb.Append(exception).Append('\n');
        }
        return sb.ToString();
    }

    internal static string LevelName(LogLevel level) => level switch
    {
        LogLevel.Trace => "Trace",
        LogLevel.Debug => "Debug",
        LogLevel.Information => "Info",
        LogLevel.Warning => "Warn",
        LogLevel.Error => "Error",
        _ => "Crit",
    };

    private sealed class FileLogger(LogFileWriter writer, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            try
            {
                writer.Write(Format(LevelName(logLevel), category, formatter(state, exception), exception));
            }
            catch (Exception) { }
        }
    }
}
