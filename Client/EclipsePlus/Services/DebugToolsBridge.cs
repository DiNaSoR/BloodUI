using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Eclipse.Services;

namespace Eclipse.Services;

/// <summary>
/// Optional integration point for the external VDebug plugin.
/// If the debug plugin is not installed, calls are no-ops (and do not error).
/// </summary>
internal static class DebugToolsBridge
{
    const string DebugToolsAssemblyName = "VDebug";
    const string DebugToolsApiTypeName = "VDebug.VDebugApi";

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

    public static void TryLogInfo(string message)
    {
        TryLog(nameof(TryLogInfo), "LogInfo", message, ref _logInfo);
    }

    public static void TryLogWarning(string message)
    {
        TryLog(nameof(TryLogWarning), "LogWarning", message, ref _logWarning);
    }

    public static void TryLogError(string message)
    {
        TryLog(nameof(TryLogError), "LogError", message, ref _logError);
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

    static void TryLog(string callSite, string methodName, string message, ref MethodInfo cache)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (!TryResolveStaticMethod(callSite, methodName, new[] { typeof(string) }, ref cache, out MethodInfo method, logFailures: false))
        {
            return;
        }

        try
        {
            method.Invoke(null, new object[] { message });
        }
        catch
        {
            // Swallow logging failures to keep VDebug optional and silent.
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
