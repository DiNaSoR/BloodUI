using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Services.CharacterMenu.Shared;

/// <summary>
/// Factory methods for creating Character Menu UI elements.
/// Extracted from CharacterMenuService to reduce file size.
/// </summary>
internal static class UIFactory
{
    #region Constants

    public const float HeaderFontScale = 1.3f;
    public const float SubHeaderFontScale = 0.87f;
    public const float EntryFontScale = 0.87f;
    public const float SubTabFontScale = 0.5f;
    public const float SubTabHeightScale = 1.0f;
    public const int ContentPaddingLeft = 24;
    public const int ContentPaddingRight = 24;
    public const int ContentPaddingTop = 12;
    public const int ContentPaddingBottom = 12;
    public const int SubTabPaddingLeft = 24;
    public const int SubTabPaddingRight = 24;
    public const int SubTabPaddingTop = 0;
    public const int SubTabPaddingBottom = 0;
    public const float SubTabSpacing = 0f;
    public const float ProfessionHeaderFontScale = 0.93f;
    public const float ProfessionFontScale = 0.82f;
    public const float ProfessionRowHeight = 30f;
    public const float ProfessionIconSize = 24f;
    public const float ProfessionNameWidth = 160f;
    public const float ProfessionLevelWidth = 52f;
    public const float ProfessionProgressWidth = 160f;
    public const float ProfessionProgressHeight = 6f;
    public const float ProfessionPercentWidth = 52f;

    #endregion

    #region Core UI Creation

    /// <summary>
    /// Creates a RectTransform object with Il2Cpp type array for proper Unity instantiation.
    /// </summary>
    public static RectTransform CreateRectTransformObject(string name, Transform parent)
    {
        if (parent == null || parent.Equals(null))
        {
            return null;
        }

        var components = new Il2CppReferenceArray<Il2CppSystem.Type>(1);
        components[0] = Il2CppType.Of<RectTransform>();
        GameObject obj = new(name, components);
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    /// <summary>
    /// Creates a simple RectTransform object.
    /// </summary>
    public static RectTransform CreateSimpleRectTransform(string name, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    /// <summary>
    /// Configures a RectTransform with top-left anchoring.
    /// </summary>
    public static void ConfigureTopLeftAnchoring(RectTransform rectTransform)
    {
        if (rectTransform == null) return;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    #endregion

    #region Layout Components

    /// <summary>
    /// Ensures a transform has a VerticalLayoutGroup component.
    /// </summary>
    public static VerticalLayoutGroup EnsureVerticalLayout(Transform target, int paddingLeft = 0, int paddingRight = 0, int paddingTop = 0, int paddingBottom = 0)
    {
        if (target == null) return null;

        var layout = target.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.padding = CreatePadding(paddingLeft, paddingRight, paddingTop, paddingBottom);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return layout;
    }

    /// <summary>
    /// Creates a HorizontalLayoutGroup with standard settings.
    /// </summary>
    public static HorizontalLayoutGroup CreateHorizontalLayout(RectTransform target, float spacing = 0f, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        if (target == null) return null;

        var layout = target.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = alignment;
        layout.spacing = spacing;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        return layout;
    }

    /// <summary>
    /// Creates a RectOffset for padding.
    /// </summary>
    public static RectOffset CreatePadding(int left, int right, int top, int bottom)
    {
        RectOffset padding = new();
        padding.left = left;
        padding.right = right;
        padding.top = top;
        padding.bottom = bottom;
        return padding;
    }

    /// <summary>
    /// Clears all children from a transform.
    /// </summary>
    public static void ClearChildren(Transform root)
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
        }
    }

    #endregion

    #region Text Elements

    /// <summary>
    /// Creates a section header text element.
    /// </summary>
    public static TextMeshProUGUI CreateSectionHeader(Transform parent, TextMeshProUGUI reference, string text)
    {
        if (parent == null || reference == null) return null;

        RectTransform rectTransform = CreateRectTransformObject("SectionHeader", parent);
        if (rectTransform == null) return null;

        ConfigureTopLeftAnchoring(rectTransform);

        var textComponent = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        CopyTextStyle(reference, textComponent);
        textComponent.text = text;
        textComponent.fontSize = reference.fontSize * HeaderFontScale;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.enableWordWrapping = false;
        textComponent.overflowMode = TextOverflowModes.Ellipsis;
        textComponent.raycastTarget = false;

        Color baseColor = textComponent.color;
        textComponent.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.95f);

        var layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = textComponent.fontSize * 1.5f;

        return textComponent;
    }

    /// <summary>
    /// Creates a section sub-header text element.
    /// </summary>
    public static TextMeshProUGUI CreateSectionSubHeader(Transform parent, TextMeshProUGUI reference, string text, bool applyAlphaFade = true)
    {
        if (parent == null || reference == null) return null;

        RectTransform rectTransform = CreateRectTransformObject("SectionSubHeader", parent);
        if (rectTransform == null) return null;

        ConfigureTopLeftAnchoring(rectTransform);

        var textComponent = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        CopyTextStyle(reference, textComponent);
        textComponent.text = text;
        textComponent.fontSize = reference.fontSize * SubHeaderFontScale;
        textComponent.fontStyle = FontStyles.Normal;
        textComponent.alignment = TextAlignmentOptions.Center;

        if (applyAlphaFade)
        {
            Color color = reference.color;
            textComponent.color = new Color(color.r, color.g, color.b, 0.8f);
        }

        var layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = textComponent.fontSize * 1.3f;

        return textComponent;
    }

    /// <summary>
    /// Creates a text element with specified styling.
    /// </summary>
    public static TextMeshProUGUI CreateText(Transform parent, TextMeshProUGUI reference, string text, float fontSize, TextAlignmentOptions alignment)
    {
        if (parent == null || reference == null) return null;

        RectTransform rectTransform = CreateRectTransformObject("Text", parent);
        if (rectTransform == null) return null;

        var textComponent = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        CopyTextStyle(reference, textComponent);
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;

        var layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = fontSize * 1.3f;

        return textComponent;
    }

    /// <summary>
    /// Creates a text element styled as a Bloodcraft entry.
    /// </summary>
    public static TextMeshProUGUI CreateTextElement(Transform parent, string name, TextMeshProUGUI reference, float fontScale, FontStyles style)
    {
        if (parent == null || reference == null) return null;

        RectTransform rectTransform = CreateRectTransformObject(name, parent);
        if (rectTransform == null) return null;

        ConfigureTopLeftAnchoring(rectTransform);

        var textComponent = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        CopyTextStyle(reference, textComponent);
        textComponent.fontSize = reference.fontSize * fontScale;
        textComponent.fontStyle = style;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.enableAutoSizing = false;
        textComponent.enableWordWrapping = false;
        textComponent.raycastTarget = false;

        LayoutElement referenceLayout = reference.GetComponent<LayoutElement>();
        if (referenceLayout != null)
        {
            var layout = rectTransform.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = referenceLayout.minHeight;
            layout.preferredHeight = referenceLayout.preferredHeight;
        }

        return textComponent;
    }

    /// <summary>
    /// Creates an entry text element for lists.
    /// </summary>
    public static TextMeshProUGUI CreateEntry(Transform parent, TextMeshProUGUI reference, string text, FontStyles style = FontStyles.Normal)
    {
        if (parent == null || reference == null) return null;

        RectTransform rectTransform = CreateRectTransformObject("Entry", parent);
        if (rectTransform == null) return null;

        ConfigureTopLeftAnchoring(rectTransform);

        var textComponent = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        CopyTextStyle(reference, textComponent);
        textComponent.text = text;
        textComponent.fontSize = reference.fontSize * EntryFontScale;
        textComponent.fontStyle = style;
        textComponent.alignment = TextAlignmentOptions.Left;

        var layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = textComponent.fontSize * 1.3f;

        return textComponent;
    }

    /// <summary>
    /// Copies text style from one TextMeshProUGUI to another.
    /// </summary>
    public static void CopyTextStyle(TextMeshProUGUI source, TextMeshProUGUI target)
    {
        if (source == null || target == null) return;

        target.font = source.font;
        target.fontMaterial = source.fontMaterial;
        target.color = source.color;
        target.enableAutoSizing = false;
        target.richText = true;
    }

    #endregion

    #region Visual Elements

    /// <summary>
    /// Creates a divider line element.
    /// </summary>
    public static RectTransform CreateDividerLine(Transform parent, float height = 2f, Color? color = null)
    {
        if (parent == null) return null;

        RectTransform rectTransform = CreateRectTransformObject("Divider", parent);
        if (rectTransform == null) return null;

        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.color = color ?? new Color(0.3f, 0.3f, 0.3f, 0.5f);
        image.raycastTarget = false;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.flexibleWidth = 1f;

        return rectTransform;
    }

    /// <summary>
    /// Creates a vertical spacer element (for vertical layouts).
    /// </summary>
    public static void AddSpacer(Transform parent, float height)
    {
        if (parent == null) return;

        RectTransform rectTransform = CreateRectTransformObject("Spacer", parent);
        if (rectTransform == null) return;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.flexibleWidth = 1f;
    }

    /// <summary>
    /// Creates a horizontal spacer element (for horizontal layouts).
    /// </summary>
    public static void AddHorizontalSpacer(Transform parent, float width, float height)
    {
        if (parent == null) return;

        RectTransform rectTransform = CreateRectTransformObject("Spacer", parent);
        if (rectTransform == null) return;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.minWidth = width;
        layout.preferredHeight = height;
    }

    /// <summary>
    /// Creates an image element.
    /// </summary>
    public static Image CreateImage(Transform parent, string name, Vector2 size, Color? color = null)
    {
        if (parent == null) return null;

        RectTransform rectTransform = CreateRectTransformObject(name, parent);
        if (rectTransform == null) return null;

        rectTransform.sizeDelta = size;

        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.color = color ?? Color.white;
        image.raycastTarget = false;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;

        return image;
    }

    /// <summary>
    /// Creates a progress bar element.
    /// </summary>
    public static (RectTransform root, Image background, Image fill) CreateProgressBar(Transform parent, string name, Vector2 size, Color backgroundColor, Color fillColor)
    {
        if (parent == null) return (null, null, null);

        RectTransform root = CreateRectTransformObject(name, parent);
        if (root == null) return (null, null, null);

        root.sizeDelta = size;

        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;

        // Background
        Image background = root.gameObject.AddComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;

        // Fill
        RectTransform fillRect = CreateRectTransformObject("Fill", root);
        if (fillRect != null)
        {
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.color = fillColor;
            fill.raycastTarget = false;

            return (root, background, fill);
        }

        return (root, background, null);
    }

    #endregion

    #region Container Elements

    /// <summary>
    /// Creates a content root container with padding.
    /// </summary>
    public static Transform CreateContentRoot(Transform parent)
    {
        if (parent == null) return null;

        RectTransform rectTransform = CreateRectTransformObject("BloodcraftContent", parent);
        if (rectTransform == null) return null;

        ConfigureTopLeftAnchoring(rectTransform);
        EnsureVerticalLayout(rectTransform, ContentPaddingLeft, ContentPaddingRight, ContentPaddingTop, ContentPaddingBottom);

        ContentSizeFitter fitter = rectTransform.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rectTransform;
    }

    /// <summary>
    /// Creates a padded section container.
    /// </summary>
    public static Transform CreatePaddedSectionRoot(Transform parent, string name)
    {
        if (parent == null) return null;

        RectTransform rectTransform = CreateRectTransformObject(name, parent);
        if (rectTransform == null) return null;

        ConfigureTopLeftAnchoring(rectTransform);
        EnsureVerticalLayout(rectTransform, SubTabPaddingLeft, SubTabPaddingRight, SubTabPaddingTop, SubTabPaddingBottom);

        return rectTransform;
    }

    /// <summary>
    /// Creates a container for text entries.
    /// </summary>
    public static Transform CreateEntriesRoot(Transform parent, float spacing = 4f)
    {
        if (parent == null) return null;

        RectTransform rectTransform = CreateRectTransformObject("BloodcraftEntries", parent);
        if (rectTransform == null) return null;

        ConfigureTopLeftAnchoring(rectTransform);
        var layout = EnsureVerticalLayout(rectTransform);
        if (layout != null)
        {
            layout.spacing = spacing;
        }

        return rectTransform;
    }

    /// <summary>
    /// Creates a list container with vertical layout.
    /// </summary>
    public static Transform CreateListRoot(Transform parent, string name, float spacing = 4f)
    {
        if (parent == null) return null;

        RectTransform rectTransform = CreateRectTransformObject(name, parent);
        if (rectTransform == null) return null;

        ConfigureTopLeftAnchoring(rectTransform);
        var layout = EnsureVerticalLayout(rectTransform);
        if (layout != null)
        {
            layout.spacing = spacing;
        }

        return rectTransform;
    }

    #endregion

    #region Entry Templates

    /// <summary>
    /// Creates a reusable entry template for Bloodcraft tab rows.
    /// </summary>
    public static (GameObject template, TextMeshProUGUI text) CreateEntryTemplate(Transform parent, TextMeshProUGUI reference)
    {
        if (parent == null || reference == null) return (null, null);

        RectTransform rectTransform = CreateRectTransformObject("BloodcraftEntryTemplate", parent);
        if (rectTransform == null) return (null, null);

        ConfigureTopLeftAnchoring(rectTransform);

        TextMeshProUGUI text = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        CopyTextStyle(reference, text);
        text.fontSize = reference.fontSize * EntryFontScale;
        text.alignment = TextAlignmentOptions.Center;
        text.text = string.Empty;
        text.raycastTarget = true;
        text.enableAutoSizing = false;
        text.enableWordWrapping = false;
        text.richText = true;

        // Always add a LayoutElement with proper height for vertical layouts
        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        LayoutElement referenceLayout = reference.GetComponent<LayoutElement>();
        if (referenceLayout != null)
        {
            layout.minHeight = referenceLayout.minHeight;
            layout.preferredHeight = referenceLayout.preferredHeight;
            layout.preferredWidth = referenceLayout.preferredWidth;
            layout.flexibleHeight = referenceLayout.flexibleHeight;
        }
        else
        {
            // Fallback: calculate height based on font size
            float entryHeight = text.fontSize * 1.5f;
            layout.minHeight = entryHeight;
            layout.preferredHeight = entryHeight;
        }

        rectTransform.gameObject.SetActive(false);
        return (rectTransform.gameObject, text);
    }

    #endregion

    #region SubTab Utilities

    /// <summary>
    /// Stretches sub-tab background graphics so adjacent tabs touch without visual gaps.
    /// </summary>
    public static void StretchSubTabGraphics(GameObject buttonObject)
    {
        if (buttonObject == null) return;

        Image[] images = buttonObject.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null) continue;

            RectTransform rectTransform = image.GetComponent<RectTransform>();
            if (rectTransform == null) continue;

            bool shouldStretch = rectTransform.transform == buttonObject.transform;
            if (!shouldStretch)
            {
                string name = rectTransform.gameObject.name;
                shouldStretch = !string.IsNullOrWhiteSpace(name)
                    && name.IndexOf("background", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (!shouldStretch) continue;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;

            if (rectTransform.transform != buttonObject.transform)
            {
                rectTransform.SetAsFirstSibling();
            }
        }
    }

    /// <summary>
    /// Resolves the font size for sub-tab labels based on height and desired size.
    /// </summary>
    public static float ResolveSubTabFontSize(float targetHeight, float desiredFontSize)
    {
        float maxFontSize = Mathf.Max(10f, targetHeight * 0.65f);
        if (desiredFontSize <= 0f) return maxFontSize;
        return Mathf.Min(desiredFontSize, maxFontSize);
    }

    /// <summary>
    /// Applies sub-tab text sizing to an array of labels with fallback support.
    /// </summary>
    public static void ApplySubTabTextSizing(TMP_Text[] templateLabels, TMP_Text fallbackLabel, float targetHeight, float desiredFontSize)
    {
        float finalFontSize = ResolveSubTabFontSize(targetHeight, desiredFontSize);

        for (int i = 0; i < templateLabels.Length; i++)
        {
            TMP_Text label = templateLabels[i];
            if (label == null) continue;
            ApplySubTabLabelStyle(label, finalFontSize);
        }

        if (fallbackLabel != null)
        {
            ApplySubTabLabelStyle(fallbackLabel, finalFontSize);
        }
    }

    /// <summary>
    /// Applies sub-tab text sizing to a list of labels.
    /// </summary>
    public static void ApplySubTabTextSizing(IReadOnlyList<TMP_Text> labels, float targetHeight, float desiredFontSize)
    {
        if (labels == null || labels.Count == 0) return;

        float finalFontSize = ResolveSubTabFontSize(targetHeight, desiredFontSize);
        for (int i = 0; i < labels.Count; i++)
        {
            TMP_Text label = labels[i];
            if (label == null) continue;
            ApplySubTabLabelStyle(label, finalFontSize);
        }
    }

    /// <summary>
    /// Applies standard styling to a sub-tab label.
    /// </summary>
    public static void ApplySubTabLabelStyle(TMP_Text label, float fontSize)
    {
        if (label == null) return;

        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = Vector4.zero;
        ConfigureSubTabLabelRect(label);
    }

    /// <summary>
    /// Configures a sub-tab label's RectTransform for full stretch.
    /// </summary>
    public static void ConfigureSubTabLabelRect(TMP_Text label)
    {
        RectTransform rectTransform = label?.rectTransform;
        if (rectTransform == null) return;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    /// <summary>
    /// Creates a sub-tab label with proper styling.
    /// </summary>
    public static TMP_Text CreateSubTabLabel(Transform parent, TextMeshProUGUI reference, string label, float fontScale)
    {
        if (parent == null || parent.Equals(null) || reference == null) return null;

        RectTransform rectTransform = CreateRectTransformObject("SubTabLabel", parent);
        if (rectTransform == null) return null;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.SetAsLastSibling();

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;

        TextMeshProUGUI text = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        CopyTextStyle(reference, text);
        text.fontSize = reference.fontSize * fontScale;
        text.fontStyle = FontStyles.Normal;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.maskable = false;
        text.raycastTarget = false;
        text.text = label;
        text.enabled = true;
        text.margin = Vector4.zero;
        ConfigureSubTabLabelRect(text);

        Color color = reference.color;
        if (color.a < 0.1f) color.a = 1f;
        text.color = color;

        return text;
    }

    #endregion
}
