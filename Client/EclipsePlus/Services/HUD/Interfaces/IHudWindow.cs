using UnityEngine;

namespace Eclipse.Services.HUD.Interfaces;

/// <summary>
/// Interface for window-style HUD components (Quest Tracker, Class UI, Tabs UI).
/// Extends IHudComponent with window-specific properties.
/// </summary>
internal interface IHudWindow : IHudComponent
{
    /// <summary>
    /// The position of the window.
    /// </summary>
    Vector2 Position { get; set; }

    /// <summary>
    /// The size of the window.
    /// </summary>
    Vector2 Size { get; set; }

    /// <summary>
    /// Whether this window can be dragged by the user.
    /// </summary>
    bool IsDraggable { get; }

    /// <summary>
    /// Show the window.
    /// </summary>
    void Show();

    /// <summary>
    /// Hide the window.
    /// </summary>
    void Hide();
}
