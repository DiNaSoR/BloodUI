using Eclipse.Services.CharacterMenu.Base;
using Eclipse.Services.CharacterMenu.Interfaces;
using Eclipse.Services.CharacterMenu.Shared;
using Eclipse.Services.HUD.Shared;
using Eclipse.Utilities;
using ProjectM.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static Eclipse.Services.DataService;
using Eclipse.Services;

namespace Eclipse.Services.CharacterMenu.Tabs;

/// <summary>
/// Character menu tab for displaying the Prestige leaderboard.
/// Panel-based implementation (matches the DesignMock: card header, type selector, leaderboard rows).
/// </summary>
internal class PrestigeTab : CharacterMenuTabBase, ICharacterMenuTabWithPanel
{
    #region Constants

    private const float SectionSpacing = 8f;
    private const float CardInnerSpacing = 6f;
    private const int CardPaddingHorizontal = 10;
    private const int CardPaddingVertical = 8;

    private const float HeaderIconSize = 18f;
    private const float HeaderHeight = 28f;

    private const float ToolbarSpacing = 10f;
    private const float RowHeight = 28f;
    private const float RowSpacing = 4f;
    private const int RowPaddingHorizontal = 10;
    private const int RowPaddingVertical = 4;

    private const float HintFontScale = 0.5f;
    private const float RowFontScale = 0.56f;
    private const float ValueFontScale = 0.56f;

    private const float RankWidth = 22f;
    private const float ValueWidth = 56f;

    private const float DropdownHeight = 28f;
    private const float DropdownArrowSize = 12f;
    private const float DropdownWidth = 240f;

    #endregion

    #region Styles

    private static readonly Color CardBackgroundColor = new(0f, 0f, 0f, 0.32f);
    private static readonly Color HeaderBackgroundColor = new(0.1f, 0.1f, 0.12f, 0.95f);
    private static readonly Color HeaderTextColor = new(0.9f, 0.87f, 0.83f, 1f);
    private static readonly Color HintColor = new(1f, 1f, 1f, 0.55f);

    private static readonly Color RowBackgroundColor = new(0.05f, 0.05f, 0.08f, 0.75f);
    private static readonly Color RowBorderColor = new(1f, 1f, 1f, 0.08f);
    private static readonly Color TopRowBackgroundColor = new(0.27f, 0.18f, 0.07f, 0.28f);
    private static readonly Color TopRowBorderColor = new(0.85f, 0.65f, 0.3f, 0.28f);
    private static readonly Color TopRowValueColor = new(0.95f, 0.82f, 0.48f, 1f);

    private static readonly Color RowTextColor = new(1f, 1f, 1f, 0.85f);
    private static readonly Color RowValueColor = new(0.31f, 0.82f, 0.33f, 1f);
    private static readonly Color DividerColor = new(1f, 1f, 1f, 0.18f);

    private static readonly Color DropdownBackgroundColor = RowBackgroundColor;
    private static readonly Color DropdownTextColor = new(1f, 1f, 1f, 0.85f);

    private static readonly string[] CardSpriteNames = ["Window_Box", "Window_Box_Background", "SimpleBox_Normal"];
    private static readonly string[] HeaderSpriteNames = ["Act_BG", "TabGradient", "Window_Box_Background"];
    private static readonly string[] RowSpriteNames = ["Window_Box_Background", "TabGradient", "SimpleBox_Normal"];
    private static readonly string[] DividerSpriteNames = ["Divider_Horizontal", "Window_Divider_Horizontal_V_Red"];
    private static readonly string[] HeaderIconSpriteNames =
        ["Stunlock_Icon_BannerDoubleSmall", "MobLevel_Skull", "Stunlock_Icon_NewStar"];
    private static readonly string[] DropdownArrowSpriteNames = ["Arrow", "FoldoutButton_Arrow"];

    #endregion

    #region Fields

    private int _leaderboardIndex;

    private RectTransform _panelRoot;
    private Transform _contentRoot;
    private TextMeshProUGUI _statusText;
    private TextMeshProUGUI _referenceText;

    private TextMeshProUGUI _hintText;
    private TextMeshProUGUI _typeSelectedText;
    private SimpleStunButton _typeDropdownButton;
    private RectTransform _typeDropdownListRoot;
    private readonly List<TypeOptionRow> _typeOptionRows = [];

    private Transform _listRoot;
    private readonly List<LeaderboardRow> _rows = [];

    #endregion

    #region Properties

    public override string TabId => "Prestige";
    public override string TabLabel => "Prestige";
    public override string SectionTitle => "Prestige Leaderboard";
    public override BloodcraftTab TabType => BloodcraftTab.Prestige;

    #endregion

    #region ICharacterMenuTabWithPanel

    public Transform CreatePanel(Transform parent, TextMeshProUGUI reference)
    {
        Reset();
        _referenceText = reference;

        RectTransform rectTransform = CreateRectTransformObject("BloodcraftPrestige", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        EnsureVerticalLayout(rectTransform, spacing: SectionSpacing);

        _statusText = UIFactory.CreateSectionSubHeader(rectTransform, reference, string.Empty);
        if (_statusText != null)
        {
            _statusText.alignment = TextAlignmentOptions.Left;
        }

        _contentRoot = CreatePrestigeContentRoot(rectTransform, reference);
        rectTransform.gameObject.SetActive(false);

        _panelRoot = rectTransform;
        return rectTransform;
    }

    public void UpdatePanel()
    {
        if (_panelRoot == null || _contentRoot == null)
        {
            return;
        }

        if (!_prestigeDataReady)
        {
            SetStatus("Awaiting prestige data...");
            _contentRoot.gameObject.SetActive(false);
            return;
        }

        if (!_prestigeSystemEnabled)
        {
            SetStatus("Prestige system disabled.");
            _contentRoot.gameObject.SetActive(false);
            return;
        }

        if (!_prestigeLeaderboardEnabled)
        {
            SetStatus("Prestige leaderboard disabled.");
            _contentRoot.gameObject.SetActive(false);
            return;
        }

        if (_prestigeLeaderboardOrder == null || _prestigeLeaderboardOrder.Count == 0)
        {
            SetStatus("No prestige data available.");
            _contentRoot.gameObject.SetActive(false);
            return;
        }

        SetStatus(string.Empty);
        _contentRoot.gameObject.SetActive(true);

        if (_leaderboardIndex >= _prestigeLeaderboardOrder.Count)
        {
            _leaderboardIndex = 0;
        }

        UpdateTypeSelector();
        UpdateLeaderboard();
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

        _leaderboardIndex = 0;
        _panelRoot = null;
        _contentRoot = null;
        _statusText = null;
        _referenceText = null;

        _hintText = null;
        _typeSelectedText = null;
        _typeDropdownButton = null;
        _typeDropdownListRoot = null;
        _typeOptionRows.Clear();

        _listRoot = null;
        _rows.Clear();
    }

    #endregion

    #region Update Logic

    private void SetStatus(string message)
    {
        if (_statusText == null)
        {
            return;
        }

        _statusText.text = message ?? string.Empty;
        _statusText.gameObject.SetActive(!string.IsNullOrWhiteSpace(_statusText.text));
    }

    private void UpdateTypeSelector()
    {
        if (_typeSelectedText == null || _prestigeLeaderboardOrder == null || _prestigeLeaderboardOrder.Count == 0)
        {
            return;
        }

        string typeKey = _prestigeLeaderboardOrder[_leaderboardIndex];
        string displayType = HudUtilities.SplitPascalCase(typeKey);
        _typeSelectedText.text = displayType;

        if (_typeDropdownButton != null)
        {
            ConfigureActionButton(_typeDropdownButton, ToggleTypeDropdown, enabled: _prestigeLeaderboardOrder.Count > 1);
        }

        if (_typeDropdownListRoot != null && _typeDropdownListRoot.gameObject.activeSelf)
        {
            UpdateTypeDropdownOptions();
        }
    }

    private void ToggleTypeDropdown()
    {
        if (_typeDropdownListRoot == null)
        {
            return;
        }

        bool isVisible = _typeDropdownListRoot.gameObject.activeSelf;
        _typeDropdownListRoot.gameObject.SetActive(!isVisible);

        if (!isVisible)
        {
            UpdateTypeDropdownOptions();
        }
    }

    private void UpdateTypeDropdownOptions()
    {
        if (_typeDropdownListRoot == null || _prestigeLeaderboardOrder == null || _referenceText == null)
        {
            return;
        }

        EnsureTypeOptionRows(_prestigeLeaderboardOrder.Count);

        for (int i = 0; i < _typeOptionRows.Count; i++)
        {
            TypeOptionRow row = _typeOptionRows[i];
            bool isActive = i < _prestigeLeaderboardOrder.Count;
            row.Root.SetActive(isActive);
            if (!isActive)
            {
                continue;
            }

            string key = _prestigeLeaderboardOrder[i];
            string display = HudUtilities.SplitPascalCase(key);
            bool isSelected = i == _leaderboardIndex;

            row.Label.text = display;
            row.Background.color = isSelected ? TopRowBackgroundColor : DropdownBackgroundColor;

            // CRITICAL: Capture loop variable in local to avoid closure bug
            int capturedIndex = i;
            ConfigureActionButton(row.Button, () => SelectTypeIndex(capturedIndex), enabled: true);
        }
    }

    private void SelectTypeIndex(int index)
    {
        if (_prestigeLeaderboardOrder == null || index < 0 || index >= _prestigeLeaderboardOrder.Count)
        {
            return;
        }

        _leaderboardIndex = index;

        if (_typeDropdownListRoot != null)
        {
            _typeDropdownListRoot.gameObject.SetActive(false);
        }

        UpdateLeaderboard();
    }

    private void UpdateLeaderboard()
    {
        if (_listRoot == null || _prestigeLeaderboardOrder == null || _prestigeLeaderboardOrder.Count == 0)
        {
            return;
        }

        string typeKey = _prestigeLeaderboardOrder[_leaderboardIndex];
        if (!_prestigeLeaderboards.TryGetValue(typeKey, out var leaderboard) || leaderboard == null)
        {
            leaderboard = [];
        }

        int count = leaderboard.Count;
        EnsureLeaderboardRows(Math.Max(1, count));

        if (count == 0)
        {
            LeaderboardRow emptyRow = _rows[0];
            emptyRow.Root.SetActive(true);
            emptyRow.Rank.text = string.Empty;
            emptyRow.Name.text = "No prestige entries yet.";
            emptyRow.Value.text = string.Empty;
            emptyRow.Background.color = RowBackgroundColor;
            emptyRow.Border.effectColor = RowBorderColor;
            emptyRow.Rank.color = RowTextColor;
            emptyRow.Name.color = new Color(1f, 1f, 1f, 0.7f);
            emptyRow.Value.color = RowValueColor;

            for (int i = 1; i < _rows.Count; i++)
            {
                _rows[i].Root.SetActive(false);
            }
            return;
        }

        for (int i = 0; i < _rows.Count; i++)
        {
            bool active = i < count;
            _rows[i].Root.SetActive(active);
            if (!active)
            {
                continue;
            }

            PrestigeLeaderboardEntry entry = leaderboard[i];
            bool isTop = i == 0;

            _rows[i].Rank.text = (i + 1).ToString(CultureInfo.InvariantCulture);
            _rows[i].Name.text = entry.Name;
            _rows[i].Value.text = entry.Value.ToString(CultureInfo.InvariantCulture);

            _rows[i].Background.color = isTop ? TopRowBackgroundColor : RowBackgroundColor;
            _rows[i].Border.effectColor = isTop ? TopRowBorderColor : RowBorderColor;
            _rows[i].Rank.color = isTop ? TopRowValueColor : RowTextColor;
            _rows[i].Name.color = isTop ? new Color(1f, 1f, 1f, 0.92f) : RowTextColor;
            _rows[i].Value.color = isTop ? TopRowValueColor : RowValueColor;
        }
    }

    private void EnsureLeaderboardRows(int count)
    {
        if (_listRoot == null || _referenceText == null)
        {
            return;
        }

        while (_rows.Count < count)
        {
            LeaderboardRow row = CreateLeaderboardRow(_listRoot, _referenceText);
            if (row == null)
            {
                break;
            }
            _rows.Add(row);
        }

        for (int i = 0; i < _rows.Count; i++)
        {
            _rows[i].Root.SetActive(i < count);
        }
    }

    #endregion

    #region UI Construction

    private Transform CreatePrestigeContentRoot(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform card = CreateCard(parent, "PrestigeCard");
        if (card == null)
        {
            return null;
        }

        _ = CreateCardHeader(card, reference, "Prestige Leaderboard");

        RectTransform toolbar = CreateToolbar(card);
        if (toolbar != null)
        {
            _hintText = CreateText(toolbar, reference, "Click type to cycle.", reference.fontSize * HintFontScale, FontStyles.Normal, HintColor);
            if (_hintText != null)
            {
                _hintText.enableWordWrapping = false;
                _hintText.overflowMode = TextOverflowModes.Ellipsis;
                LayoutElement hintLayout = _hintText.GetComponent<LayoutElement>() ?? _hintText.gameObject.AddComponent<LayoutElement>();
                hintLayout.flexibleWidth = 1f;
            }

            _ = CreateTypeDropdown(toolbar, reference, out _typeSelectedText, out _typeDropdownButton);
        }

        _typeDropdownListRoot = CreateDropdownList(card);
        if (_typeDropdownListRoot != null)
        {
            _typeDropdownListRoot.gameObject.SetActive(false);
        }

        _ = CreateDivider(card);
        _listRoot = CreateListRoot(card, "PrestigeList");

        _ = CreateText(card, reference,
            "Leaderboards show the top players for the selected prestige type. Some types may be disabled by the server.",
            reference.fontSize * HintFontScale, FontStyles.Normal, new Color(1f, 1f, 1f, 0.48f), allowWrap: true);

        return card;
    }

    private void EnsureTypeOptionRows(int count)
    {
        if (_typeDropdownListRoot == null || _referenceText == null)
        {
            return;
        }

        while (_typeOptionRows.Count < count)
        {
            TypeOptionRow row = CreateTypeOptionRow(_typeDropdownListRoot, _referenceText);
            if (row == null)
            {
                break;
            }
            _typeOptionRows.Add(row);
        }

        for (int i = 0; i < _typeOptionRows.Count; i++)
        {
            _typeOptionRows[i].Root.SetActive(i < count);
        }
    }

    private static RectTransform CreateCard(Transform parent, string name)
    {
        RectTransform rectTransform = CreateRectTransformObject(name, parent);
        if (rectTransform == null)
        {
            return null;
        }

        UIFactory.ConfigureTopLeftAnchoring(rectTransform);

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, CardSpriteNames);
        background.color = CardBackgroundColor;
        background.raycastTarget = false;

        VerticalLayoutGroup layout = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = CardInnerSpacing;
        layout.padding = CreatePadding(CardPaddingHorizontal, CardPaddingHorizontal, CardPaddingVertical, CardPaddingVertical);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = rectTransform.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rectTransform;
    }

    private static RectTransform CreateCardHeader(Transform parent, TextMeshProUGUI reference, string title)
    {
        RectTransform rectTransform = CreateRectTransformObject("PrestigeHeader", parent);
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
        layout.spacing = 8f;
        layout.padding = CreatePadding(8, 8, 4, 4);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = HeaderHeight;
        rowLayout.preferredHeight = HeaderHeight;

        Image icon = UIFactory.CreateImage(rectTransform, "HeaderIcon", new Vector2(HeaderIconSize, HeaderIconSize), new Color(1f, 1f, 1f, 0.9f));
        if (icon != null)
        {
            ApplySprite(icon, HeaderIconSpriteNames);
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            if (icon.sprite == null)
            {
                icon.gameObject.SetActive(false);
            }
        }

        float leftOffset = 8f;
        if (icon != null && icon.gameObject.activeSelf)
        {
            leftOffset += HeaderIconSize + 8f;
        }

        _ = CreateHeaderTitleOverlay(rectTransform, reference, title, leftOffset);

        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        return rectTransform;
    }

    private static RectTransform CreateToolbar(Transform parent)
    {
        RectTransform rectTransform = CreateRectTransformObject("PrestigeToolbar", parent);
        if (rectTransform == null)
        {
            return null;
        }

        UIFactory.ConfigureTopLeftAnchoring(rectTransform);

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = ToolbarSpacing;
        layout.padding = CreatePadding(0, 0, 0, 0);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        ContentSizeFitter fitter = rectTransform.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rectTransform;
    }

    private static RectTransform CreateTypeDropdown(Transform parent, TextMeshProUGUI reference, out TextMeshProUGUI selectedText, out SimpleStunButton button)
    {
        selectedText = null;
        button = null;

        RectTransform rectTransform = CreateRectTransformObject("PrestigeTypeSelect", parent);
        if (rectTransform == null)
        {
            return null;
        }

        UIFactory.ConfigureTopLeftAnchoring(rectTransform);

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, RowSpriteNames);
        background.color = DropdownBackgroundColor;
        background.raycastTarget = true;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 8f;
        layout.padding = CreatePadding(RowPaddingHorizontal, RowPaddingHorizontal, RowPaddingVertical, RowPaddingVertical);
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = DropdownHeight;
        rowLayout.preferredHeight = DropdownHeight;
        rowLayout.preferredWidth = DropdownWidth;

        selectedText = CreateText(rectTransform, reference, "Type", reference.fontSize * RowFontScale, FontStyles.Normal, DropdownTextColor);
        if (selectedText != null)
        {
            selectedText.enableWordWrapping = false;
            selectedText.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement labelLayout = selectedText.GetComponent<LayoutElement>() ?? selectedText.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
        }

        Image arrow = UIFactory.CreateImage(rectTransform, "Arrow", new Vector2(DropdownArrowSize, DropdownArrowSize), new Color(1f, 1f, 1f, 0.7f));
        if (arrow != null)
        {
            ApplySprite(arrow, DropdownArrowSpriteNames);
            arrow.type = Image.Type.Simple;
            arrow.preserveAspect = true;
            if (arrow.sprite == null)
            {
                arrow.gameObject.SetActive(false);
            }
        }

        button = rectTransform.gameObject.AddComponent<SimpleStunButton>();
        ConfigureActionButton(button, null, enabled: false);
        return rectTransform;
    }

    private static RectTransform CreateDropdownList(Transform parent)      
    {
        RectTransform rectTransform = CreateRectTransformObject("PrestigeTypeDropdownList", parent);
        if (rectTransform == null)
        {
            return null;
        }

        UIFactory.ConfigureTopLeftAnchoring(rectTransform);

        VerticalLayoutGroup layout = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = RowSpacing;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        ContentSizeFitter fitter = rectTransform.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rectTransform;
    }

    private static TypeOptionRow CreateTypeOptionRow(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform rectTransform = CreateRectTransformObject("TypeOptionRow", parent);
        if (rectTransform == null)
        {
            return null;
        }

        UIFactory.ConfigureTopLeftAnchoring(rectTransform);

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, RowSpriteNames);
        background.color = DropdownBackgroundColor;
        background.raycastTarget = true;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 8f;
        layout.padding = CreatePadding(RowPaddingHorizontal, RowPaddingHorizontal, RowPaddingVertical, RowPaddingVertical);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = DropdownHeight;
        rowLayout.preferredHeight = DropdownHeight;

        TextMeshProUGUI label = CreateText(rectTransform, reference, string.Empty, reference.fontSize * RowFontScale, FontStyles.Normal, DropdownTextColor);
        if (label != null)
        {
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement labelLayout = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
        }

        SimpleStunButton button = rectTransform.gameObject.AddComponent<SimpleStunButton>();
        ConfigureActionButton(button, null, enabled: false);

        return new TypeOptionRow(rectTransform.gameObject, background, label, button);
    }

    private static RectTransform CreateDivider(Transform parent)
    {
        RectTransform rectTransform = CreateRectTransformObject("PrestigeDivider", parent);
        if (rectTransform == null)
        {
            return null;
        }

        Image image = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(image, DividerSpriteNames);
        image.color = DividerColor;
        image.raycastTarget = false;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 6f;
        layout.preferredHeight = 6f;
        return rectTransform;
    }

    private static Transform CreateListRoot(Transform parent, string name)
    {
        RectTransform rectTransform = CreateRectTransformObject(name, parent);
        if (rectTransform == null)
        {
            return null;
        }

        VerticalLayoutGroup layout = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = RowSpacing;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        ContentSizeFitter fitter = rectTransform.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rectTransform;
    }

    private static LeaderboardRow CreateLeaderboardRow(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform rectTransform = CreateRectTransformObject("PrestigeRow", parent);
        if (rectTransform == null)
        {
            return null;
        }

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, RowSpriteNames);
        background.color = RowBackgroundColor;
        background.raycastTarget = false;

        Outline outline = rectTransform.gameObject.AddComponent<Outline>();
        outline.effectColor = RowBorderColor;
        outline.effectDistance = new Vector2(1f, -1f);

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 10f;
        layout.padding = CreatePadding(RowPaddingHorizontal, RowPaddingHorizontal, RowPaddingVertical, RowPaddingVertical);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = RowHeight;
        rowLayout.preferredHeight = RowHeight;

        TextMeshProUGUI rank = CreateText(rectTransform, reference, string.Empty, reference.fontSize * RowFontScale, FontStyles.Bold, RowTextColor);
        if (rank != null)
        {
            rank.alignment = TextAlignmentOptions.Center;
            LayoutElement rankLayout = rank.GetComponent<LayoutElement>() ?? rank.gameObject.AddComponent<LayoutElement>();
            rankLayout.preferredWidth = RankWidth;
            rankLayout.minWidth = RankWidth;
        }

        TextMeshProUGUI name = CreateText(rectTransform, reference, string.Empty, reference.fontSize * RowFontScale, FontStyles.Normal, RowTextColor);
        if (name != null)
        {
            name.enableWordWrapping = false;
            name.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement nameLayout = name.GetComponent<LayoutElement>() ?? name.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;
        }

        TextMeshProUGUI value = CreateText(rectTransform, reference, string.Empty, reference.fontSize * ValueFontScale, FontStyles.Normal, RowValueColor);
        if (value != null)
        {
            value.alignment = TextAlignmentOptions.Right;
            LayoutElement valueLayout = value.GetComponent<LayoutElement>() ?? value.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = ValueWidth;
            valueLayout.minWidth = ValueWidth;
            valueLayout.flexibleWidth = 0f;
        }

        return new LeaderboardRow(rectTransform.gameObject, background, outline, rank, name, value);
    }

    #endregion

    #region Low-level helpers

    private static TextMeshProUGUI CreateHeaderTitleOverlay(
        RectTransform parent,
        TextMeshProUGUI reference,
        string title,
        float leftOffset)
    {
        if (parent == null || reference == null)
        {
            return null;
        }

        RectTransform rectTransform = CreateRectTransformObject("HeaderTitle", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 0.5f);
        rectTransform.offsetMin = new Vector2(leftOffset, 0f);
        rectTransform.offsetMax = new Vector2(-8f, 0f);

        TextMeshProUGUI titleText = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        UIFactory.CopyTextStyle(reference, titleText);
        titleText.text = title;
        titleText.fontSize = reference.fontSize * 0.6f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = HeaderTextColor;
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.enableWordWrapping = false;
        titleText.overflowMode = TextOverflowModes.Ellipsis;
        titleText.raycastTarget = false;
        titleText.ForceMeshUpdate();

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        return titleText;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        TextMeshProUGUI reference,
        string text,
        float fontSize,
        FontStyles style,
        Color color,
        bool allowWrap = false)
    {
        if (allowWrap)
        {
            RectTransform rectTransform = CreateRectTransformObject("WrappedText", parent);
            if (rectTransform == null)
            {
                return null;
            }

            TextMeshProUGUI tmpWrapped = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
            UIFactory.CopyTextStyle(reference, tmpWrapped);
            tmpWrapped.text = text;
            tmpWrapped.fontSize = fontSize;
            tmpWrapped.fontStyle = style;
            tmpWrapped.color = color;
            tmpWrapped.alignment = TextAlignmentOptions.Left;
            tmpWrapped.enableWordWrapping = true;
            tmpWrapped.overflowMode = TextOverflowModes.Overflow;
            tmpWrapped.richText = true;
            tmpWrapped.raycastTarget = false;

            ContentSizeFitter fitter = rectTransform.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1f;

            return tmpWrapped;
        }

        TextMeshProUGUI tmp = UIFactory.CreateText(parent, reference, text, fontSize, TextAlignmentOptions.Left);
        if (tmp == null)
        {
            return null;
        }

        tmp.fontStyle = style;
        tmp.color = color;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void ConfigureActionButton(SimpleStunButton button, Action action, bool enabled)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();

        if (!enabled || action == null)
        {
            return;
        }

        button.onClick.AddListener((UnityAction)(() => action()));
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
            DebugToolsBridge.TryLogWarning($"[Prestige Tab] Failed to configure vertical layout: {ex.Message}");
        }
    }

    private static Sprite ResolveSprite(params string[] spriteNames)
    {
        if (spriteNames == null || spriteNames.Length == 0)
        {
            return null;
        }

        foreach (string name in spriteNames)
        {
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

    #endregion

    #region Nested row types

    private sealed class TypeOptionRow
    {
        public GameObject Root { get; }
        public Image Background { get; }
        public TextMeshProUGUI Label { get; }
        public SimpleStunButton Button { get; }

        public TypeOptionRow(GameObject root, Image background, TextMeshProUGUI label, SimpleStunButton button)
        {
            Root = root;
            Background = background;
            Label = label;
            Button = button;
        }
    }

    private sealed class LeaderboardRow
    {
        public GameObject Root { get; }
        public Image Background { get; }
        public Outline Border { get; }
        public TextMeshProUGUI Rank { get; }
        public TextMeshProUGUI Name { get; }
        public TextMeshProUGUI Value { get; }

        public LeaderboardRow(GameObject root, Image background, Outline border, TextMeshProUGUI rank, TextMeshProUGUI name, TextMeshProUGUI value)
        {
            Root = root;
            Background = background;
            Border = border;
            Rank = rank;
            Name = name;
            Value = value;
        }
    }

    #endregion
}
