using VRiseUI.Adapters.Models;
using VRiseUI.UI.Components;
using VRiseUI.UI.Interfaces;

namespace VRiseUI.Core;

/// <summary>
/// Factory for creating UI components from manifest definitions.
/// Uses registration pattern - no switch statements.
/// </summary>
public static class UIComponentFactory
{
    private static readonly Dictionary<string, Func<Adapter, HudElementDef, IUIComponent>> _creators = new();

    static UIComponentFactory()
    {
        // Register all component types
        Register("progressBar", (adapter, def) => new ProgressBar(adapter, def));
        Register("label", (adapter, def) => new Label(adapter, def));
        Register("button", (adapter, def) => new Button(adapter, def));
        Register("panel", (adapter, def) => new Panel(adapter, def));
        // Add more component types as needed
    }

    /// <summary>
    /// Register a component type creator.
    /// </summary>
    public static void Register(string type, Func<Adapter, HudElementDef, IUIComponent> creator)
    {
        _creators[type.ToLowerInvariant()] = creator;
    }

    /// <summary>
    /// Create a component from its definition.
    /// </summary>
    public static IUIComponent Create(Adapter adapter, HudElementDef def)
    {
        var type = def.Type?.ToLowerInvariant() ?? "";

        if (!_creators.TryGetValue(type, out var creator))
        {
            Plugin.Log.LogWarning($"Unknown component type: {def.Type}");
            return null;
        }

        try
        {
            return creator(adapter, def);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Failed to create component {def.Id}: {ex.Message}");
            return null;
        }
    }
}
