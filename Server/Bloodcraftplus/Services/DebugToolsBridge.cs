using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bloodcraft.Services;

/// <summary>
/// Optional integration point for the external VDebug plugin.
/// If VDebug is not installed, calls fall back to Core.Log.
/// </summary>
internal static class DebugToolsBridge
{
    const string DebugToolsAssemblyName = "VDebug";
    const string DebugToolsApiTypeName = "VDebug.VDebugApi";

    // Structured logging convention: source identifies client/server, category identifies subsystem.
    const string Source = "Server";

    static MethodInfo _logInfo;
    static MethodInfo _logWarning;
    static MethodInfo _logError;

    /// <summary>
    /// Log an info message via VDebug (if installed), or fall back to Core.Log.
    /// </summary>
    public static void TryLogInfo(string message) => TryLogInfo(null, message);

    /// <summary>
    /// Log an info message with category via VDebug (if installed), or fall back to Core.Log.
    /// </summary>
    public static void TryLogInfo(string category, string message)
    {
        TryLogWithSourceCategory("LogInfo", message, Source, category, ref _logInfo, fallback: () => Core.Log.LogInfo(message));
    }

    /// <summary>
    /// Log a warning message via VDebug (if installed), or fall back to Core.Log.
    /// </summary>
    public static void TryLogWarning(string message) => TryLogWarning(null, message);

    /// <summary>
    /// Log a warning message with category via VDebug (if installed), or fall back to Core.Log.
    /// </summary>
    public static void TryLogWarning(string category, string message)
    {
        TryLogWithSourceCategory("LogWarning", message, Source, category, ref _logWarning, fallback: () => Core.Log.LogWarning(message));
    }

    /// <summary>
    /// Log an error message via VDebug (if installed), or fall back to Core.Log.
    /// </summary>
    public static void TryLogError(string message) => TryLogError(null, message);

    /// <summary>
    /// Log an error message with category via VDebug (if installed), or fall back to Core.Log.
    /// </summary>
    public static void TryLogError(string category, string message)
    {
        TryLogWithSourceCategory("LogError", message, Source, category, ref _logError, fallback: () => Core.Log.LogError(message));
    }

    static void TryLogWithSourceCategory(string methodName, string message, string source, string category, ref MethodInfo cache, Action fallback)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        // Prefer v3 API: LogX(source, category, message)
        if (TryResolveStaticMethod(methodName, new[] { typeof(string), typeof(string), typeof(string) }, ref cache, out MethodInfo method))
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
        if (TryResolveStaticMethod(methodName, new[] { typeof(string), typeof(string) }, ref v2Cache, out MethodInfo v2Method))
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
        if (TryResolveStaticMethod(methodName, new[] { typeof(string) }, ref legacy, out MethodInfo legacyMethod))
        {
            try
            {
                legacyMethod.Invoke(null, new object[] { message });
                return;
            }
            catch
            {
                // Fall through to built-in.
            }
        }

        // Final fallback to Core.Log
        fallback?.Invoke();
    }

    static bool TryResolveStaticMethod(string methodName, Type[] parameterTypes, ref MethodInfo cache, out MethodInfo method)
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
}

