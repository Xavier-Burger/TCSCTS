namespace TCSCTS;

public static class ConsoleHelper
{
    // Lock to keep color changes + writes atomic when multiple tasks log concurrently.
    private static readonly object _consoleLock = new();

    // General log with optional color.
    public static void Log(DateTime startTime, string message, ConsoleColor? color = null)
    {
        TimeSpan diff = DateTime.Now - startTime;
        lock (_consoleLock)
        {
            var prevColor = Console.ForegroundColor;
            if (color.HasValue)
            {
                Console.ForegroundColor = color.Value;
            }
            Console.WriteLine($"[{diff:hh\\:mm\\:ss\\.fff}] {message}");
            if (color.HasValue)
            {
                Console.ForegroundColor = prevColor; // restore
            }
        }
    }

    // Title with optional color (defaults to Cyan for visibility if not specified).
    public static void Title(string message, ConsoleColor? color = null)
    {
        lock (_consoleLock)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = color ?? ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ForegroundColor = prevColor;
        }
    }

    // Convenience severity helpers (optional usage):
    public static void Info(DateTime startTime, string message) => Log(startTime, message, ConsoleColor.Gray);
    public static void Success(DateTime startTime, string message) => Log(startTime, message, ConsoleColor.Green);
    public static void Warn(DateTime startTime, string message) => Log(startTime, message, ConsoleColor.Yellow);
    public static void Error(DateTime startTime, string message) => Log(startTime, message, ConsoleColor.Red);

    // Per-class origin helpers
    public static void Workspace(DateTime startTime, string message) => Log(startTime, message, ConsoleColor.Blue);
    public static void Inventory(DateTime startTime, string message) => Log(startTime, message, ConsoleColor.DarkYellow);
    public static void Kettle(DateTime startTime, string message) => Log(startTime, message, ConsoleColor.DarkCyan);
    public static void Example(DateTime startTime, string message) => Log(startTime, message, ConsoleColor.White);
}