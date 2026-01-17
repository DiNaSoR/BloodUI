using Eclipse.Services.HUD.Interfaces;
using Eclipse.Services.HUD.Shared;
using ProjectM.UI;
using UnityEngine;

namespace Eclipse.Services.HUD.Base;

/// <summary>
/// Abstract base class for all HUD components.
/// Provides common functionality and lifecycle management.
/// </summary>
internal abstract class HudComponentBase : IHudComponent
{
    protected UICanvasBase CanvasBase { get; private set; }
    protected Canvas BottomBarCanvas { get; private set; }
    protected int Layer { get; private set; }

    public abstract string ComponentId { get; }
    public virtual bool IsEnabled => true;
    public bool IsVisible { get; set; } = true;
    public bool IsReady { get; protected set; }

    public virtual void Initialize(UICanvasBase canvas)
    {
        CanvasBase = canvas;
        BottomBarCanvas = canvas.BottomBarParent.gameObject.GetComponent<Canvas>();
        Layer = BottomBarCanvas.gameObject.layer;
    }

    public abstract void Update();

    public virtual void Reset()
    {
        IsReady = false;
    }

    public virtual void Toggle()
    {
        IsVisible = !IsVisible;
        OnVisibilityChanged(IsVisible);
    }

    public virtual void OnInputDeviceChanged(bool isGamepad)
    {
        // Override in derived classes if needed
    }

    /// <summary>
    /// Called when visibility changes. Override to handle visibility updates.
    /// </summary>
    protected virtual void OnVisibilityChanged(bool visible)
    {
        // Override in derived classes to show/hide UI elements
    }

    /// <summary>
    /// Safely destroys a GameObject if it exists.
    /// </summary>
    protected static void SafeDestroy(ref GameObject obj)
    {
        if (obj != null)
        {
            UnityEngine.Object.Destroy(obj);
            obj = null;
        }
    }
}
