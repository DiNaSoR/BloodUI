using Eclipse.Services.HUD.ProgressBars;
using Eclipse.Services.HUD.QuestTracker;
using Eclipse.Services.HUD.Shared;
using ProjectM.UI;

namespace Eclipse.Services.HUD;

/// <summary>
/// Integration layer between the legacy CanvasService and the new modular HUD components.
/// Provides factory methods to create and register components with the orchestrator.
/// </summary>
internal static class HudIntegration
{
    private static HudOrchestrator _orchestrator;

    /// <summary>
    /// Gets or creates the HUD orchestrator.
    /// </summary>
    public static HudOrchestrator GetOrCreateOrchestrator(UICanvasBase canvas)
    {
        if (_orchestrator == null || !_orchestrator.IsInitialized)
        {
            _orchestrator = new HudOrchestrator(canvas);
            RegisterDefaultComponents();
        }
        return _orchestrator;
    }

    /// <summary>
    /// Registers all default HUD components with the orchestrator.
    /// </summary>
    private static void RegisterDefaultComponents()
    {
        if (_orchestrator == null) return;

        // Register progress bar components
        if (HudConfiguration.ExperienceBarEnabled)
        {
            _orchestrator.RegisterComponent(new ExperienceBarComponent());
        }

        if (HudConfiguration.LegacyBarEnabled)
        {
            _orchestrator.RegisterComponent(new LegacyBarComponent());
        }

        if (HudConfiguration.ExpertiseBarEnabled)
        {
            _orchestrator.RegisterComponent(new ExpertiseBarComponent());
        }

        if (HudConfiguration.FamiliarBarEnabled)
        {
            _orchestrator.RegisterComponent(new FamiliarBarComponent());
        }

        // Register quest tracker component
        if (HudConfiguration.QuestTrackerEnabled)
        {
            _orchestrator.RegisterComponent(new QuestTrackerComponent());
        }
    }

    /// <summary>
    /// Gets the Experience bar component.
    /// </summary>
    public static ExperienceBarComponent GetExperienceBar()
    {
        return _orchestrator?.GetComponent<ExperienceBarComponent>();
    }

    /// <summary>
    /// Gets the Legacy bar component.
    /// </summary>
    public static LegacyBarComponent GetLegacyBar()
    {
        return _orchestrator?.GetComponent<LegacyBarComponent>();
    }

    /// <summary>
    /// Gets the Expertise bar component.
    /// </summary>
    public static ExpertiseBarComponent GetExpertiseBar()
    {
        return _orchestrator?.GetComponent<ExpertiseBarComponent>();
    }

    /// <summary>
    /// Gets the Familiar bar component.
    /// </summary>
    public static FamiliarBarComponent GetFamiliarBar()
    {
        return _orchestrator?.GetComponent<FamiliarBarComponent>();
    }

    /// <summary>
    /// Gets the Quest Tracker component.
    /// </summary>
    public static QuestTrackerComponent GetQuestTracker()
    {
        return _orchestrator?.GetComponent<QuestTrackerComponent>();
    }

    /// <summary>
    /// Resets the integration layer and orchestrator.
    /// </summary>
    public static void Reset()
    {
        _orchestrator?.Reset();
        _orchestrator = null;
    }

    /// <summary>
    /// Updates the experience bar data from legacy DataHUD values.
    /// </summary>
    public static void SyncExperienceBarFromLegacy(
        float progress,
        int level,
        int prestige,
        DataService.PlayerClass classType)
    {
        var bar = GetExperienceBar();
        if (bar == null) return;

        bar.Progress = progress;
        bar.Level = level;
        bar.Prestige = prestige;
        bar.ClassType = classType;
    }

    /// <summary>
    /// Updates the legacy bar data from legacy DataHUD values.
    /// </summary>
    public static void SyncLegacyBarFromLegacy(
        float progress,
        int level,
        int prestige,
        string bloodType,
        System.Collections.Generic.List<string> bonusStats)
    {
        var bar = GetLegacyBar();
        if (bar == null) return;

        bar.Progress = progress;
        bar.Level = level;
        bar.Prestige = prestige;
        bar.TypeLabel = bloodType;
        bar.BonusStats = bonusStats;
    }

    /// <summary>
    /// Updates the expertise bar data from legacy DataHUD values.
    /// </summary>
    public static void SyncExpertiseBarFromLegacy(
        float progress,
        int level,
        int prestige,
        string weaponType,
        System.Collections.Generic.List<string> bonusStats)
    {
        var bar = GetExpertiseBar();
        if (bar == null) return;

        bar.Progress = progress;
        bar.Level = level;
        bar.Prestige = prestige;
        bar.TypeLabel = weaponType;
        bar.BonusStats = bonusStats;
    }

    /// <summary>
    /// Updates the familiar bar data from legacy DataHUD values.
    /// </summary>
    public static void SyncFamiliarBarFromLegacy(
        float progress,
        int level,
        int prestige,
        string familiarName,
        System.Collections.Generic.List<string> stats)
    {
        var bar = GetFamiliarBar();
        if (bar == null) return;

        bar.Progress = progress;
        bar.Level = level;
        bar.Prestige = prestige;
        bar.FamiliarName = familiarName;
        bar.FamiliarStats = stats;
    }

    /// <summary>
    /// Updates the quest tracker data from legacy DataHUD values.
    /// </summary>
    public static void SyncQuestTrackerFromLegacy(
        DataService.TargetType dailyTargetType,
        string dailyTarget,
        int dailyProgress,
        int dailyGoal,
        bool dailyVBlood,
        DataService.TargetType weeklyTargetType,
        string weeklyTarget,
        int weeklyProgress,
        int weeklyGoal,
        bool weeklyVBlood)
    {
        var tracker = GetQuestTracker();
        if (tracker == null) return;

        tracker.DailyQuest.TargetType = dailyTargetType;
        tracker.DailyQuest.Target = dailyTarget;
        tracker.DailyQuest.Progress = dailyProgress;
        tracker.DailyQuest.Goal = dailyGoal;
        tracker.DailyQuest.IsVBlood = dailyVBlood;

        tracker.WeeklyQuest.TargetType = weeklyTargetType;
        tracker.WeeklyQuest.Target = weeklyTarget;
        tracker.WeeklyQuest.Progress = weeklyProgress;
        tracker.WeeklyQuest.Goal = weeklyGoal;
        tracker.WeeklyQuest.IsVBlood = weeklyVBlood;
    }
}
