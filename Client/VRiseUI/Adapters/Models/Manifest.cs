namespace VRiseUI.Adapters.Models;

/// <summary>
/// Manifest model - defines the UI configuration for a mod adapter.
/// Loaded from manifest.json in each adapter folder.
/// </summary>
public class Manifest
{
    /// <summary>
    /// Manifest format version.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// The mod this adapter is for (e.g., "BloodCraft").
    /// </summary>
    public string ModId { get; set; }

    /// <summary>
    /// Compatible mod version pattern (e.g., "1.12.x").
    /// </summary>
    public string ModVersion { get; set; }

    /// <summary>
    /// Author of this adapter.
    /// </summary>
    public string Author { get; set; }

    /// <summary>
    /// Display name shown in UI.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Description of what this adapter provides.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// How to detect if the mod is installed.
    /// </summary>
    public DetectionConfig Detection { get; set; }

    /// <summary>
    /// HUD element definitions.
    /// </summary>
    public HudConfig Hud { get; set; }

    /// <summary>
    /// Menu definitions.
    /// </summary>
    public List<MenuDef> Menus { get; set; }

    /// <summary>
    /// Notification pattern definitions.
    /// </summary>
    public NotificationConfig Notifications { get; set; }
}

/// <summary>
/// Configuration for detecting if a mod is installed.
/// </summary>
public class DetectionConfig
{
    /// <summary>
    /// Detection method: "command" or "assembly".
    /// </summary>
    public string Method { get; set; } = "command";

    /// <summary>
    /// Command to send for detection.
    /// </summary>
    public string Command { get; set; }

    /// <summary>
    /// Expected response pattern.
    /// </summary>
    public string ExpectedResponse { get; set; }
}

/// <summary>
/// HUD configuration.
/// </summary>
public class HudConfig
{
    /// <summary>
    /// List of HUD elements.
    /// </summary>
    public List<HudElementDef> Elements { get; set; } = new();
}

/// <summary>
/// Definition for a single HUD element.
/// </summary>
public class HudElementDef
{
    /// <summary>
    /// Unique ID for this element.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Element type: progressBar, label, button, panel, etc.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Display label.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Position configuration.
    /// </summary>
    public PositionDef Position { get; set; }

    /// <summary>
    /// Size configuration.
    /// </summary>
    public SizeDef Size { get; set; }

    /// <summary>
    /// Style configuration.
    /// </summary>
    public StyleDef Style { get; set; }

    /// <summary>
    /// Icon path relative to adapter assets folder.
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// Data source configuration.
    /// </summary>
    public DataSourceDef DataSource { get; set; }
}

/// <summary>
/// Position definition.
/// </summary>
public class PositionDef
{
    public float X { get; set; }
    public float Y { get; set; }
    public string Anchor { get; set; } = "topLeft";
}

/// <summary>
/// Size definition.
/// </summary>
public class SizeDef
{
    public float Width { get; set; }
    public float Height { get; set; }
}

/// <summary>
/// Style definition.
/// </summary>
public class StyleDef
{
    public string BackgroundColor { get; set; }
    public string ForegroundColor { get; set; }
    public string BorderColor { get; set; }
    public float BorderWidth { get; set; }
    public float CornerRadius { get; set; }
    public float FontSize { get; set; }
}

/// <summary>
/// Data source definition for binding UI to mod data.
/// </summary>
public class DataSourceDef
{
    /// <summary>
    /// Chat command to send to get data.
    /// </summary>
    public string Command { get; set; }

    /// <summary>
    /// Regex pattern to parse response.
    /// </summary>
    public string Pattern { get; set; }

    /// <summary>
    /// Mapping of regex groups to data fields.
    /// </summary>
    public Dictionary<string, string> Mapping { get; set; }

    /// <summary>
    /// How often to refresh data (milliseconds). 0 = manual only.
    /// </summary>
    public int RefreshInterval { get; set; }

    /// <summary>
    /// How long to cache responses (milliseconds).
    /// </summary>
    public int CacheTime { get; set; } = 1000;
}

/// <summary>
/// Menu definition.
/// </summary>
public class MenuDef
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Title { get; set; }
    public string Hotkey { get; set; }
    public PositionDef Position { get; set; }
    public SizeDef Size { get; set; }
    public List<TabDef> Tabs { get; set; }
}

/// <summary>
/// Tab definition for menus.
/// </summary>
public class TabDef
{
    public string Id { get; set; }
    public string Label { get; set; }
    public ContentDef Content { get; set; }
}

/// <summary>
/// Content definition for tabs.
/// </summary>
public class ContentDef
{
    public string Type { get; set; }
    public DataSourceDef DataSource { get; set; }
    public List<ActionDef> Actions { get; set; }
}

/// <summary>
/// Action definition (button clicks, etc).
/// </summary>
public class ActionDef
{
    public string Label { get; set; }
    public string Command { get; set; }
    public string Icon { get; set; }
}

/// <summary>
/// Notification configuration.
/// </summary>
public class NotificationConfig
{
    public List<NotificationPattern> Patterns { get; set; } = new();
}

/// <summary>
/// Pattern for matching and displaying notifications.
/// </summary>
public class NotificationPattern
{
    public string Match { get; set; }
    public string Style { get; set; }
    public string Sound { get; set; }
}
