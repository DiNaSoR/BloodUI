using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Eclipse.Services.DataService;

namespace Eclipse.Services.CharacterMenu.Interfaces;

/// <summary>
/// Base interface for all character menu tabs.
/// Defines the lifecycle methods and properties that all tabs must implement.
/// </summary>
internal interface ICharacterMenuTab
{
    /// <summary>
    /// Unique identifier for this tab.
    /// </summary>
    string TabId { get; }

    /// <summary>
    /// Display label for the tab button.
    /// </summary>
    string TabLabel { get; }

    /// <summary>
    /// Section title displayed in the content area.
    /// </summary>
    string SectionTitle { get; }

    /// <summary>
    /// The BloodcraftTab enum value for this tab.
    /// </summary>
    BloodcraftTab TabType { get; }

    /// <summary>
    /// Sort order for tab display (lower = earlier).
    /// </summary>
    int SortOrder { get; }

    /// <summary>
    /// Whether this tab has been initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Initialize the tab with content root and reference text.
    /// </summary>
    /// <param name="contentRoot">The parent transform for tab content.</param>
    /// <param name="referenceText">Reference TextMeshPro for styling.</param>
    void Initialize(Transform contentRoot, TextMeshProUGUI referenceText);

    /// <summary>
    /// Update the tab's display state.
    /// </summary>
    void Update();

    /// <summary>
    /// Reset the tab to its initial state.
    /// </summary>
    void Reset();

    /// <summary>
    /// Called when this tab becomes active.
    /// </summary>
    void OnActivated();

    /// <summary>
    /// Called when this tab becomes inactive.
    /// </summary>
    void OnDeactivated();
}

/// <summary>
/// Interface for tabs that display a list of text entries.
/// </summary>
internal interface ICharacterMenuTabWithEntries : ICharacterMenuTab
{
    /// <summary>
    /// Build the list of entries to display.
    /// </summary>
    /// <returns>List of BloodcraftEntry objects to render.</returns>
    List<BloodcraftEntry> BuildEntries();
}

/// <summary>
/// Interface for tabs that display a custom panel (e.g., Professions, StatBonuses).
/// </summary>
internal interface ICharacterMenuTabWithPanel : ICharacterMenuTab
{
    /// <summary>
    /// Create the custom panel UI.
    /// </summary>
    /// <param name="parent">Parent transform.</param>
    /// <param name="reference">Reference text for styling.</param>
    /// <returns>The created panel transform.</returns>
    Transform CreatePanel(Transform parent, TextMeshProUGUI reference);

    /// <summary>
    /// Update the panel contents.
    /// </summary>
    void UpdatePanel();
}

/// <summary>
/// Represents a single line entry in the Bloodcraft tab.
/// </summary>
public readonly struct BloodcraftEntry
{
    public string Text { get; }
    public string Command { get; }
    public Action Action { get; }
    public bool Enabled { get; }
    public FontStyles Style { get; }

    /// <summary>
    /// Initializes a new Bloodcraft entry.
    /// </summary>
    /// <param name="text">The display text for the entry.</param>
    /// <param name="style">The font style to apply.</param>
    /// <param name="command">An optional chat command to send when clicked.</param>
    /// <param name="action">An optional local action to invoke when clicked.</param>
    /// <param name="enabled">Whether the entry is clickable.</param>
    public BloodcraftEntry(string text, FontStyles style, string command = "", Action action = null, bool enabled = false)
    {
        Text = text;
        Command = command;
        Action = action;
        Enabled = enabled;
        Style = style;
    }
}
