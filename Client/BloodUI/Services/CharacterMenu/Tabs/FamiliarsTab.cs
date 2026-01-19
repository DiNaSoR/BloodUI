using BloodUI.Services.CharacterMenu.Base;
using BloodUI.Services.CharacterMenu.Interfaces;
using BloodUI.Services.CharacterMenu.Shared;
using BloodUI.Services.HUD.Shared;
using BloodUI.Utilities;
using ProjectM.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static BloodUI.Services.CanvasService.DataHUD;
using static BloodUI.Services.DataService;
using BloodUI.Services;

namespace BloodUI.Services.CharacterMenu.Tabs;

/// <summary>
/// Character menu tab for managing Familiars.
/// Panel-based implementation extracted from the legacy CharacterMenuService.
/// </summary>
internal partial class FamiliarsTab : CharacterMenuTabBase, ICharacterMenuTabWithPanel
{
    private enum FamiliarsMode
    {
        Familiars,
        BattleGroups,
        Talents
    }

    private enum BoxRowActionMode
    {
        Bind,
        RemoveToOverflow
    }

    #region Constants

    private const float FamiliarSectionSpacing = 8f;
    private const float FamiliarRightColumnSpacing = 0f;
    private const float FamiliarColumnSpacing = 2f;
    private const float FamiliarCardMinHeight = 140f;
    private const float FamiliarCardInnerSpacing = 4f;
    private const int FamiliarCardPaddingHorizontal = 10;
    private const int FamiliarCardPaddingVertical = 8;
    private const float FamiliarActionRowHeight = 28f;
    private const float FamiliarActionSpacing = 4f;
    private const int FamiliarActionPaddingHorizontal = 10;
    private const int FamiliarActionPaddingVertical = 4;
    private const float FamiliarCommandWidth = 80f;
    private const float FamiliarProgressHeight = 6f;
    private const int FamiliarBoxSlotCount = 10;
    private const float FamiliarTextHeightMultiplier = 1.2f;
    private const float FamiliarColumnDividerWidth = 2f;
    private const float FamiliarColumnDividerHeightPercent = 0.8f;
    private const float FamiliarHeaderIconSize = 18f;
    private const float FamiliarActionIconSize = 16f;
    private const float FamiliarBoxIconSize = 16f;
    private const float FamiliarDropdownArrowSize = 12f;
    private const float FamiliarPortraitSize = 68f;
    private const float FamiliarPortraitOffsetX = -6f;
    private const float FamiliarPortraitOffsetY = -40f;
    private const float FamiliarNameFontScale = 0.75f;
    private const float FamiliarStatsFontScale = 0.58f;
    private const float FamiliarMetaFontScale = 0.52f;
    private const float FamiliarSectionFontScale = 0.58f;
    private const float FamiliarActionFontScale = 0.56f;
    private const float FamiliarCommandFontScale = 0.52f;
    private const float FamiliarSubHeaderFontScale = 0.52f;
    private const int FamiliarCommandPaddingHorizontal = 6;
    private const int FamiliarCommandPaddingVertical = 2;
    private const float FamiliarBoxRefreshCooldownSeconds = 8f;
    private const float FamiliarBoxSwitchDelaySeconds = 2.1f;
    private const float FamiliarModeTabsHeight = 32f;
    private const float FamiliarModeTabFontScale = 0.5f;
    private const float FamiliarConfirmWindowSeconds = 2.5f;

    #endregion

    #region Styles

    private static readonly Color FamiliarCardBackgroundColor = new(0f, 0f, 0f, 0.32f);
    private static readonly Color FamiliarActionBackgroundColor = new(0.05f, 0.05f, 0.08f, 0.75f);
    private static readonly Color FamiliarPrimaryActionBackgroundColor = new(0.5f, 0.22f, 0.12f, 0.45f);
    private static readonly Color FamiliarNameColor = new(0.95f, 0.84f, 0.7f, 1f);
    private static readonly Color FamiliarStatsColor = new(1f, 1f, 1f, 0.7f);
    private static readonly Color FamiliarMetaColor = new(1f, 1f, 1f, 0.55f);
    private static readonly Color FamiliarSectionLabelColor = new(0.9f, 0.87f, 0.83f, 1f);
    private static readonly Color FamiliarCommandTextColor = new(1f, 1f, 1f, 0.6f);
    private static readonly Color FamiliarCommandBackgroundColor = new(0f, 0f, 0f, 0.35f);
    private static readonly Color FamiliarSubHeaderTextColor = new(1f, 1f, 1f, 0.55f);
    private static readonly Color FamiliarBoxRowTextColor = new(1f, 1f, 1f, 0.75f);
    private static readonly Color FamiliarBoxRowLevelColor = new(1f, 1f, 1f, 0.5f);
    private static readonly Color FamiliarStatusTextColor = new(1f, 1f, 1f, 0.8f);
    private static readonly Color FamiliarBondFillColor = new(0.75f, 0.38f, 0.21f, 0.95f);
    private static readonly Color FamiliarBondBackgroundColor = new(0f, 0f, 0f, 0.35f);
    private static readonly Color FamiliarHeaderBackgroundColor = new(0.1f, 0.1f, 0.12f, 0.95f);
    private static readonly Color FamiliarColumnDividerColor = new(1f, 1f, 1f, 0.4f);
    private static readonly Color FamiliarToggleEnabledTextColor = new(0.35f, 0.9f, 0.4f, 1f);
    private static readonly Color FamiliarToggleDisabledTextColor = new(0.9f, 0.35f, 0.35f, 1f);
    private static readonly Color FamiliarShinyNameColorA = new(1f, 0.86f, 0.25f, 1f);
    private static readonly Color FamiliarShinyNameColorB = new(1f, 1f, 1f, 1f);
    private const float FamiliarShinyPulseSpeed = 4f;

    private static readonly Color FamiliarModeTabBackgroundColor = new(0.05f, 0.05f, 0.08f, 0.55f);
    private static readonly Color FamiliarModeTabActiveBackgroundColor = FamiliarPrimaryActionBackgroundColor;
    private static readonly Color FamiliarModeTabInactiveTextColor = new(1f, 1f, 1f, 0.7f);
    private static readonly Color FamiliarModeTabActiveTextColor = new(1f, 1f, 1f, 0.95f);
    private static readonly Color FamiliarModeTabBorderColor = new(1f, 1f, 1f, 0.15f);
    private static readonly Color FamiliarModeTabActiveBorderColor = new(1f, 1f, 1f, 0.35f);

    private static readonly string[] FamiliarCardSpriteNames = ["Window_Box", "Window_Box_Background", "SimpleBox_Normal"];
    private static readonly string[] FamiliarRowSpriteNames = ["Window_Box_Background", "TabGradient", "SimpleBox_Normal"];
    private static readonly string[] FamiliarDividerSpriteNames = ["Divider_Horizontal", "Window_Divider_Horizontal_V_Red"];
    private static readonly string[] FamiliarHeaderSpriteNames = ["Act_BG", "TabGradient", "Window_Box_Background"];
    private static readonly string[] FamiliarColumnDividerSpriteNames = ["ActionSlotDivider"];
    private static readonly string[] FamiliarProgressBackgroundSpriteNames =
        ["SimpleProgressBar_Empty_Default", "SimpleProgressBar_Mask", "Attribute_TierIndicator_Fixed"];
    private static readonly string[] FamiliarProgressFillSpriteNames =
        ["SimpleProgressBar_Fill", "Attribute_TierIndicator_Fixed"];
    private static readonly string[] FamiliarCommandPillSpriteNames = ["SimpleBox_Normal", "Window_Box_Background"];
    private static readonly string[] FamiliarDropdownSpriteNames = ["Window_Box_Background", "SimpleBox_Normal"];
    private static readonly string[] FamiliarDropdownArrowSpriteNames = ["Arrow", "FoldoutButton_Arrow"];
    private static readonly string[] FamiliarHeaderActiveIconSpriteNames = ["Portrait_Small_Smoke_AlphaWolf", "Portrait_Small_Smoke_Unknown"];
    private static readonly string[] FamiliarHeaderQuickIconSpriteNames = ["ActionWheel_InnerCircle_Gradient", "ActionWheel_InnerCircle_Gradient"];
    private static readonly string[] FamiliarHeaderBoxIconSpriteNames = ["Box_InventoryExtraBagBG", "Window_Box_Background"];
    private static readonly string[] FamiliarHeaderDefaultIconSpriteNames = ["Stunlock_Icons_spellbook_blood", "IconBackground"];
    private static readonly string[] FamiliarHeaderBattleIconSpriteNames = ["MobLevel_Skull", "IconBackground"];
    private static readonly string[] FamiliarActionIconCallSpriteNames = ["Icon_TakeItems", "Icon_DepositItems"];
    private static readonly string[] FamiliarActionIconToggleSpriteNames = ["Icon_SortItems", "Icon_DropItems"];
    private static readonly string[] FamiliarActionIconUnbindSpriteNames = ["Icon_DropItems", "Icon_SortItems"];
    private static readonly string[] FamiliarActionIconSearchSpriteNames = ["Icon_TakeItems", "Icon_DepositItems"];
    private static readonly string[] FamiliarActionIconOverflowSpriteNames = ["Icon_DepositItems", "Icon_TakeItems"];
    private static readonly string[] FamiliarActionIconEmoteSpriteNames = ["Icon_SortItems", "Icon_DropItems"];
    private static readonly string[] FamiliarActionIconShowSpriteNames = ["Icon_TakeItems", "Icon_DepositItems"];
    private static readonly string[] FamiliarActionIconLevelSpriteNames = ["spell_level_icon", "Icon_TakeItems"];
    private static readonly string[] FamiliarActionIconPrestigeSpriteNames = ["strength_level_icon", "Icon_TakeItems"];
    private static readonly string[] FamiliarBoxIconSpriteNames = ["Portrait_Small_Smoke_Unknown", "Portrait_Small_Smoke_AlphaWolf"];
    private static readonly string[] FamiliarBoxIconActiveSpriteNames = ["Portrait_Small_Smoke_AlphaWolf", "Portrait_Small_Smoke_Unknown"];
    private static readonly string[] FamiliarActivePortraitSpriteNames = ["Portrait_Small_Smoke_AlphaWolf", "Portrait_Small_Smoke_Unknown"];
    private static readonly string[] FamiliarInactivePortraitSpriteNames = ["Portrait_Small_Smoke_Unknown", "Portrait_Small_Smoke_AlphaWolf"];
    private static readonly string[] FamiliarBoxFallbackNames = ["Box 1 - Forest", "Box 2 - Highlands", "Box 3 - Crypt", "Box 4 - Swamp"];

    #endregion

    #region Fields

    private Transform _panelRoot;
    private Transform _contentRoot;
    private RectTransform _manageRoot;
    private RectTransform _battlesRoot;
    private TextMeshProUGUI _statusText;
    private TextMeshProUGUI _referenceText;

    private ModeTab _familiarsModeTab;
    private ModeTab _battleGroupsModeTab;
    private ModeTab _talentsModeTab;

    // Talents UI
    private RectTransform _talentsRoot;
    private TextMeshProUGUI _talentPointsText;
    private Transform _talentTreeRoot;
    private readonly List<TalentNodeUI> _talentNodes = [];

    private TextMeshProUGUI _activeNameText;
    private TextMeshProUGUI _activeStatsText;
    private TextMeshProUGUI _activeMetaText;
    private Image _bondFill;
    private Image _activePortrait;
    private TextMeshProUGUI _toggleCombatModeLabel;
    private TextMeshProUGUI _toggleEmoteActionsLabel;
    private TextMeshProUGUI _toggleShinyLabel;
    private TextMeshProUGUI _toggleVBloodEmotesLabel;

    private TextMeshProUGUI _boxSelectedText;
    private Transform _boxDropdownListRoot;
    private Transform _boxListRoot;
    private readonly List<FamiliarBoxOptionRow> _boxOptionRows = [];
    private readonly List<FamiliarBoxRow> _boxRows = [];

    private TextMeshProUGUI _destinationBoxSelectedText;
    private RectTransform _destinationBoxDropdownListRoot;
    private readonly List<FamiliarBoxOptionRow> _destinationBoxOptionRows = [];
    private string _destinationBoxName = string.Empty;

    private TextMeshProUGUI _deleteActiveBoxLabel;
    private bool _deleteActiveBoxConfirmArmed;
    private float _deleteActiveBoxConfirmUntil;

    private BoxRowActionMode _boxRowActionMode = BoxRowActionMode.Bind;
    private ToggleTab _boxRowBindTab;
    private ToggleTab _boxRowRemoveTab;

    private float _boxLastRefreshTime = -1000f;
    private Coroutine _boxSummonRoutine;
    private int _boxPendingSummonSlotIndex = -1;
    private string _lastFamiliarName = string.Empty;
    private int _lastFamiliarLevel = -1;
    private int _lastFamiliarPrestige = -1;
    private FamiliarsMode _mode = FamiliarsMode.Familiars;

    #endregion

    #region Properties

    public override string TabId => "Familiars";
    public override string TabLabel => "Familiars";
    public override string SectionTitle => "Familiar Management";
    public override BloodcraftTab TabType => BloodcraftTab.Familiars;

    #endregion

    #region ICharacterMenuTabWithPanel

    public Transform CreatePanel(Transform parent, TextMeshProUGUI reference)
    {
        Reset();

        _referenceText = reference;
        RectTransform rectTransform = CreateRectTransformObject("BloodcraftFamiliars", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        EnsureVerticalLayout(rectTransform, spacing: FamiliarSectionSpacing);

        _statusText = UIFactory.CreateSectionSubHeader(rectTransform, reference, string.Empty);
        if (_statusText != null)
        {
            _statusText.alignment = TextAlignmentOptions.Left;
            _statusText.color = FamiliarStatusTextColor;
        }

        _contentRoot = CreateFamiliarsContentRoot(rectTransform, reference);
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

        if (!_familiarSystemEnabled)
        {
            if (_statusText != null)
            {
                _statusText.text = "Familiars are disabled on this server.";
                _statusText.gameObject.SetActive(true);
            }

            _contentRoot.gameObject.SetActive(false);
            return;
        }

        if (_statusText != null)
        {
            _statusText.text = string.Empty;
            _statusText.gameObject.SetActive(false);
        }

        _contentRoot.gameObject.SetActive(true);
        UpdateModeTabVisuals();

        bool showFamiliars = _mode == FamiliarsMode.Familiars;
        bool showBattles = _mode == FamiliarsMode.BattleGroups;
        bool showTalents = _mode == FamiliarsMode.Talents;

        if (_manageRoot != null)
        {
            _manageRoot.gameObject.SetActive(showFamiliars);
        }
        if (_battlesRoot != null)
        {
            _battlesRoot.gameObject.SetActive(showBattles);
        }
        if (_talentsRoot != null)
        {
            _talentsRoot.gameObject.SetActive(showTalents);
        }

        if (showFamiliars)
        {
            string displayName = ResolveFamiliarDisplayName();
            bool hasFamiliar = !displayName.Equals("None", StringComparison.OrdinalIgnoreCase);
            string prestigeText = _familiarPrestige > 0 ? $" [P{_familiarPrestige}]" : string.Empty;

            if (_activeNameText != null)
            {
                _activeNameText.text = hasFamiliar
                    ? $"{displayName} Lv.{_familiarLevel}{prestigeText}"
                    : "No familiar bound";
                _activeNameText.fontStyle = hasFamiliar ? FontStyles.Bold : FontStyles.Italic;
            }

            if (_activePortrait != null)
            {
                ApplySprite(_activePortrait, hasFamiliar ? FamiliarActivePortraitSpriteNames : FamiliarInactivePortraitSpriteNames);
                _activePortrait.type = Image.Type.Simple;
                _activePortrait.preserveAspect = true;
                _activePortrait.color = hasFamiliar ? new Color(1f, 1f, 1f, 0.45f) : new Color(1f, 1f, 1f, 0.25f);
            }

            string statsLine = BuildFamiliarStatsLine();
            if (_activeStatsText != null)
            {
                _activeStatsText.text = statsLine;
                _activeStatsText.gameObject.SetActive(!string.IsNullOrWhiteSpace(statsLine));
            }

            if (_activeMetaText != null)
            {
                string progressLabel = hasFamiliar ? BuildFamiliarProgressLabel() : "Progress: --";
                string maxLabel = hasFamiliar && _familiarMaxLevel > 0 ? $"Max: {_familiarMaxLevel}" : "Max: --";
                _activeMetaText.text = $"{progressLabel} | {maxLabel}";
            }

            if (_bondFill != null)
            {
                float progress = hasFamiliar ? Mathf.Clamp01(_familiarProgress) : 0f;
                bool isMaxLevel = _familiarMaxLevel > 0 && _familiarLevel >= _familiarMaxLevel;
                _bondFill.fillAmount = isMaxLevel ? 1f : progress;
            }

            UpdateToggleIndicators();
            UpdateFamiliarBoxPanel(hasFamiliar, displayName);
            UpdateSettingsConfirmations();
        }
        else if (showBattles)
        {
            UpdateFamiliarBattlesPanel();
        }
        else if (showTalents)
        {
            UpdateFamiliarTalentsPanel();
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

        ClearPendingFamiliarBoxSwitch();

        _panelRoot = null;
        _contentRoot = null;
        _manageRoot = null;
        _battlesRoot = null;
        _statusText = null;
        _referenceText = null;
        _familiarsModeTab = null;
        _battleGroupsModeTab = null;
        _activeNameText = null;
        _activeStatsText = null;
        _activeMetaText = null;
        _bondFill = null;
        _activePortrait = null;
        _toggleCombatModeLabel = null;
        _toggleEmoteActionsLabel = null;
        _toggleShinyLabel = null;
        _toggleVBloodEmotesLabel = null;
        _resetFamiliarLabel = null;
        _resetFamiliarConfirmArmed = false;
        _resetFamiliarConfirmUntil = 0f;
        _boxSelectedText = null;
        _boxDropdownListRoot = null;
        _boxListRoot = null;
        _boxOptionRows.Clear();
        _boxRows.Clear();
        _destinationBoxSelectedText = null;
        _destinationBoxDropdownListRoot = null;
        _destinationBoxOptionRows.Clear();
        _destinationBoxName = string.Empty;
        _deleteActiveBoxLabel = null;
        _deleteActiveBoxConfirmArmed = false;
        _deleteActiveBoxConfirmUntil = 0f;
        _boxRowActionMode = BoxRowActionMode.Bind;
        _boxRowBindTab = null;
        _boxRowRemoveTab = null;
        _overflowListRoot = null;
        _overflowRows.Clear();
        _selectedOverflowIndex = -1;
        _overflowLastRefreshTime = -1000f;
        _boxLastRefreshTime = -1000f;
        _boxSummonRoutine = null;
        _boxPendingSummonSlotIndex = -1;
        _lastFamiliarName = string.Empty;
        _lastFamiliarLevel = -1;
        _lastFamiliarPrestige = -1;
        _mode = FamiliarsMode.Familiars;

        // Reset talent state
        _talentsRoot = null;
        _talentPointsText = null;
        _talentTreeRoot = null;
        _talentNodes.Clear();
        _cachedAllocatedTalents.Clear();
        _talentsSyncedFromServer = false;
        _lastTalentRequestTime = -1000f;
        _infoTitleText = null;
        _infoDescText = null;
        _infoEffectText = null;
    }

    #endregion

    #region Panel Construction

    private void CreateModeTabs(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform rectTransform = CreateRectTransformObject("FamiliarsModeTabs", parent);
        if (rectTransform == null)
        {
            return;
        }
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 0f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.padding = CreatePadding(0, 0, 0, 0);

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = FamiliarModeTabsHeight;
        rowLayout.preferredHeight = FamiliarModeTabsHeight;

        _familiarsModeTab = CreateModeTab(rectTransform, reference, "Familiars", FamiliarsMode.Familiars);
        _battleGroupsModeTab = CreateModeTab(rectTransform, reference, "Battles", FamiliarsMode.BattleGroups);
        _talentsModeTab = CreateModeTab(rectTransform, reference, "Talents", FamiliarsMode.Talents);

        UpdateModeTabVisuals();
    }

    private ModeTab CreateModeTab(Transform parent, TextMeshProUGUI reference, string label, FamiliarsMode mode)
    {
        RectTransform rectTransform = CreateRectTransformObject($"FamiliarsModeTab_{mode}", parent);
        if (rectTransform == null)
        {
            return null;
        }
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, FamiliarRowSpriteNames);
        background.color = FamiliarModeTabBackgroundColor;
        background.raycastTarget = true;

        Outline outline = rectTransform.gameObject.AddComponent<Outline>();
        outline.effectColor = FamiliarModeTabBorderColor;
        outline.effectDistance = new Vector2(1f, -1f);

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.minHeight = FamiliarModeTabsHeight;
        layout.preferredHeight = FamiliarModeTabsHeight;

        SimpleStunButton button = rectTransform.gameObject.AddComponent<SimpleStunButton>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener((UnityAction)(() => SetMode(mode)));
        }

        TMP_Text tmpLabel = UIFactory.CreateSubTabLabel(rectTransform, reference, label, FamiliarModeTabFontScale);
        TextMeshProUGUI labelText = tmpLabel as TextMeshProUGUI;
        if (labelText != null)
        {
            labelText.color = FamiliarModeTabInactiveTextColor;
        }

        return new ModeTab(background, outline, labelText, button, mode);
    }

    private void SetMode(FamiliarsMode mode)
    {
        if (_mode == mode)
        {
            return;
        }

        _mode = mode;
        UpdateModeTabVisuals();
        UpdatePanel();
    }

    private void UpdateModeTabVisuals()
    {
        ConfigureModeTabVisual(_familiarsModeTab, _mode == FamiliarsMode.Familiars);
        ConfigureModeTabVisual(_battleGroupsModeTab, _mode == FamiliarsMode.BattleGroups);
        ConfigureModeTabVisual(_talentsModeTab, _mode == FamiliarsMode.Talents);
    }

    private static void ConfigureModeTabVisual(ModeTab tab, bool isActive)
    {
        if (tab == null)
        {
            return;
        }

        if (tab.Background != null)
        {
            tab.Background.color = isActive ? FamiliarModeTabActiveBackgroundColor : FamiliarModeTabBackgroundColor;
        }

        if (tab.Border != null)
        {
            tab.Border.effectColor = isActive ? FamiliarModeTabActiveBorderColor : FamiliarModeTabBorderColor;
        }

        if (tab.Label != null)
        {
            tab.Label.color = isActive ? FamiliarModeTabActiveTextColor : FamiliarModeTabInactiveTextColor;
        }
    }

    private Transform CreateFamiliarsContentRoot(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform rectTransform = CreateRectTransformObject("FamiliarContentRoot", parent);
        if (rectTransform == null)
        {
            return null;
        }
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        EnsureVerticalLayout(rectTransform, spacing: FamiliarSectionSpacing);

        CreateModeTabs(rectTransform, reference);

        _manageRoot = CreateRectTransformObject("FamiliarsManageRoot", rectTransform);
        if (_manageRoot == null)
        {
            return rectTransform;
        }
        _manageRoot.anchorMin = new Vector2(0f, 1f);
        _manageRoot.anchorMax = new Vector2(1f, 1f);
        _manageRoot.pivot = new Vector2(0f, 1f);
        _manageRoot.offsetMin = Vector2.zero;
        _manageRoot.offsetMax = Vector2.zero;
        EnsureVerticalLayout(_manageRoot, spacing: FamiliarSectionSpacing);

        _battlesRoot = CreateRectTransformObject("FamiliarsBattlesRoot", rectTransform);
        if (_battlesRoot != null)
        {
            _battlesRoot.anchorMin = new Vector2(0f, 1f);
            _battlesRoot.anchorMax = new Vector2(1f, 1f);
            _battlesRoot.pivot = new Vector2(0f, 1f);
            _battlesRoot.offsetMin = Vector2.zero;
            _battlesRoot.offsetMax = Vector2.zero;
            EnsureVerticalLayout(_battlesRoot, spacing: FamiliarSectionSpacing);
        }

        _talentsRoot = CreateRectTransformObject("FamiliarsTalentsRoot", rectTransform);
        if (_talentsRoot != null)
        {
            _talentsRoot.anchorMin = new Vector2(0f, 1f);
            _talentsRoot.anchorMax = new Vector2(1f, 1f);
            _talentsRoot.pivot = new Vector2(0f, 1f);
            _talentsRoot.offsetMin = Vector2.zero;
            _talentsRoot.offsetMax = Vector2.zero;
            EnsureVerticalLayout(_talentsRoot, spacing: FamiliarSectionSpacing);
        }

        RectTransform topRow = CreateRectTransformObject("FamiliarTopRow", _manageRoot);
        if (topRow == null)
        {
            return rectTransform;
        }
        topRow.anchorMin = new Vector2(0f, 1f);
        topRow.anchorMax = new Vector2(1f, 1f);
        topRow.pivot = new Vector2(0f, 1f);
        topRow.offsetMin = Vector2.zero;
        topRow.offsetMax = Vector2.zero;

        HorizontalLayoutGroup topLayout = topRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        topLayout.childAlignment = TextAnchor.UpperLeft;
        topLayout.spacing = FamiliarColumnSpacing;
        topLayout.childForceExpandWidth = true;
        topLayout.childForceExpandHeight = true;
        topLayout.childControlWidth = true;
        topLayout.childControlHeight = true;

        Transform leftColumn = CreateFamiliarColumn(topRow, "FamiliarLeftColumn", FamiliarSectionSpacing);
        _ = CreateFamiliarColumnDivider(topRow);
        Transform rightColumn = CreateFamiliarColumn(topRow, "FamiliarRightColumn", FamiliarRightColumnSpacing);

        CreateFamiliarActiveCard(leftColumn, reference);
        _ = CreateFamiliarDivider(leftColumn);
        CreateFamiliarSettingsCard(leftColumn, reference);
        _ = CreateFamiliarDivider(leftColumn);
        CreateFamiliarAdvancedActions(leftColumn, reference);

        CreateFamiliarBoxCard(rightColumn, reference);

        CreateFamiliarBattlesPanel(_battlesRoot, reference);
        CreateFamiliarTalentsPanel(_talentsRoot, reference);

        return rectTransform;
    }

    private static Transform CreateFamiliarColumn(Transform parent, string name, float spacing)
    {
        RectTransform rectTransform = CreateRectTransformObject(name, parent);
        if (rectTransform == null)
        {
            return null;
        }
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = spacing;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        LayoutElement layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        return rectTransform;
    }

    private void CreateFamiliarActiveCard(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform card = CreateFamiliarCard(parent, "FamiliarActiveCard", stretchHeight: false);
        if (card == null)
        {
            return;
        }

        _ = CreateFamiliarSectionLabel(card, reference, "Active Familiar", FamiliarHeaderActiveIconSpriteNames);
        _activePortrait = CreateFamiliarPortrait(card);
        _activeNameText = CreateFamiliarText(card, reference, "No familiar bound", FamiliarNameFontScale,
            FontStyles.Bold, TextAlignmentOptions.Left, FamiliarNameColor);
        _activeStatsText = CreateFamiliarText(card, reference, string.Empty, FamiliarStatsFontScale,
            FontStyles.Normal, TextAlignmentOptions.Left, FamiliarStatsColor);
        _activeMetaText = CreateFamiliarText(card, reference, string.Empty, FamiliarMetaFontScale,
            FontStyles.Normal, TextAlignmentOptions.Left, FamiliarMetaColor);

        _ = CreateFamiliarProgressBar(card, out _bondFill);
        _ = CreateFamiliarText(card, reference, "Bond Strength", FamiliarMetaFontScale,
            FontStyles.Normal, TextAlignmentOptions.Left, FamiliarMetaColor);
    }

    private static Image CreateFamiliarPortrait(Transform parent)
    {
        RectTransform rectTransform = CreateRectTransformObject("FamiliarPortrait", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.sizeDelta = new Vector2(FamiliarPortraitSize, FamiliarPortraitSize);
        rectTransform.anchoredPosition = new Vector2(FamiliarPortraitOffsetX, FamiliarPortraitOffsetY);

        Image image = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(image, FamiliarActivePortraitSpriteNames);
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = new Color(1f, 1f, 1f, 0.45f);
        image.raycastTarget = false;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;

        return image;
    }

    private void CreateFamiliarQuickActions(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform card = CreateFamiliarCard(parent, "FamiliarQuickActionsCard", stretchHeight: false, enforceMinimumHeight: false);
        if (card == null)
        {
            return;
        }

        _ = CreateFamiliarSectionLabel(card, reference, "Quick Actions", FamiliarHeaderQuickIconSpriteNames);
        Transform listRoot = CreateFamiliarActionList(card);
        if (listRoot == null)
        {
            return;
        }

        _ = CreateFamiliarActionRow(listRoot, reference, "Call / Dismiss Familiar", ".fam t", FamiliarActionIconCallSpriteNames, true);
        _toggleCombatModeLabel = CreateFamiliarActionRow(listRoot, reference, "Toggle Combat Mode", ".fam c", FamiliarActionIconToggleSpriteNames, false);
        _ = CreateFamiliarActionRow(listRoot, reference, "Unbind Familiar", ".fam ub", FamiliarActionIconUnbindSpriteNames, false);
    }

    private void CreateFamiliarBoxCard(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform card = CreateFamiliarCard(parent, "FamiliarBoxCard", stretchHeight: true);
        if (card == null)
        {
            return;
        }

        _ = CreateFamiliarSectionLabel(card, reference, "Boxes & Storage", FamiliarHeaderBoxIconSpriteNames);

        _ = CreateFamiliarSubHeaderRow(card, reference, "Destination Box");
        _ = CreateFamiliarDropdownRow(card, reference, out _destinationBoxSelectedText, ToggleDestinationBoxDropdown);
        _destinationBoxDropdownListRoot = CreateFamiliarBoxDropdownList(card);

        _ = CreateFamiliarSubHeaderRow(card, reference, "Select Box");
        _ = CreateFamiliarDropdownRow(card, reference, out _boxSelectedText, ToggleFamiliarBoxDropdown);
        _boxDropdownListRoot = CreateFamiliarBoxDropdownList(card);

        _ = CreateFamiliarSubHeaderRow(card, reference, "Current Box Click Action");
        CreateBoxRowActionModeTabs(card, reference);

        _ = CreateFamiliarSubHeaderRow(card, reference, "Current Box");
        _boxListRoot = CreateFamiliarBoxList(card);

        _ = CreateFamiliarDivider(card);

        _ = CreateFamiliarSubHeaderRow(card, reference, "Box Tools");
        Transform toolsRoot = CreateFamiliarActionList(card);
        if (toolsRoot != null)
        {
            _ = CreateFamiliarActionRow(toolsRoot, reference, "Add Box (Auto Name)", AddBoxAuto, FamiliarActionIconSearchSpriteNames, false);
            _ = CreateFamiliarActionRow(toolsRoot, reference, "Rename Active Box (Auto Name)", RenameActiveBoxAuto, FamiliarActionIconToggleSpriteNames, false);
            _deleteActiveBoxLabel = CreateFamiliarActionRow(toolsRoot, reference, "Delete Active Box", DeleteActiveBoxMaybeConfirm, FamiliarActionIconUnbindSpriteNames, false);
            _ = CreateFamiliarActionRow(toolsRoot, reference, "Move Active Familiar → Destination", MoveActiveFamiliarToDestination, FamiliarActionIconOverflowSpriteNames, false);
        }

        _ = CreateFamiliarDivider(card);
        CreateOverflowSection(card, reference);
    }

    private void CreateBoxRowActionModeTabs(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform rectTransform = CreateRectTransformObject("BoxRowActionModeTabs", parent);
        if (rectTransform == null)
        {
            return;
        }
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 0f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.padding = CreatePadding(0, 0, 0, 0);

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = FamiliarModeTabsHeight;
        rowLayout.preferredHeight = FamiliarModeTabsHeight;

        _boxRowBindTab = CreateToggleTab(rectTransform, reference, "Bind", BoxRowActionMode.Bind);
        _boxRowRemoveTab = CreateToggleTab(rectTransform, reference, "Remove", BoxRowActionMode.RemoveToOverflow);
        UpdateBoxRowActionModeTabVisuals();
    }

    private ToggleTab CreateToggleTab(Transform parent, TextMeshProUGUI reference, string label, BoxRowActionMode mode)
    {
        RectTransform rectTransform = CreateRectTransformObject($"BoxRowAction_{mode}", parent);
        if (rectTransform == null)
        {
            return null;
        }
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, FamiliarRowSpriteNames);
        background.color = FamiliarModeTabBackgroundColor;
        background.raycastTarget = true;

        Outline outline = rectTransform.gameObject.AddComponent<Outline>();
        outline.effectColor = FamiliarModeTabBorderColor;
        outline.effectDistance = new Vector2(1f, -1f);

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.minHeight = FamiliarModeTabsHeight;
        layout.preferredHeight = FamiliarModeTabsHeight;

        SimpleStunButton button = rectTransform.gameObject.AddComponent<SimpleStunButton>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener((UnityAction)(() => SetBoxRowActionMode(mode)));
        }

        TMP_Text tmpLabel = UIFactory.CreateSubTabLabel(rectTransform, reference, label, FamiliarModeTabFontScale);
        TextMeshProUGUI labelText = tmpLabel as TextMeshProUGUI;
        if (labelText != null)
        {
            labelText.color = FamiliarModeTabInactiveTextColor;
        }

        return new ToggleTab(background, outline, labelText, button, mode);
    }

    private void SetBoxRowActionMode(BoxRowActionMode mode)
    {
        if (_boxRowActionMode == mode)
        {
            return;
        }

        _boxRowActionMode = mode;
        UpdateBoxRowActionModeTabVisuals();
        UpdatePanel();
    }

    private void UpdateBoxRowActionModeTabVisuals()
    {
        ConfigureToggleTabVisual(_boxRowBindTab, _boxRowActionMode == BoxRowActionMode.Bind);
        ConfigureToggleTabVisual(_boxRowRemoveTab, _boxRowActionMode == BoxRowActionMode.RemoveToOverflow);
    }

    private static void ConfigureToggleTabVisual(ToggleTab tab, bool isActive)
    {
        if (tab == null)
        {
            return;
        }

        if (tab.Background != null)
        {
            tab.Background.color = isActive ? FamiliarModeTabActiveBackgroundColor : FamiliarModeTabBackgroundColor;
        }

        if (tab.Border != null)
        {
            tab.Border.effectColor = isActive ? FamiliarModeTabActiveBorderColor : FamiliarModeTabBorderColor;
        }

        if (tab.Label != null)
        {
            tab.Label.color = isActive ? FamiliarModeTabActiveTextColor : FamiliarModeTabInactiveTextColor;
        }
    }

    private void CreateFamiliarAdvancedActions(Transform parent, TextMeshProUGUI reference)
    {
        _ = CreateFamiliarSectionLabel(parent, reference, "Quick Actions", FamiliarHeaderQuickIconSpriteNames);
        Transform listRoot = CreateFamiliarActionList(parent);
        if (listRoot == null)
        {
            return;
        }

        _ = CreateFamiliarActionRow(listRoot, reference, "Call / Dismiss Familiar", ".fam t", FamiliarActionIconCallSpriteNames, true);
        _toggleCombatModeLabel = CreateFamiliarActionRow(listRoot, reference, "Toggle Combat Mode", ".fam c", FamiliarActionIconToggleSpriteNames, false);
        _ = CreateFamiliarActionRow(listRoot, reference, "Unbind Familiar", ".fam ub", FamiliarActionIconUnbindSpriteNames, false);
        _ = CreateFamiliarActionRow(listRoot, reference, "View Overflow", ".fam of", FamiliarActionIconOverflowSpriteNames, false);
        _toggleEmoteActionsLabel = CreateFamiliarActionRow(listRoot, reference, "Toggle Emote Actions", ".fam e", FamiliarActionIconEmoteSpriteNames, false);
        _ = CreateFamiliarActionRow(listRoot, reference, "Show Emote Actions", ".fam actions", FamiliarActionIconShowSpriteNames, false);
        _ = CreateFamiliarActionRow(listRoot, reference, "Get Familiar Level", ".fam gl", FamiliarActionIconLevelSpriteNames, false);
        _ = CreateFamiliarActionRow(listRoot, reference, "Prestige Familiar", ".fam pr", FamiliarActionIconPrestigeSpriteNames, false);
    }

    private static RectTransform CreateFamiliarCard(Transform parent, string name, bool stretchHeight, bool enforceMinimumHeight = true)
    {
        RectTransform rectTransform = CreateRectTransformObject(name, parent);
        if (rectTransform == null)
        {
            return null;
        }
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, FamiliarCardSpriteNames);
        background.color = FamiliarCardBackgroundColor;
        background.raycastTarget = false;

        VerticalLayoutGroup layout = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = FamiliarCardInnerSpacing;
        layout.padding = CreatePadding(FamiliarCardPaddingHorizontal, FamiliarCardPaddingHorizontal,
            FamiliarCardPaddingVertical, FamiliarCardPaddingVertical);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        LayoutElement layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        layoutElement.flexibleHeight = stretchHeight ? 1f : 0f;

        if (enforceMinimumHeight)
        {
            layoutElement.minHeight = FamiliarCardMinHeight;
            layoutElement.preferredHeight = FamiliarCardMinHeight;
        }

        return rectTransform;
    }

    private static TextMeshProUGUI CreateFamiliarSectionLabel(Transform parent, TextMeshProUGUI reference, string text, string[] iconSpriteNames)
    {
        RectTransform rectTransform = CreateRectTransformObject($"{text}Header", parent);
        if (rectTransform == null)
        {
            return null;
        }
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, FamiliarHeaderSpriteNames);
        background.color = FamiliarHeaderBackgroundColor;
        background.raycastTarget = false;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 6f;
        layout.padding = CreatePadding(8, 8, 4, 4);
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        LayoutElement headerLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        float labelHeight = reference != null ? reference.fontSize * FamiliarSectionFontScale : 16f;
        float headerHeight = (labelHeight * FamiliarTextHeightMultiplier) + 8f;
        float minHeaderHeight = FamiliarHeaderIconSize + 8f;
        float resolvedHeaderHeight = Mathf.Max(headerHeight, minHeaderHeight);
        headerLayout.preferredHeight = resolvedHeaderHeight;
        headerLayout.minHeight = resolvedHeaderHeight;

        string[] resolvedIconSprites = iconSpriteNames ?? FamiliarHeaderDefaultIconSpriteNames;
        RectTransform iconRect = CreateRectTransformObject($"{text}Icon", rectTransform);
        if (iconRect != null)
        {
            Sprite iconSprite = ResolveSprite(resolvedIconSprites);
            if (iconSprite != null)
            {
                iconRect.sizeDelta = new Vector2(FamiliarHeaderIconSize, FamiliarHeaderIconSize);

                Image iconImage = iconRect.gameObject.AddComponent<Image>();
                iconImage.sprite = iconSprite;
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true;
                iconImage.color = new Color(1f, 1f, 1f, 0.9f);
                iconImage.raycastTarget = false;

                LayoutElement iconLayout = iconRect.gameObject.AddComponent<LayoutElement>();
                iconLayout.preferredWidth = FamiliarHeaderIconSize;
                iconLayout.minWidth = FamiliarHeaderIconSize;
                iconLayout.preferredHeight = FamiliarHeaderIconSize;
                iconLayout.minHeight = FamiliarHeaderIconSize;
        }
        else
        {
                iconRect.gameObject.SetActive(false);
            }
        }

        TextMeshProUGUI label = CreateFamiliarText(rectTransform, reference, text.ToUpperInvariant(),
            FamiliarSectionFontScale, FontStyles.Bold, TextAlignmentOptions.Left, FamiliarSectionLabelColor);
        if (label != null)
        {
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement labelLayout = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
        }

        return label;
    }

    private static Transform CreateFamiliarActionList(Transform parent)
    {
        RectTransform rectTransform = CreateRectTransformObject("FamiliarActionList", parent);
        if (rectTransform == null)
        {
            return null;
        }
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = FamiliarActionSpacing;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        ContentSizeFitter fitter = rectTransform.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        return rectTransform;
    }

    private static TextMeshProUGUI CreateFamiliarActionRow(
        Transform parent,
        TextMeshProUGUI reference,
        string label,
        string command,
        string[] iconSpriteNames,
        bool isPrimary)
    {
        RectTransform rectTransform = CreateRectTransformObject("FamiliarActionRow", parent);
        if (rectTransform == null)
        {
            return null;
        }
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, FamiliarRowSpriteNames);
        background.color = isPrimary ? FamiliarPrimaryActionBackgroundColor : FamiliarActionBackgroundColor;
        background.raycastTarget = true;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = FamiliarActionSpacing;
        layout.padding = CreatePadding(FamiliarActionPaddingHorizontal, FamiliarActionPaddingHorizontal,
            FamiliarActionPaddingVertical, FamiliarActionPaddingVertical);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        float rowHeight = FamiliarActionRowHeight;

        Image iconImage = CreateFamiliarIcon(rectTransform, FamiliarActionIconSize, iconSpriteNames, new Color(1f, 1f, 1f, 0.9f));
        if (iconImage != null)
        {
            rowHeight = Mathf.Max(rowHeight, FamiliarActionIconSize + (FamiliarActionPaddingVertical * 2f));
        }

        TextMeshProUGUI labelText = CreateFamiliarText(rectTransform, reference, label,
            FamiliarActionFontScale, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);
        if (labelText != null)
        {
            labelText.enableWordWrapping = false;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement labelLayout = labelText.GetComponent<LayoutElement>() ?? labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            rowHeight = Mathf.Max(rowHeight, labelText.fontSize * FamiliarTextHeightMultiplier
                + (FamiliarActionPaddingVertical * 2f));
        }

        _ = command;

        rowLayout.minHeight = rowHeight;
        rowLayout.preferredHeight = rowHeight;

        SimpleStunButton button = rectTransform.gameObject.AddComponent<SimpleStunButton>();
        ConfigureCommandButton(button, command, true);
        return labelText;
    }

    private static TextMeshProUGUI CreateFamiliarActionRow(
        Transform parent,
        TextMeshProUGUI reference,
        string label,
        Action onClick,
        string[] iconSpriteNames,
        bool isPrimary)
    {
        RectTransform rectTransform = CreateRectTransformObject("FamiliarActionRow", parent);
        if (rectTransform == null)
        {
            return null;
        }
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, FamiliarRowSpriteNames);
        background.color = isPrimary ? FamiliarPrimaryActionBackgroundColor : FamiliarActionBackgroundColor;
        background.raycastTarget = true;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = FamiliarActionSpacing;
        layout.padding = CreatePadding(FamiliarActionPaddingHorizontal, FamiliarActionPaddingHorizontal,
            FamiliarActionPaddingVertical, FamiliarActionPaddingVertical);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        float rowHeight = FamiliarActionRowHeight;

        Image iconImage = CreateFamiliarIcon(rectTransform, FamiliarActionIconSize, iconSpriteNames, new Color(1f, 1f, 1f, 0.9f));
        if (iconImage != null)
        {
            rowHeight = Mathf.Max(rowHeight, FamiliarActionIconSize + (FamiliarActionPaddingVertical * 2f));
        }

        TextMeshProUGUI labelText = CreateFamiliarText(rectTransform, reference, label,
            FamiliarActionFontScale, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);
        if (labelText != null)
        {
            labelText.enableWordWrapping = false;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement labelLayout = labelText.GetComponent<LayoutElement>() ?? labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            rowHeight = Mathf.Max(rowHeight, labelText.fontSize * FamiliarTextHeightMultiplier
                + (FamiliarActionPaddingVertical * 2f));
        }

        rowLayout.minHeight = rowHeight;
        rowLayout.preferredHeight = rowHeight;

        SimpleStunButton button = rectTransform.gameObject.AddComponent<SimpleStunButton>();
        ConfigureActionButton(button, onClick, true);
        return labelText;
    }

    private static Image CreateFamiliarIcon(Transform parent, float size, string[] spriteNames, Color color)
    {
        RectTransform rectTransform = CreateRectTransformObject("FamiliarIcon", parent);
        if (rectTransform == null)
        {
            return null;
        }

        Sprite sprite = ResolveSprite(spriteNames);
        if (sprite == null)
        {
            rectTransform.gameObject.SetActive(false);
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.sizeDelta = new Vector2(size, size);

        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = color;
        image.raycastTarget = false;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = size;
        layout.minWidth = size;
        layout.preferredHeight = size;
        layout.minHeight = size;

        return image;
    }

    private static TextMeshProUGUI CreateFamiliarCommandPill(Transform parent, TextMeshProUGUI reference, string command)
    {
        RectTransform rectTransform = CreateRectTransformObject("FamiliarCommandPill", parent);
        if (rectTransform == null || reference == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, FamiliarCommandPillSpriteNames);
        background.color = FamiliarCommandBackgroundColor;
        background.raycastTarget = false;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.padding = CreatePadding(FamiliarCommandPaddingHorizontal, FamiliarCommandPaddingHorizontal,
            FamiliarCommandPaddingVertical, FamiliarCommandPaddingVertical);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        LayoutElement layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = FamiliarCommandWidth;
        layoutElement.minWidth = FamiliarCommandWidth;

        TextMeshProUGUI commandText = CreateFamiliarText(rectTransform, reference, command,
            FamiliarCommandFontScale, FontStyles.Normal, TextAlignmentOptions.Center, FamiliarCommandTextColor);
        return commandText;
    }

    private static TextMeshProUGUI CreateFamiliarSubHeaderRow(Transform parent, TextMeshProUGUI reference, string label)
    {
        RectTransform rectTransform = CreateRectTransformObject("FamiliarSubHeaderRow", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = FamiliarActionSpacing;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = FamiliarActionRowHeight;
        rowLayout.minHeight = FamiliarActionRowHeight;

        TextMeshProUGUI labelText = CreateFamiliarText(rectTransform, reference, label.ToUpperInvariant(),
            FamiliarSubHeaderFontScale, FontStyles.Normal, TextAlignmentOptions.Left, FamiliarSubHeaderTextColor);
        if (labelText != null)
        {
            LayoutElement labelLayout = labelText.GetComponent<LayoutElement>() ?? labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
        }

        return labelText;
    }

    private RectTransform CreateFamiliarDropdownRow(Transform parent, TextMeshProUGUI reference, out TextMeshProUGUI selectedText, Action onClick)
    {
        selectedText = null;
        RectTransform rectTransform = CreateRectTransformObject("FamiliarBoxSelect", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, FamiliarDropdownSpriteNames);
        background.color = FamiliarActionBackgroundColor;
        background.raycastTarget = true;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = FamiliarActionSpacing;
        layout.padding = CreatePadding(FamiliarActionPaddingHorizontal, FamiliarActionPaddingHorizontal,
            FamiliarActionPaddingVertical, FamiliarActionPaddingVertical);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = FamiliarActionRowHeight;
        rowLayout.minHeight = FamiliarActionRowHeight;

        selectedText = CreateFamiliarText(rectTransform, reference, string.Empty,
            FamiliarActionFontScale, FontStyles.Normal, TextAlignmentOptions.Left, FamiliarBoxRowTextColor);
        if (selectedText != null)
        {
            selectedText.text = FamiliarBoxFallbackNames.Length > 0 ? FamiliarBoxFallbackNames[0] : "Select a box";
            LayoutElement labelLayout = selectedText.GetComponent<LayoutElement>() ?? selectedText.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
        }

        _ = CreateFamiliarIcon(rectTransform, FamiliarDropdownArrowSize, FamiliarDropdownArrowSpriteNames,
            new Color(1f, 1f, 1f, 0.7f));

        SimpleStunButton button = rectTransform.gameObject.AddComponent<SimpleStunButton>();
        ConfigureActionButton(button, onClick, onClick != null);

        return rectTransform;
    }

    #endregion

    #region Box Dropdown + List

    private void ToggleFamiliarBoxDropdown()
    {
        if (_boxDropdownListRoot == null)
        {
            Quips.SendCommand(".fam boxes");
            return;
        }

        bool isVisible = _boxDropdownListRoot.gameObject.activeSelf;
        SetFamiliarBoxDropdownVisible(!isVisible);

        if (!isVisible)
        {
            Quips.SendCommand(".fam boxes");
        }
    }

    private void SetFamiliarBoxDropdownVisible(bool visible)
    {
        if (_boxDropdownListRoot == null)
        {
            return;
        }

        _boxDropdownListRoot.gameObject.SetActive(visible);
        if (visible)
        {
            UpdateFamiliarBoxDropdownOptions();
        }
    }

    private void UpdateFamiliarBoxDropdownOptions()
    {
        if (_boxDropdownListRoot == null)
        {
            return;
        }

        if (_familiarBoxNames.Count == 0)
        {
            EnsureFamiliarBoxOptionRows(1);
            FamiliarBoxOptionRow placeholder = _boxOptionRows[0];
            if (placeholder.NameText != null)
            {
                placeholder.NameText.text = "No boxes loaded";
                placeholder.NameText.alpha = 0.65f;
            }
            if (placeholder.Background != null)
            {
                placeholder.Background.color = FamiliarActionBackgroundColor;
                placeholder.Background.raycastTarget = false;
            }
            if (placeholder.Button != null)
            {
                ConfigureActionButton(placeholder.Button, null, false);
                placeholder.LastBoxName = string.Empty;
                placeholder.LastIsSelected = false;
                placeholder.LastButtonEnabled = false;
            }
            return;
        }

        EnsureFamiliarBoxOptionRows(_familiarBoxNames.Count);

        int rowCount = Math.Min(_familiarBoxNames.Count, _boxOptionRows.Count);
        for (int i = 0; i < rowCount; i++)
        {
            FamiliarBoxOptionRow row = _boxOptionRows[i];
            string boxName = _familiarBoxNames[i];
            bool isSelected = !string.IsNullOrWhiteSpace(_familiarActiveBox)
                && boxName.Equals(_familiarActiveBox, StringComparison.OrdinalIgnoreCase);
            bool enabled = !string.IsNullOrWhiteSpace(boxName);

            if (row.NameText != null)
            {
                row.NameText.text = boxName;
                row.NameText.alpha = enabled ? 1f : 0.65f;
            }

            if (row.Background != null)
            {
                row.Background.color = isSelected ? FamiliarPrimaryActionBackgroundColor : FamiliarActionBackgroundColor;
                row.Background.raycastTarget = enabled;
            }

            if (row.Button != null
                && (!string.Equals(row.LastBoxName, boxName, StringComparison.Ordinal)
                    || row.LastIsSelected != isSelected
                    || row.LastButtonEnabled != enabled))
            {
                ConfigureActionButton(row.Button, enabled ? () => SelectFamiliarBox(boxName) : null, enabled);
                row.LastBoxName = boxName;
                row.LastIsSelected = isSelected;
                row.LastButtonEnabled = enabled;
            }
        }
    }

    private void EnsureFamiliarBoxOptionRows(int count)
    {
        if (_boxDropdownListRoot == null)
        {
            return;
        }

        TextMeshProUGUI reference = _referenceText;
        if (reference == null)
        {
            return;
        }

        while (_boxOptionRows.Count < count)
        {
            FamiliarBoxOptionRow row = CreateFamiliarBoxOptionRow(_boxDropdownListRoot, reference);
            if (row?.Root == null)
            {
                break;
            }

            _boxOptionRows.Add(row);
        }

        for (int i = 0; i < _boxOptionRows.Count; i++)
        {
            bool isActive = i < count;
            _boxOptionRows[i].Root.SetActive(isActive);
        }
    }

    private FamiliarBoxOptionRow CreateFamiliarBoxOptionRow(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform rectTransform = CreateRectTransformObject($"FamiliarBoxOption_{_boxOptionRows.Count + 1}", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, FamiliarDropdownSpriteNames);
        background.color = FamiliarActionBackgroundColor;
        background.raycastTarget = true;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = FamiliarActionSpacing;
        layout.padding = CreatePadding(FamiliarActionPaddingHorizontal, FamiliarActionPaddingHorizontal,
            FamiliarActionPaddingVertical, FamiliarActionPaddingVertical);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = FamiliarActionRowHeight;
        rowLayout.minHeight = FamiliarActionRowHeight;

        TextMeshProUGUI label = CreateFamiliarText(rectTransform, reference, string.Empty,
            FamiliarActionFontScale, FontStyles.Normal, TextAlignmentOptions.Left, FamiliarBoxRowTextColor);
        if (label != null)
        {
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement labelLayout = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
        }

        SimpleStunButton button = rectTransform.gameObject.AddComponent<SimpleStunButton>();
        ConfigureActionButton(button, null, false);

        return new FamiliarBoxOptionRow(rectTransform.gameObject, background, label, button);
    }

    private static RectTransform CreateFamiliarBoxDropdownList(Transform parent)
    {
        RectTransform rectTransform = CreateRectTransformObject("FamiliarBoxDropdownList", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = FamiliarActionSpacing;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        ContentSizeFitter fitter = rectTransform.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        rectTransform.gameObject.SetActive(false);
        return rectTransform;
    }

    private void SelectFamiliarBox(string boxName)
    {
        if (string.IsNullOrWhiteSpace(boxName))
        {
            return;
        }

        SetFamiliarBoxDropdownVisible(false);

        if (boxName.Equals(_familiarActiveBox, StringComparison.OrdinalIgnoreCase))
        {
            Quips.SendCommand(".fam l");
            return;
        }

        _familiarActiveBox = boxName;
        _familiarBoxEntries.Clear();
        _boxLastRefreshTime = Time.realtimeSinceStartup;

        Quips.SendCommand($".fam cb {QuoteChatArgument(boxName)}");
        Quips.SendCommand(".fam l");
    }

    private void ToggleDestinationBoxDropdown()
    {
        if (_destinationBoxDropdownListRoot == null)
        {
            Quips.SendCommand(".fam boxes");
            return;
        }

        bool isVisible = _destinationBoxDropdownListRoot.gameObject.activeSelf;
        SetDestinationBoxDropdownVisible(!isVisible);

        if (!isVisible)
        {
            Quips.SendCommand(".fam boxes");
        }
    }

    private void SetDestinationBoxDropdownVisible(bool visible)
    {
        if (_destinationBoxDropdownListRoot == null)
        {
            return;
        }

        _destinationBoxDropdownListRoot.gameObject.SetActive(visible);
        if (visible)
        {
            UpdateDestinationBoxDropdownOptions();
        }
    }

    private void UpdateDestinationBoxDropdownOptions()
    {
        if (_destinationBoxDropdownListRoot == null)
        {
            return;
        }

        if (_familiarBoxNames.Count == 0)
        {
            EnsureDestinationBoxOptionRows(1);
            FamiliarBoxOptionRow placeholder = _destinationBoxOptionRows[0];
            if (placeholder.NameText != null)
            {
                placeholder.NameText.text = "No boxes loaded";
                placeholder.NameText.alpha = 0.65f;
            }
            if (placeholder.Background != null)
            {
                placeholder.Background.color = FamiliarActionBackgroundColor;
                placeholder.Background.raycastTarget = false;
            }
            if (placeholder.Button != null)
            {
                ConfigureActionButton(placeholder.Button, null, false);
                placeholder.LastBoxName = string.Empty;
                placeholder.LastIsSelected = false;
                placeholder.LastButtonEnabled = false;
            }
            return;
        }

        EnsureDestinationBoxOptionRows(_familiarBoxNames.Count);

        int rowCount = Math.Min(_familiarBoxNames.Count, _destinationBoxOptionRows.Count);
        for (int i = 0; i < rowCount; i++)
        {
            FamiliarBoxOptionRow row = _destinationBoxOptionRows[i];
            string boxName = _familiarBoxNames[i];
            bool isSelected = !string.IsNullOrWhiteSpace(_destinationBoxName)
                && boxName.Equals(_destinationBoxName, StringComparison.OrdinalIgnoreCase);
            bool enabled = !string.IsNullOrWhiteSpace(boxName);

            if (row.NameText != null)
            {
                row.NameText.text = boxName;
                row.NameText.alpha = enabled ? 1f : 0.65f;
            }

            if (row.Background != null)
            {
                row.Background.color = isSelected ? FamiliarPrimaryActionBackgroundColor : FamiliarActionBackgroundColor;
                row.Background.raycastTarget = enabled;
            }

            if (row.Button != null
                && (!string.Equals(row.LastBoxName, boxName, StringComparison.Ordinal)
                    || row.LastIsSelected != isSelected
                    || row.LastButtonEnabled != enabled))
            {
                ConfigureActionButton(row.Button, enabled ? () => SelectDestinationBox(boxName) : null, enabled);
                row.LastBoxName = boxName;
                row.LastIsSelected = isSelected;
                row.LastButtonEnabled = enabled;
            }
        }
    }

    private void EnsureDestinationBoxOptionRows(int count)
    {
        if (_destinationBoxDropdownListRoot == null)
        {
            return;
        }

        TextMeshProUGUI reference = _referenceText;
        if (reference == null)
        {
            return;
        }

        while (_destinationBoxOptionRows.Count < count)
        {
            FamiliarBoxOptionRow row = CreateDestinationBoxOptionRow(_destinationBoxDropdownListRoot, reference);
            if (row?.Root == null)
            {
                break;
            }

            _destinationBoxOptionRows.Add(row);
        }

        for (int i = 0; i < _destinationBoxOptionRows.Count; i++)
        {
            bool isActive = i < count;
            _destinationBoxOptionRows[i].Root.SetActive(isActive);
        }
    }

    private FamiliarBoxOptionRow CreateDestinationBoxOptionRow(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform rectTransform = CreateRectTransformObject($"DestinationBoxOption_{_destinationBoxOptionRows.Count + 1}", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, FamiliarDropdownSpriteNames);
        background.color = FamiliarActionBackgroundColor;
        background.raycastTarget = true;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = FamiliarActionSpacing;
        layout.padding = CreatePadding(FamiliarActionPaddingHorizontal, FamiliarActionPaddingHorizontal,
            FamiliarActionPaddingVertical, FamiliarActionPaddingVertical);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = FamiliarActionRowHeight;
        rowLayout.minHeight = FamiliarActionRowHeight;

        TextMeshProUGUI label = CreateFamiliarText(rectTransform, reference, string.Empty,
            FamiliarActionFontScale, FontStyles.Normal, TextAlignmentOptions.Left, FamiliarBoxRowTextColor);
        if (label != null)
        {
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement labelLayout = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
        }

        SimpleStunButton button = rectTransform.gameObject.AddComponent<SimpleStunButton>();
        ConfigureActionButton(button, null, false);

        return new FamiliarBoxOptionRow(rectTransform.gameObject, background, label, button);
    }

    private void SelectDestinationBox(string boxName)
    {
        if (string.IsNullOrWhiteSpace(boxName))
        {
            return;
        }

        _destinationBoxName = boxName;
        SetDestinationBoxDropdownVisible(false);
        UpdatePanel();
    }

    private void AddBoxAuto()
    {
        string name = GenerateAutoBoxName();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        Quips.SendCommand($".fam ab {QuoteChatArgument(name)}");
        Quips.SendCommand(".fam boxes");
    }

    private void RenameActiveBoxAuto()
    {
        if (string.IsNullOrWhiteSpace(_familiarActiveBox))
        {
            return;
        }

        string newName = GenerateAutoBoxName();
        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }

        string current = _familiarActiveBox;
        Quips.SendCommand($".fam rb {QuoteChatArgument(current)} {QuoteChatArgument(newName)}");

        _familiarActiveBox = newName;
        Quips.SendCommand(".fam boxes");
        Quips.SendCommand(".fam l");
    }

    private void DeleteActiveBoxMaybeConfirm()
    {
        if (string.IsNullOrWhiteSpace(_familiarActiveBox))
        {
            return;
        }

        float now = Time.realtimeSinceStartup;
        if (!_deleteActiveBoxConfirmArmed || now > _deleteActiveBoxConfirmUntil)
        {
            _deleteActiveBoxConfirmArmed = true;
            _deleteActiveBoxConfirmUntil = now + FamiliarConfirmWindowSeconds;

            if (_deleteActiveBoxLabel != null)
            {
                _deleteActiveBoxLabel.text = "Confirm Delete Active Box";
                _deleteActiveBoxLabel.color = FamiliarToggleDisabledTextColor;
            }

            return;
        }

        _deleteActiveBoxConfirmArmed = false;
        if (_deleteActiveBoxLabel != null)
        {
            _deleteActiveBoxLabel.text = "Delete Active Box";
            _deleteActiveBoxLabel.color = Color.white;
        }

        string boxName = _familiarActiveBox;
        Quips.SendCommand($".fam db {QuoteChatArgument(boxName)}");
        Quips.SendCommand(".fam boxes");
        Quips.SendCommand(".fam l");
    }

    private void MoveActiveFamiliarToDestination()
    {
        string destination = ResolveDestinationBoxName();
        if (string.IsNullOrWhiteSpace(destination))
        {
            return;
        }

        Quips.SendCommand($".fam mb {QuoteChatArgument(destination)}");
        Quips.SendCommand(".fam l");
    }

    private string ResolveDestinationBoxName()
    {
        if (!string.IsNullOrWhiteSpace(_destinationBoxName))
        {
            return _destinationBoxName;
        }

        if (_familiarBoxNames.Count > 0)
        {
            return _familiarBoxNames[0];
        }

        return string.Empty;
    }

    private string GenerateAutoBoxName()
    {
        HashSet<string> existing = new(_familiarBoxNames, StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i <= 999; i++)
        {
            string candidate = $"box{i.ToString(CultureInfo.InvariantCulture)}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"box{DateTime.UtcNow.ToString("HHmmss", CultureInfo.InvariantCulture)}";
    }

    private static Transform CreateFamiliarBoxList(Transform parent)
    {
        RectTransform rectTransform = CreateRectTransformObject("FamiliarBoxList", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = FamiliarActionSpacing;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        LayoutElement layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
        layoutElement.flexibleHeight = 1f;

        return rectTransform;
    }

    private FamiliarBoxRow CreateFamiliarBoxRow(Transform parent)
    {
        TextMeshProUGUI reference = _referenceText;
        if (reference == null)
        {
            return null;
        }

        RectTransform rectTransform = CreateRectTransformObject($"FamiliarBoxRow_{_boxRows.Count + 1}", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, FamiliarRowSpriteNames);
        background.color = FamiliarActionBackgroundColor;
        background.raycastTarget = true;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = FamiliarActionSpacing;
        layout.padding = CreatePadding(FamiliarActionPaddingHorizontal, FamiliarActionPaddingHorizontal,
            FamiliarActionPaddingVertical, FamiliarActionPaddingVertical);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = FamiliarActionRowHeight;
        rowLayout.minHeight = FamiliarActionRowHeight;

        Image icon = CreateFamiliarIcon(rectTransform, FamiliarBoxIconSize, FamiliarBoxIconSpriteNames, new Color(1f, 1f, 1f, 0.9f));

        TextMeshProUGUI nameText = CreateFamiliarText(rectTransform, reference, string.Empty,
            FamiliarActionFontScale, FontStyles.Normal, TextAlignmentOptions.Left, FamiliarBoxRowTextColor);
        if (nameText != null)
        {
            LayoutElement nameLayout = nameText.GetComponent<LayoutElement>() ?? nameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;
        }

        TextMeshProUGUI levelText = CreateFamiliarText(rectTransform, reference, string.Empty,
            FamiliarSubHeaderFontScale, FontStyles.Normal, TextAlignmentOptions.Right, FamiliarBoxRowLevelColor);
        if (levelText != null)
        {
            levelText.enableWordWrapping = false;
            levelText.overflowMode = TextOverflowModes.Truncate;
        }

        SimpleStunButton button = rectTransform.gameObject.AddComponent<SimpleStunButton>();
        ConfigureCommandButton(button, string.Empty, false);

        return new FamiliarBoxRow(rectTransform.gameObject, background, icon, nameText, levelText, button);
    }

    private void EnsureFamiliarBoxRows(int count)
    {
        if (_boxListRoot == null)
        {
            return;
        }

        while (_boxRows.Count < count)
        {
            FamiliarBoxRow row = CreateFamiliarBoxRow(_boxListRoot);
            if (row?.Root == null)
            {
                break;
            }

            _boxRows.Add(row);
        }

        for (int i = 0; i < _boxRows.Count; i++)
        {
            bool isActive = i < count;
            _boxRows[i].Root.SetActive(isActive);
        }
    }

    private void UpdateFamiliarBoxRows(IReadOnlyList<FamiliarBoxEntry> entries)
    {
        if (entries == null)
        {
            return;
        }

        EnsureFamiliarBoxRows(entries.Count);

        int rowCount = Math.Min(entries.Count, _boxRows.Count);
        for (int i = 0; i < rowCount; i++)
        {
            FamiliarBoxRow row = _boxRows[i];
            FamiliarBoxEntry entry = entries[i];

            if (row.NameText != null)
            {
                row.NameText.text = entry.Name;
                row.NameText.alpha = entry.IsPlaceholder ? 0.6f : 1f;

                // Shiny familiars: animated highlight on the name text
                if (!entry.IsPlaceholder && entry.IsShiny)
                {
                    float t = 0.5f + (0.5f * Mathf.Sin((Time.realtimeSinceStartup * FamiliarShinyPulseSpeed) + (entry.SlotIndex * 0.35f)));
                    row.NameText.color = Color.Lerp(FamiliarShinyNameColorA, FamiliarShinyNameColorB, t);
                }
                else
                {
                    row.NameText.color = FamiliarBoxRowTextColor;
                }
            }

            if (row.LevelText != null)
            {
                row.LevelText.text = entry.Level > 0 ? $"Lv.{entry.Level}" : string.Empty;
                row.LevelText.alpha = entry.IsPlaceholder ? 0.4f : 1f;
            }

            if (row.Icon != null)
            {
                if (entry.IsPlaceholder)
                {
                    row.Icon.enabled = false;
                }
                else
                {
                    ApplySprite(row.Icon, entry.IsActive ? FamiliarBoxIconActiveSpriteNames : FamiliarBoxIconSpriteNames);
                    row.Icon.type = Image.Type.Simple;
                    row.Icon.preserveAspect = true;
                    row.Icon.color = new Color(1f, 1f, 1f, 0.9f);
                    row.Icon.enabled = row.Icon.sprite != null;
                }
            }

            if (row.Background != null)
            {
                row.Background.color = entry.IsActive ? FamiliarPrimaryActionBackgroundColor : FamiliarActionBackgroundColor;
            }

            if (row.Button != null)
            {
                bool enabled = !entry.IsPlaceholder
                    && entry.SlotIndex >= 1
                    && entry.SlotIndex <= FamiliarBoxSlotCount
                    && !string.IsNullOrWhiteSpace(entry.Name);

                if (row.LastSlotIndex != entry.SlotIndex
                    || row.LastIsActive != entry.IsActive
                    || row.LastButtonEnabled != enabled)
                {
                    ConfigureActionButton(row.Button, CreateFamiliarBoxRowAction(entry, enabled), enabled);
                    row.LastSlotIndex = entry.SlotIndex;
                    row.LastIsActive = entry.IsActive;
                    row.LastButtonEnabled = enabled;
                }

                if (row.Background != null)
                {
                    row.Background.raycastTarget = enabled;
                }
            }
        }
    }

    private Action CreateFamiliarBoxRowAction(FamiliarBoxEntry entry, bool enabled)
    {
        if (!enabled)
        {
            return null;
        }

        int slotIndex = entry.SlotIndex;
        if (_boxRowActionMode == BoxRowActionMode.RemoveToOverflow)
        {
            return () => RemoveFamiliarFromCurrentBox(slotIndex);
        }

        if (entry.IsActive)
        {
            return () =>
            {
                ClearPendingFamiliarBoxSwitch();
                Quips.SendCommand(".fam t");
            };
        }

        return () => TriggerFamiliarBoxSlotBind(slotIndex);
    }

    private void RemoveFamiliarFromCurrentBox(int slotIndex)
    {
        if (slotIndex < 1 || slotIndex > FamiliarBoxSlotCount)
        {
            return;
        }

        ClearPendingFamiliarBoxSwitch();
        Quips.SendCommand($".fam r {slotIndex}");
        Quips.SendCommand(".fam l");
        Quips.SendCommand(".fam of");
    }

    private void TriggerFamiliarBoxSlotBind(int slotIndex)
    {
        if (slotIndex < 1 || slotIndex > FamiliarBoxSlotCount)
        {
            return;
        }

        bool hasActiveFamiliar = !ResolveFamiliarDisplayName().Equals("None", StringComparison.OrdinalIgnoreCase);
        if (!hasActiveFamiliar)
        {
            ClearPendingFamiliarBoxSwitch();
            Quips.SendCommand($".fam b {slotIndex}");
            return;
        }

        QueueFamiliarBoxSwitch(slotIndex);
    }

    private void QueueFamiliarBoxSwitch(int slotIndex)
    {
        _boxPendingSummonSlotIndex = slotIndex;
        if (_boxSummonRoutine != null)
        {
            return;
        }

        _boxSummonRoutine = Core.StartCoroutine(DelayedBindFamiliarFromBoxRoutine());
    }

    private IEnumerator DelayedBindFamiliarFromBoxRoutine()
    {
        Quips.SendCommand(".fam ub");

        yield return new WaitForSecondsRealtime(FamiliarBoxSwitchDelaySeconds);

        int slotIndex = _boxPendingSummonSlotIndex;
        _boxPendingSummonSlotIndex = -1;
        _boxSummonRoutine = null;

        if (slotIndex < 1 || slotIndex > FamiliarBoxSlotCount)
        {
            yield break;
        }

        Quips.SendCommand($".fam b {slotIndex}");
    }

    private void ClearPendingFamiliarBoxSwitch()
    {
        _boxPendingSummonSlotIndex = -1;
        if (_boxSummonRoutine == null)
        {
            return;
        }

        Core.StopCoroutine(_boxSummonRoutine);
        _boxSummonRoutine = null;
    }

    private static string QuoteChatArgument(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "\"\"";
        }

        string trimmed = value.Trim();
        string escaped = trimmed.Replace("\"", "\\\"");
        return escaped.Contains(' ') ? $"\"{escaped}\"" : escaped;
    }

    private static RectTransform CreateFamiliarProgressBar(Transform parent, out Image fill)
    {
        RectTransform rectTransform = CreateRectTransformObject("FamiliarBondBar", parent);
        if (rectTransform == null)
        {
            fill = null;
            return null;
        }
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, FamiliarProgressBackgroundSpriteNames);
        background.color = FamiliarBondBackgroundColor;
        background.type = Image.Type.Sliced;
        background.raycastTarget = false;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = FamiliarProgressHeight;
        layout.minHeight = FamiliarProgressHeight;
        layout.flexibleWidth = 1f;

        RectTransform fillRect = CreateRectTransformObject("Fill", rectTransform);
        if (fillRect == null)
        {
            fill = null;
            return rectTransform;
        }
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);

        fill = fillRect.gameObject.AddComponent<Image>();
        Sprite fillSprite = ResolveSprite(FamiliarProgressFillSpriteNames);
        if (fillSprite != null)
        {
            fill.sprite = fillSprite;
        }
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        fill.color = FamiliarBondFillColor;
        fill.raycastTarget = false;

        return rectTransform;
    }

    private static RectTransform CreateFamiliarColumnDivider(Transform parent)
    {
        RectTransform rectTransform = CreateRectTransformObject("FamiliarColumnDivider", parent);
        if (rectTransform == null)
        {
            return null;
        }
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(FamiliarColumnDividerWidth, 0f);
        rectTransform.anchoredPosition = Vector2.zero;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = FamiliarColumnDividerWidth;
        layout.minWidth = FamiliarColumnDividerWidth;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 1f;

        RectTransform imageRect = CreateRectTransformObject("FamiliarColumnDividerImage", rectTransform);
        if (imageRect == null)
        {
            return rectTransform;
        }

        float inset = (1f - FamiliarColumnDividerHeightPercent) * 0.5f;
        imageRect.anchorMin = new Vector2(0f, inset);
        imageRect.anchorMax = new Vector2(1f, 1f - inset);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        Image image = imageRect.gameObject.AddComponent<Image>();
        ApplySprite(image, FamiliarColumnDividerSpriteNames);
        image.type = Image.Type.Tiled;
        image.color = FamiliarColumnDividerColor;
        image.raycastTarget = false;

        return rectTransform;
    }

    private static RectTransform CreateFamiliarDivider(Transform parent)
    {
        RectTransform rectTransform = CreateRectTransformObject("FamiliarDivider", parent);
        if (rectTransform == null)
        {
            return null;
        }
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);

        Image image = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(image, FamiliarDividerSpriteNames);
        image.color = new Color(1f, 1f, 1f, 0.6f);
        image.raycastTarget = false;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        float height = image.sprite != null ? 8f : 2f;
        layout.preferredHeight = height;
        layout.minHeight = height;
        return rectTransform;
    }

    private static TextMeshProUGUI CreateFamiliarText(
        Transform parent,
        TextMeshProUGUI reference,
        string text,
        float fontScale,
        FontStyles style,
        TextAlignmentOptions alignment,
        Color color)
    {
        if (reference == null)
        {
            return null;
        }

        TextMeshProUGUI label = CreateTextElement(parent, "FamiliarText", reference, fontScale, style);
        if (label == null)
        {
            return null;
        }

        label.text = text;
        label.alignment = alignment;
        label.color = color;
        ApplyFamiliarTextLayout(label);
        return label;
    }

    private static void ApplyFamiliarTextLayout(TextMeshProUGUI label)
    {
        if (label == null)
        {
            return;
        }

        LayoutElement layout = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
        float height = label.fontSize * FamiliarTextHeightMultiplier;
        layout.preferredHeight = height;
        layout.minHeight = height;
    }

    #endregion

    #region Familiar Panel Updates

    private void UpdateToggleIndicators()
    {
        ApplyToggleIndicator(_toggleCombatModeLabel, "Toggle Combat Mode", _familiarCombatModeEnabled);
        ApplyToggleIndicator(_toggleEmoteActionsLabel, "Toggle Emote Actions", _familiarEmoteActionsEnabled);
        ApplyToggleIndicator(_toggleShinyLabel, "Toggle Shiny Familiars", _familiarShinyEnabled);
        ApplyToggleIndicator(_toggleVBloodEmotesLabel, "Toggle VBlood Emotes", _familiarVBloodEmotesEnabled);
    }

    private static void ApplyToggleIndicator(TextMeshProUGUI label, string baseLabel, bool? enabled)
    {
        if (label == null)
        {
            return;
        }

        if (enabled == true)
        {
            label.text = $"{baseLabel} (ON)";
            label.color = FamiliarToggleEnabledTextColor;
        }
        else if (enabled == false)
        {
            label.text = $"{baseLabel} (OFF)";
            label.color = FamiliarToggleDisabledTextColor;
        }
        else
        {
            label.text = baseLabel;
            label.color = Color.white;
        }
    }

    private void UpdateFamiliarBoxPanel(bool hasFamiliar, string displayName)
    {
        bool hasName = !string.IsNullOrWhiteSpace(displayName);
        bool levelChanged = hasFamiliar && (_familiarLevel != _lastFamiliarLevel || _familiarPrestige != _lastFamiliarPrestige);
        bool nameChanged = hasName && !displayName.Equals(_lastFamiliarName, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(_destinationBoxName) && _familiarBoxNames.Count > 0)
        {
            _destinationBoxName = _familiarBoxNames[0];
        }

        if (!string.IsNullOrWhiteSpace(_destinationBoxName) && _familiarBoxNames.Count > 0
            && !_familiarBoxNames.Any(name => name.Equals(_destinationBoxName, StringComparison.OrdinalIgnoreCase)))
        {
            _destinationBoxName = _familiarBoxNames[0];
        }

        if (_destinationBoxSelectedText != null)
        {
            string fallbackLabel = FamiliarBoxFallbackNames.Length > 0 ? FamiliarBoxFallbackNames[0] : "Select a box";
            string selectedBox = string.IsNullOrWhiteSpace(_destinationBoxName)
                ? (_familiarBoxNames.Count > 0 ? _familiarBoxNames[0] : fallbackLabel)
                : _destinationBoxName;
            _destinationBoxSelectedText.text = selectedBox;
        }

        if (_boxSelectedText != null)
        {
            string fallbackLabel = FamiliarBoxFallbackNames.Length > 0 ? FamiliarBoxFallbackNames[0] : "Select a box";
            string selectedBox = string.IsNullOrWhiteSpace(_familiarActiveBox)
                ? (_familiarBoxNames.Count > 0 ? _familiarBoxNames[0] : fallbackLabel)
                : _familiarActiveBox;
            _boxSelectedText.text = selectedBox;
        }

        UpdateBoxRowActionModeTabVisuals();

        List<FamiliarBoxEntry> entries = BuildFamiliarBoxEntries(hasFamiliar, displayName);
        UpdateFamiliarBoxRows(entries);
        UpdateFamiliarBoxDropdownOptions();
        if (_destinationBoxDropdownListRoot != null && _destinationBoxDropdownListRoot.gameObject.activeSelf)
        {
            UpdateDestinationBoxDropdownOptions();
        }
        UpdateOverflowPanel();

        if (_familiarBoxEntries.Count == 0 || _familiarBoxNames.Count == 0 || levelChanged || nameChanged)
        {
            RequestFamiliarBoxData();
        }

        // Reset talent sync state when familiar changes so we fetch fresh data
        if (nameChanged)
        {
            DataService._familiarTalentsDataReady = false;
            _talentsSyncedFromServer = false;
            _cachedAllocatedTalents.Clear();
        }

        if (hasName)
        {
            _lastFamiliarName = displayName;
        }
        if (hasFamiliar)
        {
            _lastFamiliarLevel = _familiarLevel;
            _lastFamiliarPrestige = _familiarPrestige;
        }

        if (_deleteActiveBoxConfirmArmed && Time.realtimeSinceStartup > _deleteActiveBoxConfirmUntil)
        {
            _deleteActiveBoxConfirmArmed = false;
            if (_deleteActiveBoxLabel != null)
            {
                _deleteActiveBoxLabel.text = "Delete Active Box";
                _deleteActiveBoxLabel.color = Color.white;
            }
        }
    }

    private void RequestFamiliarBoxData()
    {
        float now = Time.realtimeSinceStartup;
        if (now - _boxLastRefreshTime < FamiliarBoxRefreshCooldownSeconds)
        {
            return;
        }

        _boxLastRefreshTime = now;
        Quips.SendCommand(".fam boxes");
        Quips.SendCommand(".fam l");
    }

    private static List<FamiliarBoxEntry> BuildFamiliarBoxEntries(bool hasFamiliar, string displayName)
    {
        List<FamiliarBoxEntry> entries = new(FamiliarBoxSlotCount);
        Dictionary<int, FamiliarBoxEntryData> entriesBySlot = [];

        for (int i = 0; i < _familiarBoxEntries.Count; i++)
        {
            FamiliarBoxEntryData entry = _familiarBoxEntries[i];
            if (entry == null)
            {
                continue;
            }

            if (entry.SlotIndex < 1 || entry.SlotIndex > FamiliarBoxSlotCount)
            {
                continue;
            }

            if (!entriesBySlot.ContainsKey(entry.SlotIndex))
            {
                entriesBySlot[entry.SlotIndex] = entry;
            }
        }

        for (int slotIndex = 1; slotIndex <= FamiliarBoxSlotCount; slotIndex++)
        {
            if (entriesBySlot.TryGetValue(slotIndex, out FamiliarBoxEntryData slotEntry))
            {
                bool isActive = hasFamiliar && slotEntry.Name.Equals(displayName, StringComparison.OrdinalIgnoreCase);
                int level = isActive ? _familiarLevel : slotEntry.Level;
                entries.Add(new FamiliarBoxEntry(slotIndex, slotEntry.Name, level, isActive, false, slotEntry.IsShiny));
            }
            else
            {
                entries.Add(new FamiliarBoxEntry(slotIndex, string.Empty, 0, false, true, false));
            }
        }

        return entries;
    }

    private static string ResolveFamiliarDisplayName()
    {
        if (string.IsNullOrWhiteSpace(_familiarName))
        {
            return "None";
        }

        if (_familiarName.Equals("Familiar", StringComparison.OrdinalIgnoreCase)
            || _familiarName.Equals("Frailed", StringComparison.OrdinalIgnoreCase))
        {
            return "None";
        }

        return _familiarName;
    }

    private static string BuildFamiliarStatsLine()
    {
        if (_familiarStats == null || _familiarStats.Count < 3)
        {
            return string.Empty;
        }

        string health = string.IsNullOrWhiteSpace(_familiarStats[0]) ? string.Empty : $"HP:{_familiarStats[0]}";
        string physPower = string.IsNullOrWhiteSpace(_familiarStats[1]) ? string.Empty : $"PP:{_familiarStats[1]}";
        string spellPower = string.IsNullOrWhiteSpace(_familiarStats[2]) ? string.Empty : $"SP:{_familiarStats[2]}";

        List<string> parts = [];
        if (!string.IsNullOrEmpty(health)) parts.Add(health);
        if (!string.IsNullOrEmpty(physPower)) parts.Add(physPower);
        if (!string.IsNullOrEmpty(spellPower)) parts.Add(spellPower);

        return parts.Count > 0 ? string.Join(" | ", parts) : string.Empty;
    }

    private static string BuildFamiliarProgressLabel()
    {
        if (_familiarMaxLevel > 0 && _familiarLevel >= _familiarMaxLevel)
        {
            return "Progress: Max";
        }

        float progress = Mathf.Clamp01(_familiarProgress);
        return $"Progress: {progress * 100f:0}%";
    }

    #endregion

    #region Shared UI Helpers

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
            DebugToolsBridge.TryLogWarning($"[Bloodcraft Tab] Failed to configure vertical layout: {ex.Message}");
        }
    }

    private static RectOffset CreatePadding(int left, int right, int top, int bottom)
        => UIFactory.CreatePadding(left, right, top, bottom);

    private static RectTransform CreateRectTransformObject(string name, Transform parent)
        => UIFactory.CreateRectTransformObject(name, parent);

    private static TextMeshProUGUI CreateTextElement(Transform parent, string name, TextMeshProUGUI reference, float scale, FontStyles style)
        => UIFactory.CreateTextElement(parent, name, reference, scale, style);

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
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
    }

    private static void ConfigureCommandButton(SimpleStunButton button, string command, bool enabled)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();

        if (!enabled || string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        button.onClick.AddListener((UnityAction)(() => Quips.SendCommand(command)));
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

    #endregion

    #region Nested UI Row Types

    private sealed class FamiliarBoxOptionRow
    {
        public GameObject Root { get; }
        public Image Background { get; }
        public TextMeshProUGUI NameText { get; }
        public SimpleStunButton Button { get; }
        public string LastBoxName { get; set; } = string.Empty;
        public bool LastIsSelected { get; set; }
        public bool LastButtonEnabled { get; set; }

        public FamiliarBoxOptionRow(GameObject root, Image background, TextMeshProUGUI nameText, SimpleStunButton button)
        {
            Root = root;
            Background = background;
            NameText = nameText;
            Button = button;
        }
    }

    private sealed class ModeTab
    {
        public Image Background { get; }
        public Outline Border { get; }
        public TextMeshProUGUI Label { get; }
        public SimpleStunButton Button { get; }
        public FamiliarsMode Mode { get; }

        public ModeTab(Image background, Outline border, TextMeshProUGUI label, SimpleStunButton button, FamiliarsMode mode)
        {
            Background = background;
            Border = border;
            Label = label;
            Button = button;
            Mode = mode;
        }
    }

    private sealed class ToggleTab
    {
        public Image Background { get; }
        public Outline Border { get; }
        public TextMeshProUGUI Label { get; }
        public SimpleStunButton Button { get; }
        public BoxRowActionMode Mode { get; }

        public ToggleTab(Image background, Outline border, TextMeshProUGUI label, SimpleStunButton button, BoxRowActionMode mode)
        {
            Background = background;
            Border = border;
            Label = label;
            Button = button;
            Mode = mode;
        }
    }

    private sealed class FamiliarBoxRow
    {
        public GameObject Root { get; }
        public Image Background { get; }
        public Image Icon { get; }
        public TextMeshProUGUI NameText { get; }
        public TextMeshProUGUI LevelText { get; }
        public SimpleStunButton Button { get; }
        public int LastSlotIndex { get; set; } = -1;
        public bool LastIsActive { get; set; }
        public bool LastButtonEnabled { get; set; }

        public FamiliarBoxRow(GameObject root, Image background, Image icon, TextMeshProUGUI nameText, TextMeshProUGUI levelText, SimpleStunButton button)
        {
            Root = root;
            Background = background;
            Icon = icon;
            NameText = nameText;
            LevelText = levelText;
            Button = button;
        }
    }

    private readonly struct FamiliarBoxEntry
    {
        public int SlotIndex { get; }
        public string Name { get; }
        public int Level { get; }
        public bool IsActive { get; }
        public bool IsPlaceholder { get; }
        public bool IsShiny { get; }

        public FamiliarBoxEntry(int slotIndex, string name, int level, bool isActive, bool isPlaceholder, bool isShiny)
        {
            SlotIndex = slotIndex;
            Name = name;
            Level = level;
            IsActive = isActive;
            IsPlaceholder = isPlaceholder;
            IsShiny = isShiny;
        }
    }

    #endregion
}
