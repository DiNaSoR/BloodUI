using Eclipse.Services.HUD.Base;
using Eclipse.Services.HUD.Interfaces;
using Eclipse.Services.HUD.Shared;
using ProjectM.UI;
using UnityEngine;
using UnityEngine.UI;
using static Eclipse.Services.DataService;

namespace Eclipse.Services.HUD.QuestTracker;

/// <summary>
/// HUD component for displaying quest tracking information.
/// Shows daily and weekly quest progress.
/// </summary>
internal class QuestTrackerComponent : HudComponentBase, IHudWindow
{
    #region Nested Types

    /// <summary>
    /// Represents a single quest window (daily or weekly).
    /// </summary>
    public class QuestWindow
    {
        public GameObject WindowObject { get; set; }
        public LocalizedText HeaderText { get; set; }
        public LocalizedText SubHeaderText { get; set; }
        public Image IconImage { get; set; }

        public TargetType TargetType { get; set; } = TargetType.Kill;
        public int Progress { get; set; }
        public int Goal { get; set; }
        public string Target { get; set; } = string.Empty;
        public bool IsVBlood { get; set; }

        public void Reset()
        {
            WindowObject = null;
            HeaderText = null;
            SubHeaderText = null;
            IconImage = null;
            TargetType = TargetType.Kill;
            Progress = 0;
            Goal = 0;
            Target = string.Empty;
            IsVBlood = false;
        }
    }

    #endregion

    #region Fields

    private readonly QuestWindow _dailyQuest = new();
    private readonly QuestWindow _weeklyQuest = new();

    #endregion

    #region Properties

    public override string ComponentId => "QuestTracker";
    public override bool IsEnabled => HudConfiguration.QuestTrackerEnabled;

    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool IsDraggable => true;

    /// <summary>
    /// Gets the daily quest window.
    /// </summary>
    public QuestWindow DailyQuest => _dailyQuest;

    /// <summary>
    /// Gets the weekly quest window.
    /// </summary>
    public QuestWindow WeeklyQuest => _weeklyQuest;

    #endregion

    #region Lifecycle

    public override void Initialize(UICanvasBase canvas)
    {
        base.Initialize(canvas);

        if (!IsEnabled) return;

        // Quest windows are configured by CanvasService.ConfigureHUD
        IsReady = true;
    }

    public override void Update()
    {
        if (!IsEnabled || !IsReady || HudData.KillSwitch) return;

        UpdateQuest(_dailyQuest);
        UpdateQuest(_weeklyQuest);
    }

    public override void Reset()
    {
        base.Reset();
        _dailyQuest.Reset();
        _weeklyQuest.Reset();
    }

    #endregion

    #region IHudWindow

    public void Show()
    {
        IsVisible = true;
        if (_dailyQuest.WindowObject != null)
            _dailyQuest.WindowObject.SetActive(true);
        if (_weeklyQuest.WindowObject != null)
            _weeklyQuest.WindowObject.SetActive(true);
    }

    public void Hide()
    {
        IsVisible = false;
        if (_dailyQuest.WindowObject != null)
            _dailyQuest.WindowObject.SetActive(false);
        if (_weeklyQuest.WindowObject != null)
            _weeklyQuest.WindowObject.SetActive(false);
    }

    #endregion

    #region Display Updates

    /// <summary>
    /// Updates a quest window's display.
    /// </summary>
    private void UpdateQuest(QuestWindow quest)
    {
        if (quest.WindowObject == null || quest.SubHeaderText == null) return;

        // Update icon based on quest type
        if (quest.IconImage != null)
        {
            Sprite targetSprite = quest.IsVBlood
                ? HudData.QuestKillVBloodUnit
                : HudData.QuestKillStandardUnit;

            if (targetSprite != null && quest.IconImage.sprite != targetSprite)
            {
                quest.IconImage.sprite = targetSprite;
            }
        }

        // Update subheader with progress
        string progressText = BuildProgressText(quest);
        if (quest.SubHeaderText.GetText() != progressText)
        {
            quest.SubHeaderText.ForceSet(progressText);
        }
    }

    /// <summary>
    /// Builds the progress text for a quest.
    /// </summary>
    private static string BuildProgressText(QuestWindow quest)
    {
        if (quest.Goal == 0)
        {
            return "No quest active";
        }

        string targetText = quest.TargetType switch
        {
            TargetType.Kill => $"Kill {quest.Target}",
            TargetType.Craft => $"Craft {quest.Target}",
            TargetType.Gather => $"Gather {quest.Target}",
            _ => quest.Target
        };

        bool isComplete = quest.Progress >= quest.Goal;
        string colorTag = isComplete ? "#90EE90" : "#FFFFFF";

        return $"<color={colorTag}>{targetText}: {quest.Progress}/{quest.Goal}</color>";
    }

    #endregion

    #region UI Element Setters

    /// <summary>
    /// Sets the daily quest UI elements.
    /// </summary>
    public void SetDailyQuestElements(
        GameObject windowObject,
        LocalizedText headerText,
        LocalizedText subHeaderText,
        Image iconImage)
    {
        _dailyQuest.WindowObject = windowObject;
        _dailyQuest.HeaderText = headerText;
        _dailyQuest.SubHeaderText = subHeaderText;
        _dailyQuest.IconImage = iconImage;
    }

    /// <summary>
    /// Sets the weekly quest UI elements.
    /// </summary>
    public void SetWeeklyQuestElements(
        GameObject windowObject,
        LocalizedText headerText,
        LocalizedText subHeaderText,
        Image iconImage)
    {
        _weeklyQuest.WindowObject = windowObject;
        _weeklyQuest.HeaderText = headerText;
        _weeklyQuest.SubHeaderText = subHeaderText;
        _weeklyQuest.IconImage = iconImage;
    }

    #endregion
}
