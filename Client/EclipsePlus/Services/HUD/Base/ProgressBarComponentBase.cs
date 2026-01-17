using Eclipse.Services.HUD.Interfaces;
using Eclipse.Services.HUD.Shared;
using ProjectM.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Services.HUD.Base;

/// <summary>
/// Abstract base class for progress bar HUD components.
/// Provides common functionality for Experience, Legacy, Expertise, and Familiar bars.
/// </summary>
internal abstract class ProgressBarComponentBase : HudComponentBase, IHudProgressBar
{
    // UI Element references
    protected GameObject BarGameObject { get; set; }
    protected GameObject InformationPanel { get; set; }
    protected Image FillImage { get; set; }
    protected LocalizedText HeaderText { get; set; }
    protected LocalizedText ProgressText { get; set; }
    protected LocalizedText[] StatTexts { get; set; } = new LocalizedText[3];

    // IHudProgressBar properties
    public float Progress { get; set; }
    public int Level { get; set; }
    public abstract int MaxLevel { get; }
    public int Prestige { get; set; }
    public string TypeLabel { get; set; } = string.Empty;
    public abstract HudData.UIElement ElementType { get; }
    public abstract Color FillColor { get; }

    public override void Reset()
    {
        base.Reset();
        SafeDestroy(ref barObj);
        BarGameObject = null;
        InformationPanel = null;
        FillImage = null;
        HeaderText = null;
        ProgressText = null;
        StatTexts = new LocalizedText[3];
    }

    private GameObject barObj;

    public abstract void ConfigureBar();

    public virtual void UpdateStats(List<string> stats)
    {
        if (stats == null || StatTexts == null) return;

        for (int i = 0; i < StatTexts.Length && i < stats.Count; i++)
        {
            if (StatTexts[i] != null)
            {
                StatTexts[i].Text.SetText(stats[i]);
            }
        }
    }

    /// <summary>
    /// Updates the progress bar display with current values.
    /// </summary>
    protected void UpdateBarDisplay()
    {
        if (!IsReady || !IsEnabled) return;

        // Update fill amount
        if (FillImage != null)
        {
            FillImage.fillAmount = Progress;
        }

        // Update header text with level and prestige
        if (HeaderText != null)
        {
            string prestigeText = Prestige > 0 ? $" ({HudUtilities.ToRoman(Prestige)})" : string.Empty;
            string typeText = !string.IsNullOrEmpty(TypeLabel) ? $" [{TypeLabel}]" : string.Empty;
            HeaderText.Text.SetText($"Lv {Level}{prestigeText}{typeText}");
        }

        // Update progress percentage text
        if (ProgressText != null)
        {
            int percentage = (int)(Progress * 100);
            ProgressText.Text.SetText($"{percentage}%");
        }
    }
}
