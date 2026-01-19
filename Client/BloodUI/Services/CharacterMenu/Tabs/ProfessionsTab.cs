using BloodUI.Services.CharacterMenu.Base;
using BloodUI.Services.CharacterMenu.Interfaces;
using BloodUI.Services.CharacterMenu.Shared;
using BloodUI.Services.HUD.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BloodUI.Services.CanvasService.DataHUD;
using static BloodUI.Services.DataService;
using BloodUI.Services;

namespace BloodUI.Services.CharacterMenu.Tabs;

/// <summary>
/// Character menu tab for displaying profession progress.
/// Panel-based implementation extracted from the legacy CharacterMenuService.
/// </summary>
internal class ProfessionsTab : CharacterMenuTabBase, ICharacterMenuTabWithPanel
{
    #region Constants

    private const float HeaderBackgroundAlpha = 0.95f;
    private const float RowBackgroundAlpha = 0.35f;
    private const float RowSpacing = 4f;
    private const float RowHorizontalSpacing = 12f;
    private const int RowPaddingHorizontal = 8;
    private const int RowPaddingVertical = 4;
    private const float DividerOpacity = 0.7f;

    private const float HeaderFontScale = UIFactory.ProfessionHeaderFontScale;
    private const float RowFontScale = UIFactory.ProfessionFontScale;

    private const float RowHeight = UIFactory.ProfessionRowHeight;
    private const float IconSize = UIFactory.ProfessionIconSize;
    private const float NameWidth = UIFactory.ProfessionNameWidth;
    private const float LevelWidth = UIFactory.ProfessionLevelWidth;
    private const float ProgressMinWidth = UIFactory.ProfessionProgressWidth;
    private const float ProgressHeight = UIFactory.ProfessionProgressHeight;
    private const float PercentWidth = UIFactory.ProfessionPercentWidth;

    private static readonly Color HeaderBackgroundColor = new(0.1f, 0.1f, 0.12f, HeaderBackgroundAlpha);
    private static readonly Color RowBackgroundColor = new(0f, 0f, 0f, RowBackgroundAlpha);

    private static readonly string[] HeaderSpriteNames = ["Act_BG", "TabGradient", "Window_Box_Background"];
    private static readonly string[] DividerSpriteNames = ["Divider_Horizontal", "Window_Divider_Horizontal_V_Red"];
    private static readonly string[] ProgressBackgroundSpriteNames =
        ["SimpleProgressBar_Empty_Default", "SimpleProgressBar_Mask", "Window_Box_Background"];
    private static readonly string[] ProgressFillSpriteNames =
        ["SimpleProgressBar_Fill", "ChargeRefill", "SimpleProgressBar_Mask"];

    #endregion

    #region Fields

    private RectTransform _panelRoot;
    private Transform _listRoot;
    private TextMeshProUGUI _statusText;
    private TextMeshProUGUI _summaryText;
    private TextMeshProUGUI _referenceText;
    private readonly List<ProfessionRow> _rows = [];

    #endregion

    #region Properties

    public override string TabId => "Professions";
    public override string TabLabel => "Professions";
    public override string SectionTitle => "Professions";
    public override BloodcraftTab TabType => BloodcraftTab.Progression;

    #endregion

    #region ICharacterMenuTabWithPanel

    public Transform CreatePanel(Transform parent, TextMeshProUGUI reference)
    {
        Reset();
        _referenceText = reference;

        RectTransform rectTransform = CreateRectTransformObject("BloodcraftProfessions", parent);
        if (rectTransform == null)
        {
            return null;
        }

        UIFactory.ConfigureTopLeftAnchoring(rectTransform);
        EnsureVerticalLayout(rectTransform, spacing: RowSpacing);

        _statusText = UIFactory.CreateSectionSubHeader(rectTransform, reference, string.Empty);
        if (_statusText != null)
        {
            _statusText.alignment = TextAlignmentOptions.Left;
        }

        _ = CreateProfessionHeaderRow(rectTransform, reference);
        _listRoot = UIFactory.CreateListRoot(rectTransform, "ProfessionList", RowSpacing);
        _ = CreateDividerLine(rectTransform);
        _summaryText = UIFactory.CreateSectionSubHeader(rectTransform, reference, string.Empty);

        rectTransform.gameObject.SetActive(false);
        _panelRoot = rectTransform;
        return rectTransform;
    }

    public void UpdatePanel()
    {
        if (_panelRoot == null)
        {
            return;
        }

        if (!HudConfiguration.ProfessionBarsEnabled)
        {
            if (_statusText != null)
            {
                _statusText.text = "Profession UI disabled.";
                _statusText.gameObject.SetActive(true);
            }

            if (_listRoot != null)
            {
                _listRoot.gameObject.SetActive(false);
            }

            if (_summaryText != null)
            {
                _summaryText.text = string.Empty;
            }

            return;
        }

        if (_statusText != null)
        {
            _statusText.text = string.Empty;
            _statusText.gameObject.SetActive(false);
        }

        if (_listRoot != null)
        {
            _listRoot.gameObject.SetActive(true);
        }

        List<ProfessionEntry> entries = [..GetProfessionEntries()];
        EnsureRows(entries.Count);

        int rowCount = Math.Min(entries.Count, _rows.Count);
        for (int i = 0; i < rowCount; i++)
        {
            UpdateRow(_rows[i], entries[i]);
        }

        if (_summaryText != null)
        {
            _summaryText.text = BuildProfessionSummaryText(entries);
        }
    }

    #endregion

    #region Lifecycle

    public override void Update()
    {
        UpdatePanel();
    }

    public override void Reset()
    {
        base.Reset();
        _panelRoot = null;
        _listRoot = null;
        _statusText = null;
        _summaryText = null;
        _referenceText = null;
        _rows.Clear();
    }

    #endregion

    #region Private Methods

    private static IEnumerable<ProfessionEntry> GetProfessionEntries()
    {
        return new[]
        {
            new ProfessionEntry(Profession.Enchanting, _enchantingLevel, _enchantingProgress),
            new ProfessionEntry(Profession.Alchemy, _alchemyLevel, _alchemyProgress),
            new ProfessionEntry(Profession.Harvesting, _harvestingLevel, _harvestingProgress),
            new ProfessionEntry(Profession.Blacksmithing, _blacksmithingLevel, _blacksmithingProgress),
            new ProfessionEntry(Profession.Tailoring, _tailoringLevel, _tailoringProgress),
            new ProfessionEntry(Profession.Woodcutting, _woodcuttingLevel, _woodcuttingProgress),
            new ProfessionEntry(Profession.Mining, _miningLevel, _miningProgress),
            new ProfessionEntry(Profession.Fishing, _fishingLevel, _fishingProgress)
        };
    }

    private void EnsureRows(int count)
    {
        if (_listRoot == null)
        {
            return;
        }

        while (_rows.Count < count)
        {
            ProfessionRow row = CreateProfessionRow(_listRoot);
            if (row == null)
            {
                break;
            }

            _rows.Add(row);
        }

        for (int i = 0; i < _rows.Count; i++)
        {
            bool isActive = i < count;
            _rows[i].Root.SetActive(isActive);
        }
    }

    private ProfessionRow CreateProfessionRow(Transform parent)
    {
        if (_referenceText == null)
        {
            return null;
        }

        RectTransform rectTransform = CreateRectTransformObject($"ProfessionRow_{_rows.Count + 1}", parent);
        if (rectTransform == null)
        {
            return null;
        }

        UIFactory.ConfigureTopLeftAnchoring(rectTransform);

        Image background = rectTransform.gameObject.AddComponent<Image>();
        background.sprite = ResolveProgressBackgroundSprite();
        background.color = RowBackgroundColor;
        background.type = Image.Type.Sliced;
        background.raycastTarget = false;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = RowHorizontalSpacing;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.padding = CreatePadding(RowPaddingHorizontal, RowPaddingHorizontal, RowPaddingVertical, RowPaddingVertical);

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = RowHeight;
        rowLayout.minHeight = RowHeight;

        Image icon = CreateProfessionIcon(rectTransform);
        TextMeshProUGUI nameText = CreateRowLabel(rectTransform, _referenceText, NameWidth, TextAlignmentOptions.Left);
        TextMeshProUGUI levelText = CreateRowLabel(rectTransform, _referenceText, LevelWidth, TextAlignmentOptions.Center);
        Image progressFill = CreateProgressBar(rectTransform, out Image progressBackground);
        TextMeshProUGUI progressText = CreateRowLabel(rectTransform, _referenceText, PercentWidth, TextAlignmentOptions.Right);

        return new ProfessionRow(rectTransform.gameObject, icon, nameText, levelText, progressFill, progressBackground, progressText);
    }

    private static void UpdateRow(ProfessionRow row, ProfessionEntry entry)
    {
        if (row == null)
        {
            return;
        }

        Sprite iconSprite = ResolveProfessionIcon(entry.Profession);
        if (row.Icon != null)
        {
            if (iconSprite != null)
            {
                row.Icon.sprite = iconSprite;
                row.Icon.enabled = true;
            }
            else
            {
                row.Icon.enabled = false;
            }
        }

        string name = HudUtilities.SplitPascalCase(entry.Profession.ToString());
        if (row.NameText != null)
        {
            row.NameText.text = name;
        }

        if (row.LevelText != null)
        {
            row.LevelText.text = entry.Level.ToString(CultureInfo.InvariantCulture);
        }

        float progress = Mathf.Clamp01(entry.Progress);
        if (row.ProgressFill != null)
        {
            row.ProgressFill.fillAmount = progress;
        }

        if (row.ProgressText != null)
        {
            row.ProgressText.text = $"{progress * 100f:0}%";
        }

        if (ProfessionColors.TryGetValue(entry.Profession, out Color color))
        {
            Color textColor = new(color.r, color.g, color.b, 1f);
            if (row.NameText != null)
            {
                row.NameText.color = textColor;
            }

            if (row.ProgressFill != null)
            {
                row.ProgressFill.color = new Color(color.r, color.g, color.b, 0.9f);
            }

            if (row.ProgressBackground != null)
            {
                row.ProgressBackground.color = new Color(color.r * 0.2f, color.g * 0.2f, color.b * 0.2f, 0.6f);
            }
        }

        if (row.LevelText != null)
        {
            row.LevelText.color = Color.white;
        }

        if (row.ProgressText != null)
        {
            row.ProgressText.color = new Color(1f, 1f, 1f, 0.85f);
        }
    }

    private static string BuildProfessionSummaryText(List<ProfessionEntry> entries)
    {
        if (entries.Count == 0)
        {
            return "No profession data available.";
        }

        int totalLevel = 0;
        float totalProgress = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            totalLevel += entries[i].Level;
            totalProgress += entries[i].Progress;
        }

        float averageProgress = totalProgress / entries.Count;
        return $"Total Level: {totalLevel}   Avg Progress: {averageProgress * 100f:0}%";
    }

    private static RectTransform CreateProfessionHeaderRow(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform rectTransform = CreateRectTransformObject("ProfessionHeaderRow", parent);
        if (rectTransform == null)
        {
            return null;
        }

        UIFactory.ConfigureTopLeftAnchoring(rectTransform);

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, HeaderSpriteNames);
        background.color = HeaderBackgroundColor;
        background.raycastTarget = false;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = RowHorizontalSpacing;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.padding = CreatePadding(RowPaddingHorizontal, RowPaddingHorizontal, RowPaddingVertical, RowPaddingVertical);

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = RowHeight;
        rowLayout.minHeight = RowHeight;

        UIFactory.AddHorizontalSpacer(rectTransform, IconSize, RowHeight);
        _ = CreateHeaderLabel(rectTransform, reference, "Profession", NameWidth, TextAlignmentOptions.Left);
        _ = CreateHeaderLabel(rectTransform, reference, "Level", LevelWidth, TextAlignmentOptions.Center);
        _ = CreateHeaderLabel(rectTransform, reference, "Progress", 0f, TextAlignmentOptions.Left, 1f);
        UIFactory.AddHorizontalSpacer(rectTransform, PercentWidth, RowHeight);

        return rectTransform;
    }

    private static TextMeshProUGUI CreateHeaderLabel(
        Transform parent,
        TextMeshProUGUI reference,
        string text,
        float width,
        TextAlignmentOptions alignment,
        float flexibleWidth = 0f)
    {
        TextMeshProUGUI label = UIFactory.CreateTextElement(parent, $"Header_{text}", reference, HeaderFontScale, FontStyles.Bold);
        if (label == null)
        {
            return null;
        }

        label.text = text;
        label.alignment = alignment;

        LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.minWidth = width;
        layout.flexibleWidth = flexibleWidth;
        layout.preferredHeight = RowHeight;
        layout.minHeight = RowHeight;
        return label;
    }

    private static RectTransform CreateDividerLine(Transform parent)
    {
        RectTransform rectTransform = CreateRectTransformObject("BloodcraftDivider", parent);
        if (rectTransform == null)
        {
            return null;
        }

        UIFactory.ConfigureTopLeftAnchoring(rectTransform);

        Image image = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(image, DividerSpriteNames);
        image.color = new Color(1f, 1f, 1f, DividerOpacity);
        image.raycastTarget = false;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        float height = image.sprite != null ? 8f : 2f;
        layout.preferredHeight = height;
        layout.minHeight = height;
        return rectTransform;
    }

    private static Image CreateProfessionIcon(Transform parent)
    {
        RectTransform rectTransform = CreateRectTransformObject("ProfessionIcon", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.sizeDelta = new Vector2(IconSize, IconSize);

        Image icon = rectTransform.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = IconSize;
        layout.minWidth = IconSize;
        layout.preferredHeight = IconSize;
        layout.minHeight = IconSize;

        return icon;
    }

    private static TextMeshProUGUI CreateRowLabel(Transform parent, TextMeshProUGUI reference, float width, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI label = UIFactory.CreateTextElement(parent, "RowLabel", reference, RowFontScale, FontStyles.Normal);
        if (label == null)
        {
            return null;
        }

        label.alignment = alignment;
        label.text = string.Empty;

        LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.minWidth = width;
        layout.preferredHeight = RowHeight;

        return label;
    }

    private static Image CreateProgressBar(Transform parent, out Image background)
    {
        RectTransform rectTransform = CreateRectTransformObject("ProgressBar", parent);
        if (rectTransform == null)
        {
            background = null;
            return null;
        }

        rectTransform.sizeDelta = new Vector2(ProgressMinWidth, ProgressHeight);

        background = rectTransform.gameObject.AddComponent<Image>();
        background.sprite = ResolveProgressBackgroundSprite();
        background.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        background.type = Image.Type.Sliced;
        background.raycastTarget = false;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = ProgressMinWidth;
        layout.preferredWidth = 0f;
        layout.flexibleWidth = 1f;
        layout.preferredHeight = ProgressHeight;
        layout.minHeight = ProgressHeight;

        RectTransform fillRect = CreateRectTransformObject("Fill", rectTransform);
        if (fillRect == null)
        {
            return null;
        }

        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);

        Image fill = fillRect.gameObject.AddComponent<Image>();
        fill.sprite = ResolveProgressFillSprite();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        fill.raycastTarget = false;

        return fill;
    }

    private static Sprite ResolveProgressBackgroundSprite()
    {
        Sprite sprite = ResolveSprite(ProgressBackgroundSpriteNames);
        if (sprite != null)
        {
            return sprite;
        }

        if (_alchemyFill != null && _alchemyFill.sprite != null)
        {
            return _alchemyFill.sprite;
        }

        return _alchemyProgressFill != null ? _alchemyProgressFill.sprite : null;
    }

    private static Sprite ResolveProgressFillSprite()
    {
        Sprite sprite = ResolveSprite(ProgressFillSpriteNames);
        if (sprite != null)
        {
            return sprite;
        }

        if (_alchemyProgressFill != null && _alchemyProgressFill.sprite != null)
        {
            return _alchemyProgressFill.sprite;
        }

        return _alchemyFill != null ? _alchemyFill.sprite : null;
    }

    private static Sprite ResolveProfessionIcon(Profession profession)
    {
        if (HudData.ProfessionIcons.TryGetValue(profession, out string spriteName)
            && HudData.Sprites.TryGetValue(spriteName, out Sprite sprite)
            && sprite != null)
        {
            return sprite;
        }

        return null;
    }

    private static Sprite ResolveSprite(params string[] spriteNames)
    {
        if (spriteNames == null || spriteNames.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < spriteNames.Length; i++)
        {
            string name = spriteNames[i];
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (HudData.Sprites.TryGetValue(name, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private static void ApplySprite(Image image, params string[] spriteNames)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = ResolveSprite(spriteNames);
        if (sprite == null)
        {
            image.sprite = null;
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
    }

    private static RectOffset CreatePadding(int left, int right, int top, int bottom)
        => UIFactory.CreatePadding(left, right, top, bottom);

    private static RectTransform CreateRectTransformObject(string name, Transform parent)
        => UIFactory.CreateRectTransformObject(name, parent);

    private static void EnsureVerticalLayout(Transform root, int paddingLeft = 0, int paddingRight = 0,
        int paddingTop = 0, int paddingBottom = 0, float spacing = 6f)
    {
        if (root == null || root.Equals(null)) return;

        try
        {
            var layout = UIFactory.EnsureVerticalLayout(root, paddingLeft, paddingRight, paddingTop, paddingBottom);
            if (layout != null)
            {
                layout.spacing = spacing;
                layout.childControlHeight = true;
            }

            ContentSizeFitter fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        catch (Exception ex)
        {
            DebugToolsBridge.TryLogWarning($"[Professions Tab] Failed to configure vertical layout: {ex.Message}");
        }
    }

    private sealed class ProfessionRow
    {
        public GameObject Root { get; }
        public Image Icon { get; }
        public TextMeshProUGUI NameText { get; }
        public TextMeshProUGUI LevelText { get; }
        public Image ProgressFill { get; }
        public Image ProgressBackground { get; }
        public TextMeshProUGUI ProgressText { get; }

        public ProfessionRow(
            GameObject root,
            Image icon,
            TextMeshProUGUI nameText,
            TextMeshProUGUI levelText,
            Image progressFill,
            Image progressBackground,
            TextMeshProUGUI progressText)
        {
            Root = root;
            Icon = icon;
            NameText = nameText;
            LevelText = levelText;
            ProgressFill = progressFill;
            ProgressBackground = progressBackground;
            ProgressText = progressText;
        }
    }

    private readonly struct ProfessionEntry
    {
        public Profession Profession { get; }
        public int Level { get; }
        public float Progress { get; }

        public ProfessionEntry(Profession profession, int level, float progress)
        {
            Profession = profession;
            Level = level;
            Progress = progress;
        }
    }

    #endregion
}

