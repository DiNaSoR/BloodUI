using System;
using System.Collections.Concurrent;
using System.Threading;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace VDebug.Services;

/// <summary>
/// Log level for structured logging.
/// </summary>
public enum LogLevel
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Provides structured, machine-parseable log formatting with optional repeat suppression.
/// All VDebug logs are funneled through this single emission path.
/// </summary>
internal static class StructuredLogging
{
    #region Configuration

    internal static ConfigEntry<bool> RepeatSuppression;
    internal static ConfigEntry<int> RepeatWindowSeconds;
    internal static ConfigEntry<int> RepeatFlushSeconds;

    public static void BindConfig(ConfigFile config)
    {
        RepeatSuppression = config.Bind(
            "Logging", "RepeatSuppressionEnabled", true,
            "If true, repeated identical log messages will be suppressed and summarized."
        );
        RepeatWindowSeconds = config.Bind(
            "Logging", "RepeatWindowSeconds", 2,
            "Time window (seconds) for detecting repeated messages."
        );
        RepeatFlushSeconds = config.Bind(
            "Logging", "RepeatFlushSeconds", 10,
            "After this many seconds of suppression, emit a summary line showing repeat count."
        );
    }

    #endregion

    #region Repeat suppression state

    class RepeatEntry
    {
        public string FormattedMessage;
        public LogLevel Level;
        public int Count;
        public DateTime FirstSeen;
        public DateTime LastSeen;
    }

    static readonly ConcurrentDictionary<string, RepeatEntry> _repeatTracker = new();
    static Timer _flushTimer;

    public static void Initialize()
    {
        // Start a background timer to periodically flush suppressed repeats.
        _flushTimer ??= new Timer(FlushRepeats, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    static void FlushRepeats(object state)
    {
        try
        {
            var flushSeconds = RepeatFlushSeconds?.Value ?? 10;
            var now = DateTime.UtcNow;

            foreach (var kvp in _repeatTracker)
            {
                var entry = kvp.Value;
                if (entry.Count > 1 && (now - entry.FirstSeen).TotalSeconds >= flushSeconds)
                {
                    // Emit summary
                    var summary = $"[repeated x{entry.Count} in last {flushSeconds}s] {entry.FormattedMessage}";
                    EmitRaw(entry.Level, summary);
                    _repeatTracker.TryRemove(kvp.Key, out _);
                }
                else if (entry.Count <= 1 && (now - entry.LastSeen).TotalSeconds > flushSeconds * 2)
                {
                    // Stale entry, clean up
                    _repeatTracker.TryRemove(kvp.Key, out _);
                }
            }
        }
        catch
        {
            // Never crash the flush timer.
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Emit a structured log entry.
    /// Format: <c>2026-01-17T14:22:03.123Z [I] [Client] [Layout] Loaded layout path=D:\...</c>
    /// </summary>
    /// <param name="source">Source identifier (e.g., "Client", "Server").</param>
    /// <param name="category">Category/subsystem (e.g., "Layout", "EclipseSync").</param>
    /// <param name="level">Log severity.</param>
    /// <param name="message">The actual log message.</param>
    /// <param name="context">Optional key-value pairs for structured context.</param>
    public static void Log(string source, string category, LogLevel level, string message, params (string Key, string Value)[] context)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var formatted = FormatStructured(source, category, level, message, context);
        var suppressionKey = $"{source}|{category}|{level}|{message}";

        if (TrySuppressRepeat(suppressionKey, formatted, level))
        {
            return; // Suppressed
        }

        Emit(source, level, formatted);
    }

    /// <summary>
    /// Emit a log entry with source only (no category).
    /// </summary>
    public static void Log(string source, LogLevel level, string message)
    {
        Log(source, null, level, message);
    }

    /// <summary>
    /// Emit a simple log entry (no structured metadata).
    /// </summary>
    public static void Log(LogLevel level, string message)
    {
        Log(null, null, level, message);
    }

    #endregion

    #region Formatting

    static string FormatStructured(string source, string category, LogLevel level, string message, (string Key, string Value)[] context)
    {
        // Format: 2026-01-17T14:22:03.123Z [I] [Source] [Category] Message key=value ...
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var levelChar = level switch
        {
            LogLevel.Info => "I",
            LogLevel.Warning => "W",
            LogLevel.Error => "E",
            _ => "?"
        };

        var sb = new System.Text.StringBuilder();
        sb.Append(timestamp);
        sb.Append(" [");
        sb.Append(levelChar);
        sb.Append(']');

        if (!string.IsNullOrWhiteSpace(source))
        {
            sb.Append(" [");
            sb.Append(source);
            sb.Append(']');
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            sb.Append(" [");
            sb.Append(category);
            sb.Append(']');
        }

        sb.Append(' ');
        sb.Append(message);

        if (context != null && context.Length > 0)
        {
            foreach (var (key, value) in context)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    sb.Append(' ');
                    sb.Append(key);
                    sb.Append('=');
                    sb.Append(value ?? string.Empty);
                }
            }
        }

        return sb.ToString();
    }

    #endregion

    #region Emission

    static bool TrySuppressRepeat(string key, string formatted, LogLevel level)
    {
        if (RepeatSuppression == null || !RepeatSuppression.Value)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var windowSeconds = RepeatWindowSeconds?.Value ?? 2;

        if (_repeatTracker.TryGetValue(key, out var entry))
        {
            if ((now - entry.LastSeen).TotalSeconds <= windowSeconds)
            {
                // Within window - suppress
                entry.Count++;
                entry.LastSeen = now;
                return true;
            }
            else
            {
                // Window expired - flush old and emit new
                if (entry.Count > 1)
                {
                    var summary = $"[repeated x{entry.Count}] {entry.FormattedMessage}";
                    EmitRaw(entry.Level, summary);
                }
                _repeatTracker.TryRemove(key, out _);
            }
        }

        // First occurrence - track it
        _repeatTracker[key] = new RepeatEntry
        {
            FormattedMessage = formatted,
            Level = level,
            Count = 1,
            FirstSeen = now,
            LastSeen = now
        };

        return false;
    }

    static void Emit(string source, LogLevel level, string formatted)
    {
        // Apply ANSI coloring if enabled
        var output = ApplyAnsiColoring(source, level, formatted);
        EmitRaw(level, output);
    }

    static void EmitRaw(LogLevel level, string message)
    {
        ManualLogSource log = VDebugLog.Log;

        switch (level)
        {
            case LogLevel.Warning:
                log.LogWarning(message);
                break;
            case LogLevel.Error:
                log.LogError(message);
                break;
            default:
                log.LogInfo(message);
                break;
        }
    }

    static string ApplyAnsiColoring(string source, LogLevel level, string formatted)
    {
        try
        {
            if (Plugin.EnableAnsiColors != null && Plugin.EnableAnsiColors.Value && ConsoleAnsiSupport.IsEnabled)
            {
                var ansiLevel = level switch
                {
                    LogLevel.Warning => AnsiColors.VLevel.Warning,
                    LogLevel.Error => AnsiColors.VLevel.Error,
                    _ => AnsiColors.VLevel.Info
                };

                var prefix = AnsiColors.FormatPrefix(source);
                var body = AnsiColors.FormatMessage(ansiLevel, formatted);
                return $"{prefix} {body}";
            }
        }
        catch
        {
            // Fall through to plain output.
        }

        return formatted;
    }

    #endregion
}
