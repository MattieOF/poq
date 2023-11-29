using System.Runtime.CompilerServices;

namespace poq;

/// <summary>
/// The severity/role of each log message.
/// Can be used to filter log messages both in frontends and by <see cref="LogCategory"/>s.
/// <para>Effects the colour of text used in the console (trace = gray, info = green, warn = yellow,
/// error = red, fatal = dark red).</para>
/// </summary>
public enum LogLevel
{
    Trace,
    Info,
    Warn,
    Error,
    Fatal
}

/// <summary>
/// Struct describing the name and minimum/default log levels for a log category.
/// This allows for messages from certain parts of the engine or game to be filtered out or amplified.
/// </summary>
/// <param name="Name">Display name used for the log category.</param>
/// <param name="Minimum">Any message logged with this category with a level below this will be ignored.</param>
/// <param name="Default">If no level is provided, use this level by default when logging with this category.</param>
public record struct LogCategory(
    string Name = "UnnamedCategory",
    LogLevel Minimum = LogLevel.Trace,
    LogLevel Default = LogLevel.Info);

/// <summary>
/// Static class holding data and providing functions for logging.
/// </summary>
public static class Log
{
    /// <summary>
    /// Category used for logging about core engine functionality.
    /// </summary>
    internal static LogCategory CoreEngine = new("poqCore");
    /// <summary>
    /// Category used by the logger itself.
    /// </summary>
    internal static LogCategory Logger = new("poqLog");

    /// <summary>
    /// How often the log file will be flushed.
    /// </summary>
    public static TimeSpan LogFileFlushInterval
    {
        get => _logFileFlushInterval;
        set => SetLogFileFlushInterval(value);
    }
    
    /// <summary>
    /// Whether or not the log has been initialised or not (opening log file, etc.)
    /// </summary>
    public static bool Initialised { get; private set; }
    /// <summary>
    /// If true, log to a file
    /// </summary>
    public static readonly bool LogToFile = true;
    /// <summary>
    /// If true, log to the console
    /// </summary>
    public static readonly bool LogToConsole = true;
    
    /// <summary>
    /// Log file used if <see cref="LogToFile"/> is true.
    /// </summary>
    private static TextWriter? _logFile;
    /// <summary>
    /// Timer responsible for flushing the log file every time <see cref="LogFileFlushInterval"/> elapses.
    /// </summary>
    private static Timer _logFlushTimer = new(_ => _logFile?.Flush());
    /// <summary>
    /// Private instance of <see cref="LogFileFlushInterval"/> so we can retrieve the current value.
    /// </summary>
    private static TimeSpan _logFileFlushInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Initialise the log; for example, open the log file.
    /// Calling this function when the log has already been initialised will silently return.
    /// </summary>
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

    /// <summary>
    /// Shut down the log (for example, flush and close file)
    /// </summary>
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="newInterval"></param>
    private static void SetLogFileFlushInterval(TimeSpan newInterval)
    {
        _logFileFlushInterval = newInterval;
        _logFlushTimer.Change(TimeSpan.Zero, newInterval);
    }
    
    /// <summary>
    /// Base function to write a message to the log
    /// </summary>
    /// <param name="category">Category to use</param>
    /// <param name="level">Level to write at</param>
    /// <param name="message">The message!</param>
    /// <exception cref="InvalidOperationException">In debug mode, throws if called before <see cref="Init"/></exception>
    private static void WriteLog(in LogCategory category, in LogLevel level, in string message)
    {
        // in modifier should prevent copying data that doesn't need to be copied
#if DEBUG
        if (!Initialised)
        {
            throw new InvalidOperationException("Tried to log before log has been initialised!");
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
            _logFile!.WriteLine(formatted);
    }

    /// <summary>
    /// Get the colour for the provided <see cref="LogLevel"/>.
    /// Trace is gray, info is green, warn is yellow, error is red, fatal is dark red.
    /// </summary>
    /// <param name="level">Level to get the colour for</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid <see cref="LogLevel"/> is provided.</exception>
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

    /// <summary>
    /// Flush the log file. Can be called safely even if <see cref="LogToFile"/> is false.
    /// </summary>
    public static void Flush() => _logFile?.Flush();

    /// <summary>
    /// Write a message to the log with the specified category, at that categories default level.
    /// </summary>
    /// <param name="category">The category to use when writing to the log</param>
    /// <param name="message">The message to log</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Write(this in LogCategory category, in string message) => WriteLog(category, category.Default, message);
    /// <summary>
    /// Trace to the log with the specified category.
    /// </summary>
    /// <inheritdoc cref="Write"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Trace(this in LogCategory category, in string message) => WriteLog(category, LogLevel.Trace, message);
    /// <summary>
    /// Write an info message to the log with the specified category.
    /// </summary>
    /// <inheritdoc cref="Write"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Info(this in LogCategory category, in string message) => WriteLog(category, LogLevel.Info, message);
    /// <summary>
    /// Write a warning message to the log with the specified category.
    /// </summary>
    /// <inheritdoc cref="Write"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Warn(this in LogCategory category, in string message) => WriteLog(category, LogLevel.Warn, message);
    /// <summary>
    /// Write an error message to the log with the specified category.
    /// </summary>
    /// <inheritdoc cref="Write"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Error(this in LogCategory category, in string message) => WriteLog(category, LogLevel.Error, message);
    /// <summary>
    /// Write a fatal error message to the log with the specified category.
    /// </summary>
    /// <inheritdoc cref="Write"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Fatal(this in LogCategory category, in string message) => WriteLog(category, LogLevel.Fatal, message);
}
