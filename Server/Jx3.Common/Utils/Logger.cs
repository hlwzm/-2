namespace Jx3.Common.Utils;

/// <summary>简单日志</summary>
public static class Logger
{
    public static void Info(string tag, string msg) => Log("INFO", tag, msg);
    public static void Warn(string tag, string msg) => Log("WARN", tag, msg);
    public static void Error(string tag, string msg) => Log("ERROR", tag, msg);

    private static void Log(string level, string tag, string msg)
    {
        var time = DateTime.Now.ToString("HH:mm:ss.fff");
        Console.WriteLine($"[{time}][{level}][{tag}] {msg}");
    }
}