using Eclipse.Services.HUD.Shared;
using ProjectM.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Eclipse.Services.CanvasService.DataHUD;
using static Eclipse.Services.CanvasService.InputHUD;
using static Eclipse.Utilities.GameObjects;

namespace Eclipse.Services.HUD.Configurators;

/// <summary>
/// Configures quest tracker windows for the HUD.
/// Extracted from CanvasService.ConfigureHUD.ConfigureQuestWindow().
/// </summary>
internal static class QuestWindowConfigurator
{
    /// <summary>
    /// Configures a quest window (Daily or Weekly) with all required UI components.
    /// </summary>
    public static void Configure(
        UICanvasBase canvasBase,
        Canvas bottomBarCanvas,
        int layer,
        ref float windowOffset,
        ref GameObject questObject,
        HudData.UIElement questType,
        Color headerColor,
        ref LocalizedText header,
        ref LocalizedText subHeader,
        ref Image questIcon)
    {
        bool compactQuests = LayoutService.CurrentOptions.CompactQuests;

        // Instantiate quest tooltip
        questObject = UnityEngine.Object.Instantiate(canvasBase.BottomBarParentPrefab.FakeTooltip.gameObject, bottomBarCanvas.transform, false);
        RectTransform questTransform = questObject.GetComponent<RectTransform>();

        // Activate quest window
        questTransform.gameObject.layer = layer;
        questObject.SetActive(true);

        // Deactivate unwanted objects in quest tooltips
        GameObject entries = FindTargetUIObject(questObject.transform, "InformationEntries");
        DeactivateChildrenExceptNamed(entries.transform, "TooltipHeader");

        // Activate TooltipHeader
        GameObject tooltipHeader = FindTargetUIObject(questObject.transform, "TooltipHeader");
        tooltipHeader.SetActive(true);

        // Activate Icon&Name container
        GameObject iconNameObject = FindTargetUIObject(tooltipHeader.transform, "Icon&Name");
        iconNameObject.SetActive(true);

        // Deactivate LevelFrames and ReforgeCosts
        GameObject levelFrame = FindTargetUIObject(iconNameObject.transform, "LevelFrame");
        levelFrame.SetActive(false);
        GameObject reforgeCost = FindTargetUIObject(questObject.transform, "Tooltip_ReforgeCost");
        reforgeCost.SetActive(false);

        // Deactivate TooltipIcon
        GameObject tooltipIcon = FindTargetUIObject(tooltipHeader.transform, "TooltipIcon");
        RectTransform tooltipIconTransform = tooltipIcon.GetComponent<RectTransform>();

        // Set position relative to parent
        tooltipIconTransform.anchorMin = new Vector2(tooltipIconTransform.anchorMin.x, 0.55f);
        tooltipIconTransform.anchorMax = new Vector2(tooltipIconTransform.anchorMax.x, 0.55f);

        // Set the pivot to the vertical center
        tooltipIconTransform.pivot = new Vector2(tooltipIconTransform.pivot.x, 0.55f);

        questIcon = tooltipIcon.GetComponent<Image>();
        if (questType.Equals(HudData.UIElement.Daily))
        {
            if (HudData.Sprites.ContainsKey("BloodIcon_Small_Warrior"))
            {
                questIcon.sprite = HudData.Sprites["BloodIcon_Small_Warrior"];
            }
        }
        else if (questType.Equals(HudData.UIElement.Weekly))
        {
            if (HudData.Sprites.ContainsKey("BloodIcon_Warrior"))
            {
                questIcon.sprite = HudData.Sprites["BloodIcon_Warrior"];
            }
        }

        float iconScale = 0.35f;
        tooltipIconTransform.sizeDelta = new Vector2(tooltipIconTransform.sizeDelta.x * iconScale, tooltipIconTransform.sizeDelta.y * iconScale);

        // Set LocalizedText for QuestHeaders
        GameObject subHeaderObject = FindTargetUIObject(iconNameObject.transform, "TooltipSubHeader");
        header = FindTargetUIObject(iconNameObject.transform, "TooltipHeader").GetComponent<LocalizedText>();
        header.Text.fontSize *= compactQuests ? 1.5f : 2f;
        header.Text.color = headerColor;
        subHeader = subHeaderObject.GetComponent<LocalizedText>();
        subHeader.Text.enableAutoSizing = false;
        subHeader.Text.autoSizeTextContainer = false;
        subHeader.Text.enableWordWrapping = false;

        // Configure the subheader's content size fitter
        ContentSizeFitter subHeaderFitter = subHeaderObject.GetComponent<ContentSizeFitter>();
        subHeaderFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        subHeaderFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Size window
        float widthScale = compactQuests ? 0.55f : 0.65f;
        float heightScale = compactQuests ? 0.7f : 1f;
        questTransform.sizeDelta = new Vector2(questTransform.sizeDelta.x * widthScale, questTransform.sizeDelta.y * heightScale);

        // Set anchor and pivots
        questTransform.anchorMin = new Vector2(1, windowOffset);
        questTransform.anchorMax = new Vector2(1, windowOffset);
        questTransform.pivot = new Vector2(1, windowOffset);
        questTransform.anchoredPosition = new Vector2(0, windowOffset);

        // Keyboard/Mouse layout
        Vector2 kmAnchorMin = new(1f, windowOffset);
        Vector2 kmAnchorMax = new(1f, windowOffset);
        Vector2 kmPivot = new(1f, windowOffset);
        Vector2 kmPos = new(0f, windowOffset);

        // Controller layout
        Vector2 ctrlAnchorMin = new(0.6f, windowOffset);
        Vector2 ctrlAnchorMax = new(0.6f, windowOffset);
        Vector2 ctrlPivot = new(0.6f, windowOffset);
        Vector2 ctrlPos = new(0f, windowOffset);

        // Set header text
        header.ForceSet(questType.ToString() + " Quest");
        subHeader.ForceSet("UnitName: 0/0");

        // Add to active objects (use indexer to allow re-initialization on re-login)
        HudData.GameObjects[questType] = questObject;
        ObjectStates[questObject] = true;
        LayoutService.RegisterElement($"Quest.{questType}", questTransform);
        windowOffset += compactQuests ? 0.055f : 0.075f;

        // Register positions
        RegisterAdaptiveElement(
            questObject,
            keyboardMousePos: kmPos,
            keyboardMouseAnchorMin: kmAnchorMin,
            keyboardMouseAnchorMax: kmAnchorMax,
            keyboardMousePivot: kmPivot,
            keyboardMouseScale: questTransform.localScale,

            controllerPos: ctrlPos,
            controllerAnchorMin: ctrlAnchorMin,
            controllerAnchorMax: ctrlAnchorMax,
            controllerPivot: ctrlPivot,
            controllerScale: questTransform.localScale * 0.85f
        );
    }
}
