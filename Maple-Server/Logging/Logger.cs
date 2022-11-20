namespace Maple_Server.Logging;

public class Logger
{
    private Logger() {}

    private static Logger? _instance;
    public static Logger Instance => _instance ??= new Logger();
    
    private readonly object _consoleLock = new object();

    public void Log(LogSeverity severity, string message)
    {
        lock (_consoleLock)
        {
            var oldColor = Console.ForegroundColor;

            Console.ForegroundColor = severityToColor(severity);
            
            Console.WriteLine($"[{DateTime.Now:s}] [{severity.ToString().ToUpper()}] {message}");

            Console.ForegroundColor = oldColor;
        }
    }

    private ConsoleColor severityToColor(LogSeverity severity)
    {
        ConsoleColor color;
        
        switch (severity)
        {
            case LogSeverity.Debug:
                color = ConsoleColor.Gray;
                break;
            case LogSeverity.Warning:
                color = ConsoleColor.DarkYellow;
                break;
            case LogSeverity.Error:
                color = ConsoleColor.Red;
                break;
            default:
                color = ConsoleColor.White;
                break;
        }

        return color;
    }
}