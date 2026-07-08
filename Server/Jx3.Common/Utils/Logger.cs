using System.Collections.Concurrent;

namespace Jx3.Common.Utils;

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warn = 2,
    Error = 3,
    Fatal = 4
}

public static class LoggerConfig
{
    public static LogLevel MinLevel { get; set; } = LogLevel.Info;
    public static string LogDirectory { get; set; } = "logs";
    public static bool EnableConsole { get; set; } = true;
    public static bool EnableFile { get; set; } = true;
    public static int MaxLogDays { get; set; } = 7;
}

public static class Logger
{
    private static readonly ConcurrentQueue<string> _buffer = new();
    private static readonly string _logDir;
    private static Timer? _flushTimer;
    private static Timer? _cleanupTimer;
    private static readonly object _fileLock = new();
    

    static Logger()
    {
        _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LoggerConfig.LogDirectory);
        try { Directory.CreateDirectory(_logDir); } catch { }

        _flushTimer = new Timer(_ => Flush(), null, 5000, 5000);
        _cleanupTimer = new Timer(_ => CleanupOldLogs(), null, 60000, 3600000);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Flush();
    }

    public static void Debug(string tag, string msg) => Write(LogLevel.Debug, tag, msg);
    public static void Info(string tag, string msg) => Write(LogLevel.Info, tag, msg);
    public static void Warn(string tag, string msg) => Write(LogLevel.Warn, tag, msg);
    public static void Error(string tag, string msg) => Write(LogLevel.Error, tag, msg);
    public static void Fatal(string tag, string msg) => Write(LogLevel.Fatal, tag, msg);

    public static void Exception(string tag, Exception ex)
    {
        Write(LogLevel.Error, tag, $"{ex.GetType().Name}: {ex.Message}");
        Write(LogLevel.Error, tag, $"Stack: {ex.StackTrace}");
        if (ex.InnerException != null)
            Write(LogLevel.Error, tag, $"Inner: {ex.InnerException.Message}");
    }

    private static void Write(LogLevel level, string tag, string msg)
    {
        if (level < LoggerConfig.MinLevel) return;

        var now = DateTime.Now;
        var time = now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level switch
        {
            LogLevel.Debug => "DEBUG",
            LogLevel.Info => "INFO",
            LogLevel.Warn => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Fatal => "FATAL",
            _ => "?"
        };

        var line = $"[{time}][{levelStr}][{tag}] {msg}";

        if (LoggerConfig.EnableConsole)
        {
            var color = level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warn => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Fatal => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
            lock (typeof(Console))
            {
                var orig = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(line);
                Console.ForegroundColor = orig;
            }
        }

        if (LoggerConfig.EnableFile)
        {
            _buffer.Enqueue($"{line}");
        }
    }

    private static void Flush()
    {
        if (_buffer.IsEmpty) return;

        var date = DateTime.Now.ToString("yyyy-MM-dd");
        var filePath = Path.Combine(_logDir, $"server-{date}.log");

        lock (_fileLock)
        {
            try
            {
                using var writer = new StreamWriter(filePath, append: true);
                while (_buffer.TryDequeue(out var line))
                {
                    writer.WriteLine(line);
                }
            }
            catch
            {
                // If file write fails, keep items in buffer
            }
        }
    }

    private static void CleanupOldLogs()
    {
        try
        {
            if (!Directory.Exists(_logDir)) return;
            var cutoff = DateTime.Now.AddDays(-LoggerConfig.MaxLogDays);
            foreach (var file in Directory.GetFiles(_logDir, "server-*.log"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var dateStr = name.Replace("server-", "");
                if (DateTime.TryParse(dateStr, out var date) && date < cutoff)
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// Read recent log lines from today's log file
    /// </summary>
    public static List<string> ReadRecentLines(int count = 50)
    {
        var date = DateTime.Now.ToString("yyyy-MM-dd");
        var filePath = Path.Combine(_logDir, $"server-{date}.log");
        try
        {
            if (!File.Exists(filePath)) return new List<string>();
            var lines = File.ReadAllLines(filePath).Reverse().Take(count).Reverse().ToList();
            return lines;
        }
        catch { return new List<string>(); }
    }
}
