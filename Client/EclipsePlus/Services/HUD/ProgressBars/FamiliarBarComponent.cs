using Eclipse.Services.HUD.Base;
using Eclipse.Services.HUD.Shared;
using ProjectM.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Services.HUD.ProgressBars;

/// <summary>
/// HUD component for displaying the Familiar progress bar.
/// Shows familiar level, prestige, and stats (health, physical power, spell power).
/// </summary>
internal class FamiliarBarComponent : ProgressBarComponentBase
{
    #region Fields

    private string _familiarName = string.Empty;
    private List<string> _familiarStats = ["", "", ""];

    #endregion

    #region Properties

    public override string ComponentId => "FamiliarBar";
    public override bool IsEnabled => HudConfiguration.FamiliarBarEnabled;
    public override int MaxLevel => 90;
    public override HudData.UIElement ElementType => HudData.UIElement.Familiars;
    public override Color FillColor => Color.yellow;

    /// <summary>
    /// Gets or sets the familiar's name.
    /// </summary>
    public string FamiliarName
    {
        get => _familiarName;
        set => _familiarName = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the familiar stat strings (MaxHealth, PhysicalPower, SpellPower).
    /// </summary>
    public List<string> FamiliarStats
    {
        get => _familiarStats;
        set => _familiarStats = value ?? ["", "", ""];
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
        UpdateStats(_familiarStats);
    }

    public override void Reset()
    {
        base.Reset();
        _familiarName = string.Empty;
        _familiarStats = ["", "", ""];
    }

    #endregion

    #region Display Updates

    /// <summary>
    /// Updates the bar display with familiar name formatting.
    /// </summary>
    protected new void UpdateBarDisplay()
    {
        if (!IsReady || !IsEnabled) return;

        // Update fill amount
        if (FillImage != null)
        {
            FillImage.fillAmount = Level == MaxLevel ? 1f : Progress;
        }

        // Update header with trimmed familiar name
        if (HeaderText != null)
        {
            string displayName = HudUtilities.TrimToFirstWord(_familiarName);
            string prestigeText = Prestige > 0 ? $" {HudUtilities.ToRoman(Prestige)}" : string.Empty;
            HeaderText.Text.SetText($"{displayName}{prestigeText}");
        }

        // Update progress text
        if (ProgressText != null)
        {
            string levelString = (_familiarName == "Frailed" || _familiarName == "Familiar") ? "N/A" : Level.ToString();
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
        LocalizedText maxHealthText,
        LocalizedText physicalPowerText,
        LocalizedText spellPowerText)
    {
        BarGameObject = barGameObject;
        InformationPanel = informationPanel;
        FillImage = fillImage;
        ProgressText = progressText;
        HeaderText = headerText;
        StatTexts = [maxHealthText, physicalPowerText, spellPowerText];
    }

    #endregion
}
