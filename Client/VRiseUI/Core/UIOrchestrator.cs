using VRiseUI.UI.Interfaces;

namespace VRiseUI.Core;

/// <summary>
/// Orchestrates all UI components, managing their lifecycle and updates.
/// </summary>
public static class UIOrchestrator
{
    private static readonly Dictionary<string, IUIComponent> _components = new();
    private static readonly List<IUIComponent> _updateOrder = new();
    private static bool _initialized;

    /// <summary>
    /// Whether the orchestrator has been initialized.
    /// </summary>
    public static bool IsInitialized => _initialized;

    /// <summary>
    /// Number of registered components.
    /// </summary>
    public static int ComponentCount => _components.Count;

    /// <summary>
    /// Initialize the UI orchestrator.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        Plugin.Log.LogInfo("UI Orchestrator initialized.");
    }

    /// <summary>
    /// Shutdown the orchestrator and all components.
    /// </summary>
    public static void Shutdown()
    {
        foreach (var component in _updateOrder)
        {
            try
            {
                component.Destroy();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error destroying {component.ComponentId}: {ex.Message}");
            }
        }

        _components.Clear();
        _updateOrder.Clear();
        _initialized = false;
    }

    /// <summary>
    /// Register a component with the orchestrator.
    /// </summary>
    public static void RegisterComponent(IUIComponent component)
    {
        if (component == null) return;

        _components[component.ComponentId] = component;
        _updateOrder.Add(component);
        Plugin.Log.LogInfo($"Registered component: {component.ComponentId}");
    }

    /// <summary>
    /// Unregister a component.
    /// </summary>
    public static void UnregisterComponent(string componentId)
    {
        if (_components.TryGetValue(componentId, out var component))
        {
            _components.Remove(componentId);
            _updateOrder.Remove(component);
        }
    }

    /// <summary>
    /// Initialize all registered components.
    /// </summary>
    public static void InitializeComponents()
    {
        foreach (var component in _updateOrder.Where(c => c.IsEnabled))
        {
            try
            {
                component.Initialize();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to initialize {component.ComponentId}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Update all active and visible components.
    /// Called from the game's update loop.
    /// </summary>
    public static void Update()
    {
        if (!_initialized) return;

        foreach (var component in _updateOrder.Where(c => c.IsEnabled && c.IsVisible && c.IsReady))
        {
            try
            {
                component.Update();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error updating {component.ComponentId}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Get a component by ID.
    /// </summary>
    public static T GetComponent<T>(string componentId) where T : class, IUIComponent
    {
        return _components.TryGetValue(componentId, out var component) ? component as T : null;
    }

    /// <summary>
    /// Get a component by type.
    /// </summary>
    public static T GetComponent<T>() where T : class, IUIComponent
    {
        return _updateOrder.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Toggle visibility of a component.
    /// </summary>
    public static void ToggleComponent(string componentId)
    {
        if (_components.TryGetValue(componentId, out var component))
        {
            component.Toggle();
        }
    }
}
