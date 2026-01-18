namespace VRiseUI.UI.Interfaces;

/// <summary>
/// Base interface for all UI components in VRiseUI.
/// Defines the lifecycle methods that all components must implement.
/// </summary>
public interface IUIComponent
{
    /// <summary>
    /// Unique identifier for this component.
    /// Format: adapterId.componentType.instanceId (e.g., "bloodcraft.progressbar.experience")
    /// </summary>
    string ComponentId { get; }

    /// <summary>
    /// The adapter ID this component belongs to.
    /// </summary>
    string AdapterId { get; }

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
    /// Initialize the component and create UI elements.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Update the component's display state.
    /// Called each frame when the component is active and visible.
    /// </summary>
    void Update();

    /// <summary>
    /// Destroy the component and clean up resources.
    /// </summary>
    void Destroy();

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
