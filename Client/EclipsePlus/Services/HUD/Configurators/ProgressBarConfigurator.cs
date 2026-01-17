using Eclipse.Services.HUD.Shared;
using ProjectM.UI;
using UnityEngine;
using UnityEngine.UI;
using static Eclipse.Services.CanvasService.DataHUD;
using static Eclipse.Services.CanvasService.InputHUD;
using static Eclipse.Services.DataService;
using static Eclipse.Utilities.GameObjects;

namespace Eclipse.Services.HUD.Configurators;

/// <summary>
/// Configures horizontal and vertical progress bars for the HUD.
/// Extracted from CanvasService.ConfigureHUD.ConfigureHorizontalProgressBar() and ConfigureVerticalProgressBar().
/// </summary>
internal static class ProgressBarConfigurator
{
    #region Horizontal Progress Bar

    /// <summary>
    /// Configures a horizontal progress bar (Experience, Legacy, Expertise, Familiar).
    /// </summary>
    public static void ConfigureHorizontal(
        UICanvasBase canvasBase,
        Canvas targetInfoPanelCanvas,
        int layer,
        ref int barNumber,
        ref float horizontalBarHeaderFontSize,
        ref GameObject barGameObject,
        ref GameObject informationPanelObject,
        ref Image fill,
        ref LocalizedText level,
        ref LocalizedText header,
        HudData.UIElement element,
        Color fillColor,
        ref LocalizedText firstText,
        ref LocalizedText secondText,
        ref LocalizedText thirdText)
    {
        // Instantiate the bar object from the prefab
        barGameObject = UnityEngine.Object.Instantiate(canvasBase.TargetInfoParent.gameObject, targetInfoPanelCanvas.transform, false);
        RectTransform barRectTransform = barGameObject.GetComponent<RectTransform>();
        barRectTransform.gameObject.layer = layer;

        bool verticalBars = LayoutService.CurrentOptions.VerticalBars;

        if (verticalBars)
        {
            float offsetX = 0.94f - (barNumber * 0.03f);
            float offsetY = 0.52f;
            barRectTransform.anchorMin = new Vector2(offsetX, offsetY);
            barRectTransform.anchorMax = new Vector2(offsetX, offsetY);
            barRectTransform.pivot = new Vector2(offsetX, offsetY);
            barRectTransform.localScale = new Vector3(0.5f, 1f, 1f);
            barRectTransform.localRotation = Quaternion.Euler(0, 0, -90);
        }
        else
        {
            // Set anchor and pivot to middle-upper-right
            float offsetY = HudData.BAR_HEIGHT_SPACING * barNumber;
            float offsetX = 1f - HudData.BAR_WIDTH_SPACING;
            barRectTransform.anchorMin = new Vector2(offsetX, 0.6f - offsetY);
            barRectTransform.anchorMax = new Vector2(offsetX, 0.6f - offsetY);
            barRectTransform.pivot = new Vector2(offsetX, 0.6f - offsetY);

            // Best scale found so far for different resolutions
            barRectTransform.localScale = new Vector3(0.7f, 0.7f, 1f);
            barRectTransform.localRotation = Quaternion.identity;
        }

        // Assign fill, header, and level text components
        fill = FindTargetUIObject(barRectTransform.transform, "Fill").GetComponent<Image>();
        level = FindTargetUIObject(barRectTransform.transform, "LevelText").GetComponent<LocalizedText>();
        header = FindTargetUIObject(barRectTransform.transform, "Name").GetComponent<LocalizedText>();

        // Set initial values
        fill.fillAmount = 0f;
        fill.color = fillColor;
        level.ForceSet("0");

        // Set header text
        header.ForceSet(element.ToString());
        header.Text.fontSize *= verticalBars ? 1.1f : 1.5f;
        horizontalBarHeaderFontSize = header.Text.fontSize;

        // Set these to 0 so don't appear, deactivating instead seemed funky
        FindTargetUIObject(barRectTransform.transform, "DamageTakenFill").GetComponent<Image>().fillAmount = 0f;
        FindTargetUIObject(barRectTransform.transform, "AbsorbFill").GetComponent<Image>().fillAmount = 0f;

        // Configure informationPanels
        informationPanelObject = FindTargetUIObject(barRectTransform.transform, "InformationPanel");
        ConfigureInformationPanel(ref informationPanelObject, ref firstText, ref secondText, ref thirdText, element);

        if (verticalBars)
        {
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.localRotation = Quaternion.identity;

            RectTransform levelRect = level.GetComponent<RectTransform>();
            levelRect.localRotation = Quaternion.identity;

            informationPanelObject.SetActive(true);
            firstText.enabled = true;
            secondText.enabled = true;
            thirdText.enabled = true;
        }

        // Increment for spacing
        barNumber++;
        barGameObject.SetActive(true);

        ObjectStates[barGameObject] = true;
        HudData.GameObjects[element] = barGameObject;
        LayoutService.RegisterElement($"Bar.{element}", barRectTransform);
    }

    #endregion

    #region Vertical Progress Bar (Professions)

    /// <summary>
    /// Configures a vertical progress bar for professions.
    /// </summary>
    public static void ConfigureVertical(
        UICanvasBase canvasBase,
        Canvas targetInfoPanelCanvas,
        int layer,
        ref int graphBarNumber,
        ref GameObject barGameObject,
        ref Image progressFill,
        ref Image maxFill,
        ref LocalizedText level,
        Profession profession)
    {
        // Instantiate the bar object from the prefab
        barGameObject = UnityEngine.Object.Instantiate(canvasBase.TargetInfoParent.gameObject, targetInfoPanelCanvas.transform, false);
        RectTransform barRectTransform = barGameObject.GetComponent<RectTransform>();
        barRectTransform.gameObject.layer = layer;

        // Define the number of professions (bars)
        int totalBars = 8;

        // Calculate the total width and height for the bars
        float totalBarAreaWidth = 0.215f;
        float barWidth = totalBarAreaWidth / totalBars;

        // Calculate the starting X position to center the bar graph and position added bars appropriately
        float padding = 1f - 0.075f * 2.45f;
        float offsetX = padding + barWidth * graphBarNumber / 1.4f;

        // scale size
        Vector3 updatedScale = new(0.4f, 1f, 1f);
        barRectTransform.localScale = updatedScale;

        // positioning
        float offsetY = 0.24f;
        barRectTransform.anchorMin = new Vector2(offsetX, offsetY);
        barRectTransform.anchorMax = new Vector2(offsetX, offsetY);
        barRectTransform.pivot = new Vector2(offsetX, offsetY);

        // Assign fill and level text components
        progressFill = FindTargetUIObject(barRectTransform.transform, "Fill").GetComponent<Image>();
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;
        progressFill.color = ProfessionColors[profession];

        // Rotate the bar by 90 degrees around the Z-axis
        barRectTransform.localRotation = Quaternion.Euler(0, 0, 90);

        // Assign and adjust the level text component
        level = FindTargetUIObject(barRectTransform.transform, "LevelText").GetComponent<LocalizedText>();
        GameObject levelBackgroundObject = FindTargetUIObject(barRectTransform.transform, "LevelBackground");

        Image levelBackgroundImage = levelBackgroundObject.GetComponent<Image>();
        Sprite professionIcon = ProfessionIcons.TryGetValue(profession, out string spriteName) && HudData.Sprites.TryGetValue(spriteName, out Sprite sprite) ? sprite : levelBackgroundImage.sprite;
        levelBackgroundImage.sprite = professionIcon ?? levelBackgroundImage.sprite;
        levelBackgroundImage.color = new(1f, 1f, 1f, 1f);
        levelBackgroundObject.transform.localRotation = Quaternion.Euler(0, 0, -90);
        levelBackgroundObject.transform.localScale = new(0.25f, 1f, 1f);

        // Hide unnecessary UI elements
        var headerObject = FindTargetUIObject(barRectTransform.transform, "Name");
        headerObject?.SetActive(false);

        GameObject informationPanelObject = FindTargetUIObject(barRectTransform.transform, "InformationPanel");
        informationPanelObject?.SetActive(false);

        // Set these to 0 so don't appear
        FindTargetUIObject(barRectTransform.transform, "DamageTakenFill").GetComponent<Image>().fillAmount = 0f;
        maxFill = FindTargetUIObject(barRectTransform.transform, "AbsorbFill").GetComponent<Image>();
        maxFill.fillAmount = 0f;
        maxFill.transform.localScale = new(1f, 0.25f, 1f);
        maxFill.color = HudData.BrightGold;

        // Increment GraphBarNumber for horizontal spacing within the bar graph
        graphBarNumber++;

        barGameObject.SetActive(true);
        level.gameObject.SetActive(false);

        ObjectStates[barGameObject] = true;
        if (!ProfessionObjects.Contains(barGameObject))
            ProfessionObjects.Add(barGameObject);
        LayoutService.RegisterElement($"Profession.{profession}", barRectTransform);

        // Keyboard/Mouse layout
        float padding2 = 1f - 0.075f * 2.45f;
        float barWidth2 = 0.215f / 8;
        float offsetX_KM = padding2 + barWidth2 * graphBarNumber / 1.4f;
        float offsetY_KM = offsetY;
        Vector2 kmAnchorMin = new(offsetX_KM, offsetY_KM);
        Vector2 kmAnchorMax = new(offsetX_KM, offsetY_KM);
        Vector2 kmPivot = new(offsetX_KM, offsetY_KM);
        Vector2 kmPos = Vector2.zero;

        // Controller layout
        float ctrlBaseX = 0.6175f;
        float ctrlSpacingX = 0.015f;
        float offsetX_CTRL = ctrlBaseX + graphBarNumber * ctrlSpacingX;
        float offsetY_CTRL = 0.075f;
        Vector2 ctrlAnchorMin = new(offsetX_CTRL, offsetY_CTRL);
        Vector2 ctrlAnchorMax = new(offsetX_CTRL, offsetY_CTRL);
        Vector2 ctrlPivot = new(offsetX_CTRL, offsetY_CTRL);
        Vector2 ctrlPos = Vector2.zero;

        // Register positions
        RegisterAdaptiveElement(
            barGameObject,
            keyboardMousePos: kmPos,
            keyboardMouseAnchorMin: kmAnchorMin,
            keyboardMouseAnchorMax: kmAnchorMax,
            keyboardMousePivot: kmPivot,
            keyboardMouseScale: updatedScale,

            controllerPos: ctrlPos,
            controllerAnchorMin: ctrlAnchorMin,
            controllerAnchorMax: ctrlAnchorMax,
            controllerPivot: ctrlPivot,
            controllerScale: updatedScale * 0.85f
        );
    }

    #endregion

    #region Information Panel

    /// <summary>
    /// Configures the information panel for a progress bar.
    /// </summary>
    public static void ConfigureInformationPanel(
        ref GameObject informationPanelObject,
        ref LocalizedText firstText,
        ref LocalizedText secondText,
        ref LocalizedText thirdText,
        HudData.UIElement element)
    {
        switch (element)
        {
            case HudData.UIElement.Experience:
                ConfigureExperiencePanel(ref informationPanelObject, ref firstText, ref secondText, ref thirdText);
                break;
            default:
                ConfigureDefaultPanel(ref informationPanelObject, ref firstText, ref secondText, ref thirdText);
                break;
        }
    }

    /// <summary>
    /// Configures the experience panel with specific settings.
    /// </summary>
    public static void ConfigureExperiencePanel(
        ref GameObject panel,
        ref LocalizedText firstText,
        ref LocalizedText secondText,
        ref LocalizedText thirdText)
    {
        RectTransform panelTransform = panel.GetComponent<RectTransform>();
        Vector2 panelAnchoredPosition = panelTransform.anchoredPosition;
        panelAnchoredPosition.x = -18f;

        firstText = FindTargetUIObject(panel.transform, "BloodInfo").GetComponent<LocalizedText>();
        firstText.ForceSet("");
        firstText.enabled = false;

        GameObject affixesObject = FindTargetUIObject(panel.transform, "Affixes");
        LayoutElement layoutElement = affixesObject.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = false;

        secondText = affixesObject.GetComponent<LocalizedText>();
        secondText.ForceSet("");
        secondText.Text.fontSize *= 1.2f;
        secondText.enabled = false;

        thirdText = FindTargetUIObject(panel.transform, "PlatformUserName").GetComponent<LocalizedText>();
        thirdText.ForceSet("");
        thirdText.enabled = false;
    }

    /// <summary>
    /// Configures the default panel with standard settings.
    /// </summary>
    public static void ConfigureDefaultPanel(
        ref GameObject panel,
        ref LocalizedText firstText,
        ref LocalizedText secondText,
        ref LocalizedText thirdText)
    {
        RectTransform panelTransform = panel.GetComponent<RectTransform>();
        Vector2 panelAnchoredPosition = panelTransform.anchoredPosition;
        panelAnchoredPosition.x = -18f;

        firstText = FindTargetUIObject(panel.transform, "BloodInfo").GetComponent<LocalizedText>();
        firstText.ForceSet("");
        firstText.Text.fontSize *= 1.1f;
        firstText.enabled = false;

        GameObject affixesObject = FindTargetUIObject(panel.transform, "Affixes");
        LayoutElement layoutElement = affixesObject.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = false;

        secondText = affixesObject.GetComponent<LocalizedText>();
        secondText.ForceSet("");
        secondText.Text.fontSize *= 1.1f;
        secondText.enabled = false;

        thirdText = FindTargetUIObject(panel.transform, "PlatformUserName").GetComponent<LocalizedText>();
        thirdText.ForceSet("");
        thirdText.Text.fontSize *= 1.1f;
        thirdText.enabled = false;
    }

    #endregion
}
