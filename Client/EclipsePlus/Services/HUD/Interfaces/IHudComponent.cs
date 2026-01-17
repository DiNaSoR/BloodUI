using ProjectM.UI;

namespace Eclipse.Services.HUD.Interfaces;

/// <summary>
/// Base interface for all HUD components.
/// Defines the lifecycle methods and properties that all HUD elements must implement.
/// </summary>
internal interface IHudComponent
{
    /// <summary>
    /// Unique identifier for this component.
    /// </summary>
    string ComponentId { get; }

    /// <summary>
    /// Whether this component is enabled based on configuration.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Whether this component is currently visible.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Whether this component has been initialized and is ready for updates.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Initialize the component with the canvas reference.
    /// </summary>
    /// <param name="canvas">The UI canvas base to attach elements to.</param>
    void Initialize(UICanvasBase canvas);

    /// <summary>
    /// Update the component's display state.
    /// Called each frame when the component is active and visible.
    /// </summary>
    void Update();

    /// <summary>
    /// Reset the component to its initial state, destroying any created UI elements.
    /// </summary>
    void Reset();

    /// <summary>
    /// Toggle the visibility of this component.
    /// </summary>
    void Toggle();

    /// <summary>
    /// Called when the input device changes between keyboard and gamepad.
    /// </summary>
    /// <param name="isGamepad">True if switching to gamepad, false for keyboard.</param>
    void OnInputDeviceChanged(bool isGamepad);
}
