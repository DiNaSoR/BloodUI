using Eclipse.Services.HUD.Base;
using Eclipse.Services.HUD.Shared;
using ProjectM.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Eclipse.Services.DataService;

namespace Eclipse.Services.HUD.ProgressBars;

/// <summary>
/// HUD component for displaying the Experience progress bar.
/// Shows player level, prestige, and class information.
/// </summary>
internal class ExperienceBarComponent : ProgressBarComponentBase
{
    #region Fields

    private LocalizedText _firstText;
    private LocalizedText _classText;
    private LocalizedText _secondText;
    private PlayerClass _classType = PlayerClass.None;

    #endregion

    #region Properties

    public override string ComponentId => "ExperienceBar";
    public override bool IsEnabled => HudConfiguration.ExperienceBarEnabled;
    public override int MaxLevel => 90;
    public override HudData.UIElement ElementType => HudData.UIElement.Experience;
    public override Color FillColor => Color.green;

    /// <summary>
    /// Gets or sets the player's current class.
    /// </summary>
    public PlayerClass ClassType
    {
        get => _classType;
        set => _classType = value;
    }

    #endregion

    #region Lifecycle

    public override void Initialize(UICanvasBase canvas)
    {
        base.Initialize(canvas);

        if (!IsEnabled) return;

        ConfigureBar();
        IsReady = true;
    }

    public override void ConfigureBar()
    {
        // Configuration will be handled by CanvasService.ConfigureHUD for now
        // This is a placeholder for when the full extraction is complete
    }

    public override void Update()
    {
        if (!IsEnabled || !IsReady || HudData.KillSwitch) return;

        UpdateBarDisplay();
        UpdateClassDisplay();
    }

    public override void Reset()
    {
        base.Reset();
        _firstText = null;
        _classText = null;
        _secondText = null;
        _classType = PlayerClass.None;
    }

    #endregion

    #region Display Updates

    /// <summary>
    /// Updates the class display on the experience bar.
    /// </summary>
    private void UpdateClassDisplay()
    {
        if (_classText == null) return;

        if (_classType == PlayerClass.None)
        {
            _classText.ForceSet(string.Empty);
            return;
        }

        string className = HudUtilities.FormatClassName(_classType);
        Color classColor = HudUtilities.GetClassColor(_classType);
        string colorHex = ColorUtility.ToHtmlStringRGB(classColor);

        _classText.ForceSet($"<color=#{colorHex}>{className}</color>");
    }

    public override void UpdateStats(List<string> stats)
    {
        // Experience bar doesn't have stat display
    }

    #endregion

    #region UI Element Setters

    /// <summary>
    /// Sets the UI element references for this component.
    /// Called during initialization from CanvasService.
    /// </summary>
    public void SetUIElements(
        GameObject barGameObject,
        GameObject informationPanel,
        Image fillImage,
        LocalizedText progressText,
        LocalizedText headerText,
        LocalizedText firstText,
        LocalizedText classText,
        LocalizedText secondText)
    {
        BarGameObject = barGameObject;
        InformationPanel = informationPanel;
        FillImage = fillImage;
        ProgressText = progressText;
        HeaderText = headerText;
        _firstText = firstText;
        _classText = classText;
        _secondText = secondText;
    }

    #endregion
}
