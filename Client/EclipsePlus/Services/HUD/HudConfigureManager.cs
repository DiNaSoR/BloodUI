using Eclipse.Services.HUD.Shared;
using ProjectM.UI;
using StunShared.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Eclipse.Services.CanvasService.DataHUD;
using static Eclipse.Services.CanvasService.InputHUD;
using static Eclipse.Services.CanvasService.ToggleHUD;
using static Eclipse.Utilities.GameObjects;

namespace Eclipse.Services.HUD;

/// <summary>
/// Manages HUD element configuration.
/// Delegates from CanvasService.ConfigureHUD to maintain backward compatibility.
/// The actual implementation remains in ConfigureHUD for now but can be migrated here incrementally.
/// </summary>
internal static class HudConfigureManager
{
    #region Configuration Flags

    public static readonly bool ExperienceBarEnabled = Plugin.Leveling;
    public static readonly bool ShowPrestige = Plugin.Prestige;
    public static readonly bool LegacyBarEnabled = Plugin.Legacies;
    public static readonly bool ExpertiseBarEnabled = Plugin.Expertise;
    public static readonly bool FamiliarBarEnabled = Plugin.Familiars;
    public static readonly bool ProfessionBarsEnabled = Plugin.Professions;
    public static readonly bool QuestTrackerEnabled = Plugin.Quests;
    public static readonly bool ShiftSlotEnabled = Plugin.ShiftSlot;

    #endregion

    #region Reset

    public static void Reset()
    {
        // Reset any configuration state if needed
    }

    #endregion
}
