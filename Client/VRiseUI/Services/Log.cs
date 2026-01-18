using BepInEx.Logging;

namespace VRiseUI.Services;

/// <summary>
/// Central logging wrapper with VDebug integration.
/// Use this for all logging in VRiseUI.
/// </summary>
public static class Log
{
    private static ManualLogSource _bepinexLog;
    private static bool _initialized;
    private static bool _verbose = true; // Enable verbose logging by default

    /// <summary>
    /// Enable/disable verbose debug logging.
    /// </summary>
    public static bool Verbose
    {
        get => _verbose;
        set => _verbose = value;
    }

    /// <summary>
    /// Initialize the logger.
    /// </summary>
    public static void Initialize(ManualLogSource log)
    {
        _bepinexLog = log;
        _initialized = true;

        // Initialize VDebug bridge
        DebugBridge.Initialize(log);

        Debug("Log", "Logger initialized with VDebug bridge.");
    }

    /// <summary>
    /// Log info message.
    /// </summary>
    public static void Info(string message)
    {
        DebugBridge.LogInfo(message);
    }

    /// <summary>
    /// Log info message with category.
    /// </summary>
    public static void Info(string category, string message)
    {
        DebugBridge.LogInfo(category, message);
    }

    /// <summary>
    /// Log warning message.
    /// </summary>
    public static void Warning(string message)
    {
        DebugBridge.LogWarning(message);
    }

    /// <summary>
    /// Log warning message with category.
    /// </summary>
    public static void Warning(string category, string message)
    {
        DebugBridge.LogWarning(category, message);
    }

    /// <summary>
    /// Log error message.
    /// </summary>
    public static void Error(string message)
    {
        DebugBridge.LogError(message);
    }

    /// <summary>
    /// Log error message with category.
    /// </summary>
    public static void Error(string category, string message)
    {
        DebugBridge.LogError(category, message);
    }

    /// <summary>
    /// Log debug message (only if verbose is enabled).
    /// </summary>
    public static void Debug(string message)
    {
        if (_verbose)
        {
            DebugBridge.LogInfo("DEBUG", message);
        }
    }

    /// <summary>
    /// Log debug message with category (only if verbose is enabled).
    /// </summary>
    public static void Debug(string category, string message)
    {
        if (_verbose)
        {
            DebugBridge.LogInfo(category, $"[DEBUG] {message}");
        }
    }

    /// <summary>
    /// Log a method entry (for tracing).
    /// </summary>
    public static void Trace(string method)
    {
        if (_verbose)
        {
            DebugBridge.LogInfo("TRACE", $">> {method}");
        }
    }

    /// <summary>
    /// Log a method exit (for tracing).
    /// </summary>
    public static void TraceExit(string method)
    {
        if (_verbose)
        {
            DebugBridge.LogInfo("TRACE", $"<< {method}");
        }
    }
}
