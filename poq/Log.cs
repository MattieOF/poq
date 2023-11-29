using System.Runtime.CompilerServices;

namespace poq;

public enum LogLevel
{
    Trace,
    Info,
    Warn,
    Error,
    Fatal
}

public record struct LogCategory(
    string Name = "UnnamedCategory",
    LogLevel Minimum = LogLevel.Trace,
    LogLevel Default = LogLevel.Info);

public static class Log
{
    internal static LogCategory CoreEngine = new("poqCore");
    internal static LogCategory Logger = new("poqLog");

    public static TimeSpan LogFileFlushInterval
    {
        get => _logFileFlushInterval;
        set => SetLogFileFlushInterval(value);
    }
    
    public static bool Initialised { get; private set; }
    public static readonly bool LogToFile = true;
    public static readonly bool LogToConsole = true;
    
    private static TextWriter? _logFile;
    private static Timer _logFlushTimer = new(_ => _logFile?.Flush());
    private static TimeSpan _logFileFlushInterval = TimeSpan.FromSeconds(5);

    public static void Init()
    {
        if (Initialised)
            return;

        if (LogToFile)
        {
            Directory.CreateDirectory("Saved/Logs/");
            var path = $"Saved/Logs/{DateTime.Now:yyyy-M-d_HH-mm-ss}.log";
            _logFile = new StreamWriter(path, true);
            Initialised = true;
            Logger.Info($"Initialised logger, logging to file {path}");
            
            // Setup via config
            SetLogFileFlushInterval(_logFileFlushInterval);
        }
        else
        {
            Initialised = true;
            Logger.Info($"Initialised logger at {DateTime.Now:f}");
        }
    }

    public static void Shutdown()
    {
        Logger.Info("Shutting down log");

        if (LogToFile && _logFile is not null)
        {
            _logFile.Flush();
            _logFile.Close();
            _logFile = null;
        }
        
        Initialised = false;
    }

    private static void SetLogFileFlushInterval(TimeSpan newInterval)
    {
        _logFileFlushInterval = newInterval;
        _logFlushTimer.Change(TimeSpan.Zero, newInterval);
    }
    
    private static void WriteLog(in LogCategory category, in LogLevel level, in string message)
    {
        // in modifier should prevent copying data that doesn't need to be copied
#if DEBUG
        if (!Initialised)
        {
            throw new Exception("Tried to log before log has been initialised!");
        }
#endif

        if (level < category.Minimum)
            return;

        var formatted = $"[{DateTime.Now:HH:mm:ss.fff} {category.Name} {level.ToString()}] {message}";
        
        if (LogToConsole)
        {
            Console.ForegroundColor = level.GetColour();
            Console.WriteLine(formatted);
            Console.ForegroundColor = ConsoleColor.White; // Possibly unnecessary? Should we be logging to console outside of this function?
        }

        if (LogToFile)
        {
            _logFile!.WriteLine(formatted);
        }
    }

    public static ConsoleColor GetColour(this LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.Green,
            LogLevel.Warn => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Fatal => ConsoleColor.DarkRed,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "LogLevel was not one of the valid values")
        };
    }

    public static void Flush() => _logFile?.Flush();

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Write(this in LogCategory category, in string message) => WriteLog(category, category.Default, message);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Trace(this in LogCategory category, in string message) => WriteLog(category, LogLevel.Trace, message);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Info(this in LogCategory category, in string message) => WriteLog(category, LogLevel.Info, message);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Warn(this in LogCategory category, in string message) => WriteLog(category, LogLevel.Warn, message);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Error(this in LogCategory category, in string message) => WriteLog(category, LogLevel.Error, message);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Fatal(this in LogCategory category, in string message) => WriteLog(category, LogLevel.Fatal, message);
}
