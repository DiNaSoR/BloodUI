using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Logging;

namespace VRiseUI.Services;

/// <summary>
/// Optional integration point for the external VDebug plugin.
/// If the debug plugin is not installed, falls back to BepInEx logging.
/// </summary>
public static class DebugBridge
{
    const string DebugToolsAssemblyName = "VDebug";
    const string DebugToolsApiTypeName = "VDebug.VDebugApi";
    const string Source = "VRiseUI";

    static bool _initialized;
    static bool _vdebugAvailable;
    static MethodInfo _logInfo;
    static MethodInfo _logWarning;
    static MethodInfo _logError;
    static ManualLogSource _fallbackLog;

    /// <summary>
    /// Initialize the debug bridge.
    /// </summary>
    public static void Initialize(ManualLogSource fallbackLog)
    {
        _fallbackLog = fallbackLog;
        _vdebugAvailable = FindDebugToolsAssembly() != null;
        _initialized = true;

        if (_vdebugAvailable)
        {
            _fallbackLog?.LogInfo("[DebugBridge] VDebug detected! Logging to VDebug.");
        }
        else
        {
            _fallbackLog?.LogInfo("[DebugBridge] VDebug not found. Logging to BepInEx.");
        }
    }

    /// <summary>
    /// Log an info message.
    /// </summary>
    public static void LogInfo(string message) => LogInfo(null, message);

    /// <summary>
    /// Log an info message with category.
    /// </summary>
    public static void LogInfo(string category, string message)
    {
        var fullMessage = string.IsNullOrEmpty(category) ? message : $"[{category}] {message}";

        if (_vdebugAvailable)
        {
            TryLogVDebug("LogInfo", Source, category, message, ref _logInfo);
        }

        // Always log to BepInEx too for visibility
        _fallbackLog?.LogInfo(fullMessage);
    }

    /// <summary>
    /// Log a warning message.
    /// </summary>
    public static void LogWarning(string message) => LogWarning(null, message);

    /// <summary>
    /// Log a warning message with category.
    /// </summary>
    public static void LogWarning(string category, string message)
    {
        var fullMessage = string.IsNullOrEmpty(category) ? message : $"[{category}] {message}";

        if (_vdebugAvailable)
        {
            TryLogVDebug("LogWarning", Source, category, message, ref _logWarning);
        }

        _fallbackLog?.LogWarning(fullMessage);
    }

    /// <summary>
    /// Log an error message.
    /// </summary>
    public static void LogError(string message) => LogError(null, message);

    /// <summary>
    /// Log an error message with category.
    /// </summary>
    public static void LogError(string category, string message)
    {
        var fullMessage = string.IsNullOrEmpty(category) ? message : $"[{category}] {message}";

        if (_vdebugAvailable)
        {
            TryLogVDebug("LogError", Source, category, message, ref _logError);
        }

        _fallbackLog?.LogError(fullMessage);
    }

    static void TryLogVDebug(string methodName, string source, string category, string message, ref MethodInfo cache)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        try
        {
            // Try v3 API: LogX(source, category, message)
            if (TryResolveMethod(methodName, new[] { typeof(string), typeof(string), typeof(string) }, ref cache, out var method))
            {
                method.Invoke(null, new object[] { source, category, message });
                return;
            }

            // Try v2 API: LogX(source, message)
            MethodInfo v2Cache = null;
            if (TryResolveMethod(methodName, new[] { typeof(string), typeof(string) }, ref v2Cache, out var v2Method))
            {
                v2Method.Invoke(null, new object[] { source, message });
                return;
            }

            // Try v1 API: LogX(message)
            MethodInfo v1Cache = null;
            if (TryResolveMethod(methodName, new[] { typeof(string) }, ref v1Cache, out var v1Method))
            {
                v1Method.Invoke(null, new object[] { message });
            }
        }
        catch
        {
            // Swallow - VDebug is optional
        }
    }

    static bool TryResolveMethod(string methodName, Type[] parameterTypes, ref MethodInfo cache, out MethodInfo method)
    {
        if (cache != null)
        {
            method = cache;
            return true;
        }

        method = null;
        var assembly = FindDebugToolsAssembly();
        if (assembly == null) return false;

        var apiType = assembly.GetType(DebugToolsApiTypeName, throwOnError: false);
        if (apiType == null) return false;

        method = apiType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, parameterTypes, null);
        if (method == null) return false;

        cache = method;
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Assembly FindDebugToolsAssembly()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                if (assembly?.GetName()?.Name == DebugToolsAssemblyName)
                    return assembly;
            }
            catch { }
        }
        return null;
    }
}
