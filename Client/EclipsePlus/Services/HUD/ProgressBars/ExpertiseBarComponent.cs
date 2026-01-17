using Eclipse.Services.HUD.Base;
using Eclipse.Services.HUD.Shared;
using ProjectM.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Services.HUD.ProgressBars;

/// <summary>
/// HUD component for displaying the Expertise (Weapon) progress bar.
/// Shows weapon type level, prestige, and stat bonuses.
/// </summary>
internal class ExpertiseBarComponent : ProgressBarComponentBase
{
    #region Fields

    private List<string> _bonusStats = ["", "", ""];

    #endregion

    #region Properties

    public override string ComponentId => "ExpertiseBar";
    public override bool IsEnabled => HudConfiguration.ExpertiseBarEnabled;
    public override int MaxLevel => 100;
    public override HudData.UIElement ElementType => HudData.UIElement.Expertise;
    public override Color FillColor => Color.grey;

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

    #region Display Updates

    /// <summary>
    /// Updates the bar display with weapon type formatting.
    /// </summary>
    protected new void UpdateBarDisplay()
    {
        if (!IsReady || !IsEnabled) return;

        // Update fill amount
        if (FillImage != null)
        {
            FillImage.fillAmount = Level == MaxLevel ? 1f : Progress;
        }

        // Update header with formatted weapon type
        if (HeaderText != null)
        {
            string formattedType = HudUtilities.SplitPascalCase(TypeLabel);
            string prestigeText = Prestige > 0 ? $" {HudUtilities.ToRoman(Prestige)}" : string.Empty;
            HeaderText.Text.SetText($"{formattedType}{prestigeText}");
        }

        // Update progress text
        if (ProgressText != null)
        {
            string levelString = Level.ToString();
            if (ProgressText.GetText() != levelString)
            {
                ProgressText.ForceSet(levelString);
            }
        }
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
