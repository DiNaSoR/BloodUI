using System;
using VDebug.Services;

namespace VDebug;

/// <summary>
/// Public static API surface that other plugins can call via reflection.
/// Keep this type name stable.
/// </summary>
public static class VDebugApi
{
    /// <summary>
    /// API version. Increment when breaking changes are made to the public surface.
    /// v3: Added source/category/context overloads and structured logging.
    /// </summary>
    public const int ApiVersion = 3;

    #region Logging - Simple (legacy compatible)

    /// <summary>
    /// Log an info message (no source/category).
    /// </summary>
    public static void LogInfo(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        StructuredLogging.Log(LogLevel.Info, message);
    }

    /// <summary>
    /// Log a warning message (no source/category).
    /// </summary>
    public static void LogWarning(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        StructuredLogging.Log(LogLevel.Warning, message);
    }

    /// <summary>
    /// Log an error message (no source/category).
    /// </summary>
    public static void LogError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        StructuredLogging.Log(LogLevel.Error, message);
    }

    #endregion

    #region Logging - Source only (v2 compatible)

    /// <summary>
    /// Log an info message with source identifier.
    /// </summary>
    /// <param name="source">Source identifier (e.g., "Client", "Server").</param>
    /// <param name="message">The log message.</param>
    public static void LogInfo(string source, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        StructuredLogging.Log(source, LogLevel.Info, message);
    }

    /// <summary>
    /// Log a warning message with source identifier.
    /// </summary>
    public static void LogWarning(string source, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        StructuredLogging.Log(source, LogLevel.Warning, message);
    }

    /// <summary>
    /// Log an error message with source identifier.
    /// </summary>
    public static void LogError(string source, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        StructuredLogging.Log(source, LogLevel.Error, message);
    }

    #endregion

    #region Logging - Source + Category (v3, recommended)

    /// <summary>
    /// Log an info message with source and category.
    /// </summary>
    /// <param name="source">Source identifier (e.g., "Client", "Server").</param>
    /// <param name="category">Subsystem/category (e.g., "Layout", "EclipseSync").</param>
    /// <param name="message">The log message.</param>
    public static void LogInfo(string source, string category, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        StructuredLogging.Log(source, category, LogLevel.Info, message);
    }

    /// <summary>
    /// Log a warning message with source and category.
    /// </summary>
    public static void LogWarning(string source, string category, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        StructuredLogging.Log(source, category, LogLevel.Warning, message);
    }

    /// <summary>
    /// Log an error message with source and category.
    /// </summary>
    public static void LogError(string source, string category, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        StructuredLogging.Log(source, category, LogLevel.Error, message);
    }

    #endregion

    #region Logging - Full structured (v3, with context)

    /// <summary>
    /// Log a fully structured message with source, category, and key-value context.
    /// </summary>
    /// <param name="source">Source identifier (e.g., "Client", "Server").</param>
    /// <param name="category">Subsystem/category (e.g., "Layout", "EclipseSync").</param>
    /// <param name="level">Log level (Info, Warning, Error).</param>
    /// <param name="message">The log message.</param>
    /// <param name="context">Optional key-value pairs for structured context.</param>
    public static void Log(string source, string category, LogLevel level, string message, params (string Key, string Value)[] context)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        StructuredLogging.Log(source, category, level, message, context);
    }

    #endregion

    #region Asset Dumping

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

    public static void DumpCharacterMenu()
    {
        try
        {
            AssetDumpService.DumpCharacterMenu();
        }
        catch (Exception ex)
        {
            VDebugLog.Log.LogWarning($"[VDebugApi] DumpCharacterMenu failed: {ex}");
        }
    }

    public static void DumpHudMenu()
    {
        try
        {
            AssetDumpService.DumpHudMenu();
        }
        catch (Exception ex)
        {
            VDebugLog.Log.LogWarning($"[VDebugApi] DumpHudMenu failed: {ex}");
        }
    }

    public static void DumpMainMenu()
    {
        try
        {
            AssetDumpService.DumpMainMenu();
        }
        catch (Exception ex)
        {
            VDebugLog.Log.LogWarning($"[VDebugApi] DumpMainMenu failed: {ex}");
        }
    }

    #endregion

    #region Debug Panel Control

    /// <summary>
    /// Show the debug panel. Initializes it if not already created.
    /// </summary>
    public static void ShowDebugPanel()
    {
        try
        {
            DebugPanelService.Initialize();
            DebugPanelService.ShowPanel();
        }
        catch (Exception ex)
        {
            VDebugLog.Log.LogWarning($"[VDebugApi] ShowDebugPanel failed: {ex}");
        }
    }

    /// <summary>
    /// Hide the debug panel.
    /// </summary>
    public static void HideDebugPanel()
    {
        try
        {
            DebugPanelService.HidePanel();
        }
        catch (Exception ex)
        {
            VDebugLog.Log.LogWarning($"[VDebugApi] HideDebugPanel failed: {ex}");
        }
    }

    /// <summary>
    /// Toggle debug panel visibility.
    /// </summary>
    public static void ToggleDebugPanel()
    {
        try
        {
            DebugPanelService.Initialize();
            DebugPanelService.TogglePanel();
        }
        catch (Exception ex)
        {
            VDebugLog.Log.LogWarning($"[VDebugApi] ToggleDebugPanel failed: {ex}");
        }
    }

    #endregion
}
