using ProjectM.UI;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BloodUI.Services.CanvasService.DataHUD;
using static BloodUI.Utilities.GameObjects;
using BloodUI.Services;

namespace BloodUI.Services.HUD.Configurators;

/// <summary>
/// Configures class windows (class list, class spells) for the HUD.
/// Extracted from CanvasService.ConfigureHUD.ConfigureClassWindow().
/// </summary>
internal static class ClassWindowConfigurator
{
    /// <summary>
    /// Configures a class window with all required UI components.
    /// </summary>
    public static void Configure(
        UICanvasBase canvasBase,
        Canvas bottomBarCanvas,
        int layer,
        ref GameObject classObject,
        string layoutKey,
        string title,
        Color headerColor,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 anchoredPosition,
        ref LocalizedText header,
        ref LocalizedText subHeader,
        ref Transform entriesRoot,
        ref GameObject entryTemplate)
    {
        if (canvasBase?.BottomBarParentPrefab?.FakeTooltip == null || bottomBarCanvas == null)
        {
            FailConfigure(ref classObject, ref header, ref subHeader, ref entriesRoot, ref entryTemplate, layoutKey, "FakeTooltip prefab not found");
            return;
        }

        classObject = UnityEngine.Object.Instantiate(canvasBase.BottomBarParentPrefab.FakeTooltip.gameObject, bottomBarCanvas.transform, false);
        if (classObject == null)
        {
            FailConfigure(ref classObject, ref header, ref subHeader, ref entriesRoot, ref entryTemplate, layoutKey, "failed to instantiate FakeTooltip");
            return;
        }
        RectTransform classTransform = classObject.GetComponent<RectTransform>();
        if (classTransform == null)
        {
            FailConfigure(ref classObject, ref header, ref subHeader, ref entriesRoot, ref entryTemplate, layoutKey, "RectTransform not found");
            return;
        }

        classTransform.gameObject.layer = layer;
        classObject.SetActive(true);

        GameObject entries = FindTargetUIObject(classObject.transform, "InformationEntries");
        if (entries == null)
        {
            FailConfigure(ref classObject, ref header, ref subHeader, ref entriesRoot, ref entryTemplate, layoutKey, "InformationEntries not found");
            return;
        }

        DeactivateChildrenExceptNamed(entries.transform, "TooltipHeader");

        GameObject tooltipHeader = FindTargetUIObject(classObject.transform, "TooltipHeader");
        if (tooltipHeader == null)
        {
            FailConfigure(ref classObject, ref header, ref subHeader, ref entriesRoot, ref entryTemplate, layoutKey, "TooltipHeader not found");
            return;
        }

        tooltipHeader.SetActive(true);

        GameObject iconNameObject = FindTargetUIObject(tooltipHeader.transform, "Icon&Name");
        if (iconNameObject == null)
        {
            FailConfigure(ref classObject, ref header, ref subHeader, ref entriesRoot, ref entryTemplate, layoutKey, "Icon&Name not found");
            return;
        }

        iconNameObject.SetActive(true);

        GameObject levelFrame = FindTargetUIObject(iconNameObject.transform, "LevelFrame");
        if (levelFrame != null) levelFrame.SetActive(false);

        GameObject reforgeCost = FindTargetUIObject(classObject.transform, "Tooltip_ReforgeCost");
        if (reforgeCost != null) reforgeCost.SetActive(false);

        GameObject tooltipIcon = FindTargetUIObject(tooltipHeader.transform, "TooltipIcon");
        if (tooltipIcon != null) tooltipIcon.SetActive(false);

        GameObject subHeaderObject = FindTargetUIObject(iconNameObject.transform, "TooltipSubHeader");
        GameObject headerObject = FindTargetUIObject(iconNameObject.transform, "TooltipHeader");
        if (subHeaderObject == null || headerObject == null)
        {
            FailConfigure(ref classObject, ref header, ref subHeader, ref entriesRoot, ref entryTemplate, layoutKey, "header text objects not found");
            return;
        }

        header = headerObject.GetComponent<LocalizedText>();
        subHeader = subHeaderObject.GetComponent<LocalizedText>();
        if (header == null || subHeader == null)
        {
            FailConfigure(ref classObject, ref header, ref subHeader, ref entriesRoot, ref entryTemplate, layoutKey, "LocalizedText components missing");
            return;
        }
        if (header.Text == null || subHeader.Text == null)
        {
            FailConfigure(ref classObject, ref header, ref subHeader, ref entriesRoot, ref entryTemplate, layoutKey, "text components missing");
            return;
        }

        header.Text.color = headerColor;
        header.ForceSet(title);

        subHeader.Text.enableAutoSizing = false;
        subHeader.Text.enableWordWrapping = false;
        subHeader.ForceSet(string.Empty);

        float widthScale = 0.65f;
        float heightScale = 0.9f;
        classTransform.sizeDelta = new Vector2(classTransform.sizeDelta.x * widthScale, classTransform.sizeDelta.y * heightScale);

        classTransform.anchorMin = anchor;
        classTransform.anchorMax = anchor;
        classTransform.pivot = pivot;
        classTransform.anchoredPosition = anchoredPosition;
        classTransform.localScale = new Vector3(0.7f, 0.7f, 1f);

        entriesRoot = entries.transform;

        entryTemplate = UnityEngine.Object.Instantiate(subHeaderObject, entriesRoot, false);
        if (entryTemplate == null)
        {
            FailConfigure(ref classObject, ref header, ref subHeader, ref entriesRoot, ref entryTemplate, layoutKey, "failed to create entry template");
            return;
        }
        entryTemplate.name = $"{layoutKey}.EntryTemplate";

        LocalizedText templateText = entryTemplate.GetComponent<LocalizedText>();
        if (!TryBindLocalizedText(templateText, $"{layoutKey} entry template"))
        {
            FailConfigure(ref classObject, ref header, ref subHeader, ref entriesRoot, ref entryTemplate, layoutKey, "entry template text missing");
            return;
        }
        templateText.Text.enableAutoSizing = false;
        templateText.Text.enableWordWrapping = false;
        templateText.Text.raycastTarget = true;

        ApplyTransparentGraphic(entryTemplate, "Class entry template");

        entryTemplate.SetActive(false);

        ObjectStates.Add(classObject, true);
        LayoutService.RegisterElement(layoutKey, classTransform);
    }

    /// <summary>
    /// Cleans up a partially created class window and clears all related references when setup fails.
    /// </summary>
    static void FailConfigure(
        ref GameObject classObject,
        ref LocalizedText header,
        ref LocalizedText subHeader,
        ref Transform entriesRoot,
        ref GameObject entryTemplate,
        string layoutKey,
        string reason)
    {
        DebugToolsBridge.TryLogWarning($"Failed to configure {layoutKey}, {reason}.");
        if (classObject != null)
        {
            classObject.SetActive(false);
            UnityEngine.Object.Destroy(classObject);
        }

        classObject = null;
        header = null;
        subHeader = null;
        entriesRoot = null;
        entryTemplate = null;
    }

    /// <summary>
    /// Ensures a LocalizedText has a bound text component, attempting to bind it if missing.
    /// </summary>
    internal static bool TryBindLocalizedText(LocalizedText localizedText, string context)
    {
        if (localizedText == null)
        {
            DebugToolsBridge.TryLogWarning($"{context}: LocalizedText missing.");
            return false;
        }

        if (localizedText.Text != null)
        {
            return true;
        }

        Type localizedType = typeof(LocalizedText);
        PropertyInfo property = localizedType.GetProperty("Text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo field = localizedType.GetField("Text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? localizedType.GetField("<Text>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        Type targetType = property?.PropertyType ?? field?.FieldType;
        if (targetType == null)
        {
            DebugToolsBridge.TryLogWarning($"{context}: Unable to resolve text type.");
            return false;
        }

        Component textComponent = ResolveTextComponent(localizedText, targetType);
        if (textComponent == null)
        {
            DebugToolsBridge.TryLogWarning($"{context}: Missing text component for {targetType.Name}.");
            return false;
        }

        try
        {
            if (property != null && property.CanWrite && targetType.IsInstanceOfType(textComponent))
            {
                property.SetValue(localizedText, textComponent);
                return localizedText.Text != null;
            }

            if (field != null && targetType.IsInstanceOfType(textComponent))
            {
                field.SetValue(localizedText, textComponent);
                return localizedText.Text != null;
            }
        }
        catch (Exception ex)
        {
            DebugToolsBridge.TryLogWarning($"{context}: Failed to bind text component ({ex.GetType().Name}).");
            return false;
        }

        DebugToolsBridge.TryLogWarning($"{context}: Unable to bind text component.");
        return false;
    }

    static Component ResolveTextComponent(LocalizedText localizedText, Type targetType)
    {
        if (targetType == typeof(TextMeshProUGUI))
        {
            return localizedText.GetComponent<TextMeshProUGUI>() ?? localizedText.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (targetType == typeof(TextMeshPro))
        {
            return localizedText.GetComponent<TextMeshPro>() ?? localizedText.GetComponentInChildren<TextMeshPro>(true);
        }

        if (targetType == typeof(TMP_Text))
        {
            return localizedText.GetComponent<TMP_Text>() ?? localizedText.GetComponentInChildren<TMP_Text>(true);
        }

        if (typeof(TMP_Text).IsAssignableFrom(targetType))
        {
            return localizedText.GetComponent<TMP_Text>() ?? localizedText.GetComponentInChildren<TMP_Text>(true);
        }

        return null;
    }

    /// <summary>
    /// Applies a transparent graphic to enable raycasting on a target GameObject.
    /// </summary>
    internal static void ApplyTransparentGraphic(GameObject target, string context)
    {
        if (target == null)
        {
            DebugToolsBridge.TryLogWarning($"{context}: Target missing for graphic setup.");
            return;
        }

        Graphic graphic = target.GetComponent<Graphic>();
        if (graphic == null)
        {
            graphic = target.AddComponent<Image>();
        }

        if (graphic == null)
        {
            DebugToolsBridge.TryLogWarning($"{context}: Graphic component missing.");
            return;
        }

        graphic.raycastTarget = true;

        Image image = graphic as Image;
        if (image != null)
        {
            image.color = new Color(0f, 0f, 0f, 0f);
        }
    }
}
