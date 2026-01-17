using Eclipse.Services.HUD.Base;
using Eclipse.Services.HUD.Shared;
using ProjectM.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Services.HUD.ProgressBars;

/// <summary>
/// HUD component for displaying the Legacy (Blood) progress bar.
/// Shows blood type level, prestige, and stat bonuses.
/// </summary>
internal class LegacyBarComponent : ProgressBarComponentBase
{
    #region Fields

    private List<string> _bonusStats = ["", "", ""];

    #endregion

    #region Properties

    public override string ComponentId => "LegacyBar";
    public override bool IsEnabled => HudConfiguration.LegacyBarEnabled;
    public override int MaxLevel => 100;
    public override HudData.UIElement ElementType => HudData.UIElement.Legacy;
    public override Color FillColor => Color.red;

    /// <summary>
    /// Gets or sets the bonus stat strings to display.
    /// </summary>
    public List<string> BonusStats
    {
        get => _bonusStats;
        set => _bonusStats = value ?? ["", "", ""];
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
    }

    public override void Update()
    {
        if (!IsEnabled || !IsReady || HudData.KillSwitch) return;

        UpdateBarDisplay();
        UpdateStats(_bonusStats);
    }

    public override void Reset()
    {
        base.Reset();
        _bonusStats = ["", "", ""];
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
        LocalizedText firstStat,
        LocalizedText secondStat,
        LocalizedText thirdStat)
    {
        BarGameObject = barGameObject;
        InformationPanel = informationPanel;
        FillImage = fillImage;
        ProgressText = progressText;
        HeaderText = headerText;
        StatTexts = [firstStat, secondStat, thirdStat];
    }

    #endregion
}
