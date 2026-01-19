using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BloodUI.Services;

/// <summary>
/// Optional integration point for the external VDebug plugin.
/// If the debug plugin is not installed, calls are no-ops (and do not error).
/// </summary>
internal static class DebugToolsBridge
{
    const string DebugToolsAssemblyName = "VDebug";
    const string DebugToolsApiTypeName = "VDebug.VDebugApi";

    // Structured logging convention: source identifies client/server, category identifies subsystem.
    const string Source = "Client";

    static bool _loggedMissing;
    static MethodInfo _dumpMenuAssets;
    static MethodInfo _logInfo;
    static MethodInfo _logWarning;
    static MethodInfo _logError;

    public static void TryDumpMenuAssets()
    {
        if (!TryResolveStaticMethod(nameof(TryDumpMenuAssets), "DumpMenuAssets", ref _dumpMenuAssets, out MethodInfo method))
        {
            LogMissingOnce();
            return;
        }

        try
        {
            method.Invoke(null, null);
        }
        catch (Exception ex)
        {
            TryLogWarning($"[VDebug] Failed invoking DumpMenuAssets: {ex.Message}");
        }
    }

    /// <summary>
    /// Log an info message via VDebug (if installed).
    /// </summary>
    public static void TryLogInfo(string message) => TryLogInfo(null, message);

    /// <summary>
    /// Log an info message with category via VDebug (if installed).
    /// </summary>
    public static void TryLogInfo(string category, string message)
    {
        TryLogWithSourceCategory(nameof(TryLogInfo), "LogInfo", message, Source, category, ref _logInfo);
    }

    /// <summary>
    /// Log a warning message via VDebug (if installed).
    /// </summary>
    public static void TryLogWarning(string message) => TryLogWarning(null, message);

    /// <summary>
    /// Log a warning message with category via VDebug (if installed).
    /// </summary>
    public static void TryLogWarning(string category, string message)
    {
        TryLogWithSourceCategory(nameof(TryLogWarning), "LogWarning", message, Source, category, ref _logWarning);
    }

    /// <summary>
    /// Log an error message via VDebug (if installed).
    /// </summary>
    public static void TryLogError(string message) => TryLogError(null, message);

    /// <summary>
    /// Log an error message with category via VDebug (if installed).
    /// </summary>
    public static void TryLogError(string category, string message)
    {
        TryLogWithSourceCategory(nameof(TryLogError), "LogError", message, Source, category, ref _logError);
    }

    static MethodInfo _togglePanel;

    /// <summary>
    /// Toggle the VDebug panel visibility via reflection.
    /// </summary>
    public static void TryToggleDebugPanel()
    {
        if (!TryResolveStaticMethod(nameof(TryToggleDebugPanel), "ToggleDebugPanel", ref _togglePanel, out MethodInfo method))
        {
            LogMissingOnce();
            return;
        }

        try
        {
            method.Invoke(null, null);
        }
        catch (Exception ex)
        {
            TryLogWarning($"[VDebug] Failed invoking ToggleDebugPanel: {ex.Message}");
        }
    }

    static bool TryResolveStaticMethod(string callSite, string methodName, ref MethodInfo cache, out MethodInfo method)
        => TryResolveStaticMethod(callSite, methodName, Type.EmptyTypes, ref cache, out method, logFailures: true);

    static void TryLogWithSourceCategory(string callSite, string methodName, string message, string source, string category, ref MethodInfo cache)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        // Prefer v3 API: LogX(source, category, message)
        if (TryResolveStaticMethod(callSite, methodName, new[] { typeof(string), typeof(string), typeof(string) }, ref cache, out MethodInfo method, logFailures: false))
        {
            try
            {
                method.Invoke(null, new object[] { source, category, message });
                return;
            }
            catch
            {
                // Fall through to older APIs.
            }
        }

        // Fall back to v2 API: LogX(source, message)
        MethodInfo v2Cache = null;
        if (TryResolveStaticMethod(callSite, methodName, new[] { typeof(string), typeof(string) }, ref v2Cache, out MethodInfo v2Method, logFailures: false))
        {
            try
            {
                v2Method.Invoke(null, new object[] { source, message });
                return;
            }
            catch
            {
                // Fall through to legacy.
            }
        }

        // Fall back to v1 API: LogX(message)
        MethodInfo legacy = null;
        if (TryResolveStaticMethod(callSite, methodName, new[] { typeof(string) }, ref legacy, out MethodInfo legacyMethod, logFailures: false))
        {
            try
            {
                legacyMethod.Invoke(null, new object[] { message });
            }
            catch
            {
                // Swallow logging failures to keep VDebug optional and silent.
            }
        }
    }

    static bool TryResolveStaticMethod(string callSite, string methodName, Type[] parameterTypes, ref MethodInfo cache, out MethodInfo method, bool logFailures)
    {
        if (cache != null)
        {
            method = cache;
            return true;
        }

        method = null;

        Assembly apiAssembly = FindDebugToolsAssembly();
        if (apiAssembly == null)
        {
            return false;
        }

        Type apiType = apiAssembly.GetType(DebugToolsApiTypeName, throwOnError: false);
        if (apiType == null)
        {
            return false;
        }

        method = apiType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, parameterTypes, null);
        if (method == null)
        {
            return false;
        }

        cache = method;
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Assembly FindDebugToolsAssembly()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        for (int i = 0; i < assemblies.Length; i++)
        {
            Assembly assembly = assemblies[i];
            if (assembly == null)
            {
                continue;
            }

            AssemblyName name;
            try
            {
                name = assembly.GetName();
            }
            catch
            {
                continue;
            }

            if (name != null && string.Equals(name.Name, DebugToolsAssemblyName, StringComparison.Ordinal))
            {
                return assembly;
            }
        }

        for (int i = 0; i < assemblies.Length; i++)
        {
            Assembly assembly = assemblies[i];
            if (assembly == null)
            {
                continue;
            }

            try
            {
                if (assembly.GetType(DebugToolsApiTypeName, throwOnError: false) != null)
                {
                    return assembly;
                }
            }
            catch
            {
                continue;
            }
        }

        return null;
    }

    static void LogMissingOnce()
    {
        if (_loggedMissing)
        {
            return;
        }

        _loggedMissing = true;
    }
}
