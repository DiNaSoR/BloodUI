namespace Eclipse.Services.HUD.Shared;

/// <summary>
/// Central configuration for HUD components.
/// Provides easy access to enabled/disabled states from Plugin configuration.
/// </summary>
internal static class HudConfiguration
{
    // Feature flags - derived from Plugin configuration
    public static bool ExperienceBarEnabled => Plugin.Leveling;
    public static bool LegacyBarEnabled => Plugin.Legacies;
    public static bool ExpertiseBarEnabled => Plugin.Expertise;
    public static bool FamiliarBarEnabled => Plugin.Familiars;
    public static bool ProfessionBarsEnabled => Plugin.Professions;
    public static bool QuestTrackerEnabled => Plugin.Quests;
    public static bool ShiftSlotEnabled => Plugin.ShiftSlot;
    public static bool AttributeBuffsEnabled => Plugin.AttributeBuffsEnabled;
    public static bool PrestigeEnabled => Plugin.Prestige;
    public static bool Eclipsed => Plugin.Eclipsed;
}
