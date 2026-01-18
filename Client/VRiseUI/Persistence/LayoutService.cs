using System.IO;
using System.Text.Json;
using UnityEngine;

namespace VRiseUI.Persistence;

/// <summary>
/// Service for saving and loading UI element positions.
/// Allows users to customize their HUD layout.
/// </summary>
public static class LayoutService
{
    private static string _layoutsPath;
    private static Dictionary<string, LayoutData> _layouts = new();
    private static bool _initialized;

    /// <summary>
    /// Initialize the layout service.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        _layoutsPath = Path.Combine(
            Path.GetDirectoryName(typeof(Plugin).Assembly.Location),
            "Layouts"
        );

        if (!Directory.Exists(_layoutsPath))
        {
            Directory.CreateDirectory(_layoutsPath);
        }

        LoadAllLayouts();
        _initialized = true;

        Plugin.Log.LogInfo($"Layout service initialized. Path: {_layoutsPath}");
    }

    /// <summary>
    /// Save an element's position.
    /// </summary>
    public static void SavePosition(string adapterId, string elementId, Vector2 position)
    {
        var layout = GetOrCreateLayout(adapterId);
        layout.Positions[elementId] = new PositionData
        {
            X = position.x,
            Y = position.y
        };

        SaveLayout(adapterId, layout);
    }

    /// <summary>
    /// Save an element's size.
    /// </summary>
    public static void SaveSize(string adapterId, string elementId, Vector2 size)
    {
        var layout = GetOrCreateLayout(adapterId);
        layout.Sizes[elementId] = new SizeData
        {
            Width = size.x,
            Height = size.y
        };

        SaveLayout(adapterId, layout);
    }

    /// <summary>
    /// Try to get a saved position for an element.
    /// </summary>
    public static bool TryGetPosition(string adapterId, string elementId, out Vector2 position)
    {
        position = Vector2.zero;

        if (!_layouts.TryGetValue(adapterId, out var layout))
            return false;

        if (!layout.Positions.TryGetValue(elementId, out var posData))
            return false;

        position = new Vector2(posData.X, posData.Y);
        return true;
    }

    /// <summary>
    /// Try to get a saved size for an element.
    /// </summary>
    public static bool TryGetSize(string adapterId, string elementId, out Vector2 size)
    {
        size = Vector2.zero;

        if (!_layouts.TryGetValue(adapterId, out var layout))
            return false;

        if (!layout.Sizes.TryGetValue(elementId, out var sizeData))
            return false;

        size = new Vector2(sizeData.Width, sizeData.Height);
        return true;
    }

    /// <summary>
    /// Reset layout for an adapter to defaults.
    /// </summary>
    public static void ResetLayout(string adapterId)
    {
        if (_layouts.ContainsKey(adapterId))
        {
            _layouts.Remove(adapterId);
        }

        var layoutFile = Path.Combine(_layoutsPath, $"{adapterId}.layout.json");
        if (File.Exists(layoutFile))
        {
            File.Delete(layoutFile);
        }

        Plugin.Log.LogInfo($"Reset layout for adapter: {adapterId}");
    }

    private static LayoutData GetOrCreateLayout(string adapterId)
    {
        if (!_layouts.TryGetValue(adapterId, out var layout))
        {
            layout = new LayoutData
            {
                AdapterId = adapterId,
                Positions = new Dictionary<string, PositionData>(),
                Sizes = new Dictionary<string, SizeData>()
            };
            _layouts[adapterId] = layout;
        }
        return layout;
    }

    private static void SaveLayout(string adapterId, LayoutData layout)
    {
        try
        {
            var layoutFile = Path.Combine(_layoutsPath, $"{adapterId}.layout.json");
            var json = JsonSerializer.Serialize(layout, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(layoutFile, json);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Failed to save layout for {adapterId}: {ex.Message}");
        }
    }

    private static void LoadAllLayouts()
    {
        if (!Directory.Exists(_layoutsPath)) return;

        foreach (var file in Directory.GetFiles(_layoutsPath, "*.layout.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var layout = JsonSerializer.Deserialize<LayoutData>(json);
                if (layout != null && !string.IsNullOrEmpty(layout.AdapterId))
                {
                    _layouts[layout.AdapterId] = layout;
                    Plugin.Log.LogInfo($"Loaded layout: {layout.AdapterId}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to load layout from {file}: {ex.Message}");
            }
        }
    }
}

/// <summary>
/// Layout data for an adapter.
/// </summary>
public class LayoutData
{
    public string AdapterId { get; set; }
    public Dictionary<string, PositionData> Positions { get; set; } = new();
    public Dictionary<string, SizeData> Sizes { get; set; } = new();
}

/// <summary>
/// Position data for an element.
/// </summary>
public class PositionData
{
    public float X { get; set; }
    public float Y { get; set; }
}

/// <summary>
/// Size data for an element.
/// </summary>
public class SizeData
{
    public float Width { get; set; }
    public float Height { get; set; }
}
