using Eclipse.Services.HUD.Interfaces;
using Eclipse.Services.HUD.Shared;
using ProjectM.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Services;

namespace Eclipse.Services.HUD;

/// <summary>
/// Orchestrates all HUD components, managing their lifecycle and updates.
/// </summary>
internal class HudOrchestrator
{
    private readonly Dictionary<string, IHudComponent> _components = new();
    private readonly List<IHudComponent> _updateOrder = [];
    private UICanvasBase _canvas;
    private bool _initialized;

    /// <summary>
    /// Singleton instance of the HudOrchestrator.
    /// </summary>
    public static HudOrchestrator Instance { get; private set; }

    /// <summary>
    /// Whether the orchestrator has been initialized.
    /// </summary>
    public bool IsInitialized => _initialized;

    /// <summary>
    /// Creates a new HudOrchestrator with the given canvas.
    /// </summary>
    public HudOrchestrator(UICanvasBase canvas)
    {
        Instance = this;
        _canvas = canvas;
        HudData.CanvasBase = canvas;
    }

    /// <summary>
    /// Registers a component with the orchestrator.
    /// </summary>
    public void RegisterComponent(IHudComponent component)
    {
        if (component == null) return;

        _components[component.ComponentId] = component;
        _updateOrder.Add(component);
    }

    /// <summary>
    /// Initializes all registered components.
    /// </summary>
    public void InitializeComponents()
    {
        foreach (var component in _updateOrder.Where(c => c.IsEnabled))
        {
            try
            {
                component.Initialize(_canvas);
            }
            catch (Exception e)
            {
                DebugToolsBridge.TryLogError($"Failed to initialize {component.ComponentId}: {e}");
            }
        }
        _initialized = true;
        HudData.IsReady = true;
    }

    /// <summary>
    /// Updates all active and visible components.
    /// </summary>
    public void Update()
    {
        if (!_initialized || !HudData.IsReady || !HudData.IsActive) return;

        foreach (var component in _updateOrder.Where(c => c.IsEnabled && c.IsVisible && c.IsReady))
        {
            try
            {
                component.Update();
            }
            catch (Exception e)
            {
                DebugToolsBridge.TryLogError($"Error updating {component.ComponentId}: {e}");
            }
        }
    }

    /// <summary>
    /// Resets all components and clears registration.
    /// </summary>
    public void Reset()
    {
        foreach (var component in _updateOrder)
        {
            try
            {
                component.Reset();
            }
            catch (Exception e)
            {
                DebugToolsBridge.TryLogError($"Error resetting {component.ComponentId}: {e}");
            }
        }

        _components.Clear();
        _updateOrder.Clear();
        _initialized = false;
        HudData.IsReady = false;
    }

    /// <summary>
    /// Gets a component by its ID.
    /// </summary>
    public T GetComponent<T>(string componentId) where T : class, IHudComponent
    {
        return _components.TryGetValue(componentId, out var component) ? component as T : null;
    }

    /// <summary>
    /// Gets a component by its type.
    /// </summary>
    public T GetComponent<T>() where T : class, IHudComponent
    {
        return _updateOrder.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Toggles the visibility of a component by ID.
    /// </summary>
    public void ToggleComponent(string componentId)
    {
        if (_components.TryGetValue(componentId, out var component))
        {
            component.Toggle();
        }
    }

    /// <summary>
    /// Toggles the visibility of all components.
    /// </summary>
    public void ToggleAll()
    {
        foreach (var component in _updateOrder)
        {
            component.Toggle();
        }
    }

    /// <summary>
    /// Notifies all components of an input device change.
    /// </summary>
    public void OnInputDeviceChanged(bool isGamepad)
    {
        foreach (var component in _updateOrder)
        {
            component.OnInputDeviceChanged(isGamepad);
        }
    }

    /// <summary>
    /// Gets all registered component IDs.
    /// </summary>
    public IEnumerable<string> GetComponentIds()
    {
        return _components.Keys;
    }

    /// <summary>
    /// Checks if a component is registered.
    /// </summary>
    public bool HasComponent(string componentId)
    {
        return _components.ContainsKey(componentId);
    }
}
