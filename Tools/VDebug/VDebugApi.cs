using System;
using VDebug.Services;

namespace VDebug;

/// <summary>
/// Public static API surface that other plugins can call via reflection.
/// Keep this type name stable.
/// </summary>
public static class VDebugApi
{
    public const int ApiVersion = 1;

    public static void LogInfo(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        VDebugLog.Log.LogInfo(message);
    }

    public static void LogWarning(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        VDebugLog.Log.LogWarning(message);
    }

    public static void LogError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        VDebugLog.Log.LogError(message);
    }

    public static void DumpMenuAssets()
    {
        try
        {
            AssetDumpService.DumpMenuAssets();
        }
        catch (Exception ex)
        {
            VDebugLog.Log.LogWarning($"[VDebugApi] DumpMenuAssets failed: {ex}");
        }
    }
}

