using System.Collections.Generic;
using UnityEngine;
using static Eclipse.Services.HUD.Shared.HudData;

namespace Eclipse.Services.HUD.Interfaces;

/// <summary>
/// Interface for progress bar HUD components (Experience, Legacy, Expertise, Familiar bars).
/// Extends IHudComponent with progress-specific properties and methods.
/// </summary>
internal interface IHudProgressBar : IHudComponent
{
    /// <summary>
    /// Current progress value (0.0 to 1.0).
    /// </summary>
    float Progress { get; set; }

    /// <summary>
    /// Current level value.
    /// </summary>
    int Level { get; set; }

    /// <summary>
    /// Maximum achievable level.
    /// </summary>
    int MaxLevel { get; }

    /// <summary>
    /// Current prestige level.
    /// </summary>
    int Prestige { get; set; }

    /// <summary>
    /// Type label displayed on the bar (e.g., weapon type, blood type).
    /// </summary>
    string TypeLabel { get; set; }

    /// <summary>
    /// The UI element type this progress bar represents.
    /// </summary>
    UIElement ElementType { get; }

    /// <summary>
    /// The fill color for this progress bar.
    /// </summary>
    Color FillColor { get; }

    /// <summary>
    /// Configure the progress bar appearance and register with layout service.
    /// </summary>
    void ConfigureBar();

    /// <summary>
    /// Update the stat display values.
    /// </summary>
    /// <param name="stats">List of stat strings to display.</param>
    void UpdateStats(List<string> stats);
}
