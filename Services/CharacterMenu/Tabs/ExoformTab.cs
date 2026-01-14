using Eclipse.Services.CharacterMenu.Base;
using Eclipse.Services.CharacterMenu.Interfaces;
using Eclipse.Services.CharacterMenu.Shared;
using Eclipse.Services.HUD.Shared;
using Eclipse.Utilities;
using ProjectM.UI;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static Eclipse.Services.DataService;
using Eclipse.Services;

namespace Eclipse.Services.CharacterMenu.Tabs;

/// <summary>
/// Character menu tab for displaying and managing Exoform/Shapeshift configurations.
/// Panel-based implementation (matches the DesignMock layout intent: overview, forms, abilities, notes).
/// </summary>
internal class ExoformTab : CharacterMenuTabBase, ICharacterMenuTabWithPanel
{
    #region Constants

    private const float SectionSpacing = 8f;
    private const float ColumnSpacing = 2f;
    private const float ColumnDividerWidth = 2f;
    private const float ColumnDividerHeightPercent = 0.8f;

    private const float CardInnerSpacing = 6f;
    private const int CardPaddingHorizontal = 10;
    private const int CardPaddingVertical = 8;

    private const float HeaderIconSize = 18f;
    private const float ActionIconSize = 16f;
    private const float ActionRowHeight = 28f;
    private const float ActionRowSpacing = 6f;
    private const int ActionRowPaddingHorizontal = 10;
    private const int ActionRowPaddingVertical = 4;

    private const float TitleFontScale = 0.75f;
    private const float MetaFontScale = 0.55f;
    private const float HintFontScale = 0.5f;

    private const float ProgressHeight = 6f;
    private const float MinExoEntryDurationSeconds = 15f;

    #endregion

    #region Styles

    private static readonly Color CardBackgroundColor = new(0f, 0f, 0f, 0.32f);
    private static readonly Color RowBackgroundColor = new(0.05f, 0.05f, 0.08f, 0.75f);
    private static readonly Color PrimaryRowBackgroundColor = new(0.5f, 0.22f, 0.12f, 0.45f);
    private static readonly Color HeaderBackgroundColor = new(0.1f, 0.1f, 0.12f, 0.95f);
    private static readonly Color HeaderTextColor = new(0.9f, 0.87f, 0.83f, 1f);
    private static readonly Color ColumnDividerColor = new(1f, 1f, 1f, 0.4f);
    private static readonly Color TitleColor = new(0.95f, 0.84f, 0.7f, 1f);
    private static readonly Color MetaColor = new(1f, 1f, 1f, 0.55f);
    private static readonly Color HintColor = new(1f, 1f, 1f, 0.48f);
    private static readonly Color ToggleEnabledTextColor = new(0.35f, 0.9f, 0.4f, 1f);
    private static readonly Color ToggleDisabledTextColor = new(0.9f, 0.35f, 0.35f, 1f);

    private static readonly Color ChargeFillColor = new(0.46f, 0.31f, 0.74f, 0.95f);
    private static readonly Color ChargeBackgroundColor = new(0f, 0f, 0f, 0.35f);

    private static readonly string[] CardSpriteNames = ["Window_Box", "Window_Box_Background", "SimpleBox_Normal"];
    private static readonly string[] RowSpriteNames = ["Window_Box_Background", "TabGradient", "SimpleBox_Normal"];
    private static readonly string[] DividerSpriteNames = ["Divider_Horizontal", "Window_Divider_Horizontal_V_Red"];
    private static readonly string[] HeaderSpriteNames = ["Act_BG", "TabGradient", "Window_Box_Background"];
    private static readonly string[] ColumnDividerSpriteNames = ["ActionSlotDivider"];
    private static readonly string[] ProgressBackgroundSpriteNames =
        ["SimpleProgressBar_Empty_Default", "SimpleProgressBar_Mask", "Attribute_TierIndicator_Fixed"];
    private static readonly string[] ProgressFillSpriteNames = ["SimpleProgressBar_Fill", "ChargeRefill", "SimpleProgressBar_Mask"];

    private static readonly string[] ExoHeaderIconSpriteNames =
        ["Portrait_Small_Smoke_Dracula_Grey", "Portrait_Small_Smoke_Unknown", "MobLevel_Skull"];
    private static readonly string[] ToggleIconSpriteNames = ["Icon_SortItems", "IconBackground"];
    private static readonly string[] FormIconSpriteNames = ["FoldoutButton_Arrow", "Arrow"];
    private static readonly string[] AbilityIconSpriteNames = ["spell_icon", "IconBackground"];

    #endregion

    #region State

    private RectTransform _panelRoot;
    private Transform _contentRoot;
    private TextMeshProUGUI _statusText;
    private TextMeshProUGUI _referenceText;

    // Overview
    private TextMeshProUGUI _overviewCurrentText;
    private TextMeshProUGUI _overviewMetaText;
    private Image _chargeFill;
    private TextMeshProUGUI _chargeLabelText;
    private TextMeshProUGUI _tauntStatusText;
    private SimpleStunButton _tauntButton;

    // Forms
    private Transform _formsListRoot;
    private readonly List<FormRow> _formRows = [];
    private int _renderedFormCount;

    // Abilities
    private TextMeshProUGUI _abilitiesHeaderText;
    private Transform _abilitiesListRoot;
    private readonly List<AbilityRow> _abilityRows = [];
    private string _lastAbilitiesFormName = string.Empty;
    private int _renderedAbilityCount;

    #endregion

    #region Properties

    public override string TabId => "Exoform";
    public override string TabLabel => "Exoform";
    public override string SectionTitle => "Exoforms";
    public override BloodcraftTab TabType => BloodcraftTab.Exoform;

    #endregion

    #region ICharacterMenuTabWithPanel

    public Transform CreatePanel(Transform parent, TextMeshProUGUI reference)
    {
        Reset();
        _referenceText = reference;

        RectTransform rectTransform = CreateRectTransformObject("BloodcraftExoform", parent);
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
            Color baseColor = _statusText.color;
            _statusText.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.85f);
        }

        _contentRoot = CreateExoContentRoot(rectTransform, reference);
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

        if (!_exoFormDataReady)
        {
            SetStatus("Awaiting exoform data...");
            _contentRoot.gameObject.SetActive(false);
            return;
        }

        if (!_exoFormEnabled)
        {
            SetStatus("Exo prestiging disabled.");
            _contentRoot.gameObject.SetActive(false);
            return;
        }

        SetStatus(string.Empty);
        _contentRoot.gameObject.SetActive(true);

        UpdateOverview();
        UpdateForms();
        UpdateAbilities();
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
        _contentRoot = null;
        _statusText = null;
        _referenceText = null;

        _overviewCurrentText = null;
        _overviewMetaText = null;
        _chargeFill = null;
        _chargeLabelText = null;
        _tauntStatusText = null;
        _tauntButton = null;

        _formsListRoot = null;
        _formRows.Clear();
        _renderedFormCount = 0;

        _abilitiesHeaderText = null;
        _abilitiesListRoot = null;
        _abilityRows.Clear();
        _lastAbilitiesFormName = string.Empty;
        _renderedAbilityCount = 0;
    }

    #endregion

    #region Panel Construction

    private Transform CreateExoContentRoot(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform root = CreateRectTransformObject("ExoContentRoot", parent);
        if (root == null)
        {
            return null;
        }

        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        EnsureVerticalLayout(root, spacing: SectionSpacing);

        RectTransform columns = CreateRectTransformObject("ExoColumns", root);
        if (columns == null)
        {
            return root;
        }

        columns.anchorMin = new Vector2(0f, 1f);
        columns.anchorMax = new Vector2(1f, 1f);
        columns.pivot = new Vector2(0f, 1f);
        columns.offsetMin = Vector2.zero;
        columns.offsetMax = Vector2.zero;

        HorizontalLayoutGroup layout = columns.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = ColumnSpacing;
        layout.padding = CreatePadding(0, 0, 0, 0);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        ContentSizeFitter fitter = columns.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform left = CreateColumn(columns, "ExoLeftColumn");
        _ = CreateColumnDivider(columns);
        RectTransform right = CreateColumn(columns, "ExoRightColumn");

        CreateOverviewCard(left, reference);
        _ = CreateDivider(left);
        CreateFormsCard(left, reference);

        CreateAbilitiesCard(right, reference);
        _ = CreateDivider(right);
        CreateNotesCard(right, reference);

        return root;
    }

    private static RectTransform CreateColumn(Transform parent, string name)
    {
        RectTransform column = CreateRectTransformObject(name, parent);
        if (column == null)
        {
            return null;
        }

        column.anchorMin = new Vector2(0f, 1f);
        column.anchorMax = new Vector2(1f, 1f);
        column.pivot = new Vector2(0f, 1f);
        column.offsetMin = Vector2.zero;
        column.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = SectionSpacing;
        layout.padding = CreatePadding(0, 0, 0, 0);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        LayoutElement columnLayout = column.gameObject.AddComponent<LayoutElement>();
        columnLayout.flexibleWidth = 1f;
        columnLayout.flexibleHeight = 1f;

        return column;
    }

    private void CreateOverviewCard(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform card = CreateCard(parent, "ExoOverviewCard", stretchHeight: false);
        if (card == null)
        {
            return;
        }

        _ = CreateCardHeader(card, reference, "Exoform Overview", ExoHeaderIconSpriteNames);

        _overviewCurrentText = CreateText(card, reference, "Current Form: --", reference.fontSize * TitleFontScale, FontStyles.Bold, TitleColor);
        _overviewMetaText = CreateText(card, reference, "Exo Prestiges: -- | Minimum to enter: 15.0s", reference.fontSize * MetaFontScale, FontStyles.Normal, MetaColor);

        _ = CreateChargeBar(card, out _chargeFill);
        _chargeLabelText = CreateText(card, reference, "Stored Energy: --", reference.fontSize * MetaFontScale, FontStyles.Normal, MetaColor);

        Transform actionList = CreateListRoot(card, "ExoOverviewActions", spacing: ActionRowSpacing);
        if (actionList != null)
        {
            _tauntButton = CreateActionRowWithStatus(actionList, reference,
                label: "Taunt to Exoform",
                iconSprites: ToggleIconSpriteNames,
                statusText: out _tauntStatusText,
                isPrimary: true);
        }

        _ = CreateText(card, reference,
            "Charge refills over real time (~1 day to fully recharge). When enabled, use the Taunt emote to enter Exoform.",
            reference.fontSize * HintFontScale, FontStyles.Normal, HintColor, allowWrap: true);
    }

    private void CreateFormsCard(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform card = CreateCard(parent, "ExoFormsCard", stretchHeight: false);
        if (card == null)
        {
            return;
        }

        _ = CreateCardHeader(card, reference, "Forms", ExoHeaderIconSpriteNames);
        _formsListRoot = CreateListRoot(card, "ExoFormsList", spacing: 4f);
    }

    private void CreateAbilitiesCard(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform card = CreateCard(parent, "ExoAbilitiesCard", stretchHeight: true);
        if (card == null)
        {
            return;
        }

        _ = CreateCardHeader(card, reference, "Active Form Abilities", ExoHeaderIconSpriteNames);
        _abilitiesHeaderText = CreateText(card, reference, "Select a form to view abilities.", reference.fontSize * MetaFontScale, FontStyles.Normal, MetaColor);
        _abilitiesListRoot = CreateListRoot(card, "ExoAbilitiesList", spacing: 4f);
    }

    private void CreateNotesCard(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform card = CreateCard(parent, "ExoNotesCard", stretchHeight: false, enforceMinimumHeight: false);
        if (card == null)
        {
            return;
        }

        _ = CreateCardHeader(card, reference, "Notes", ExoHeaderIconSpriteNames);

        _ = CreateText(card, reference,
            "- You need at least 15s stored energy to enter Exoform.\n- Energy drains while in Exoform.\n- Max duration scales with Exo Prestiges.",
            reference.fontSize * HintFontScale, FontStyles.Normal, HintColor, allowWrap: true);
    }

    #endregion

    #region Panel Updates

    private void SetStatus(string message)
    {
        if (_statusText == null)
        {
            return;
        }

        _statusText.text = message ?? string.Empty;
        _statusText.gameObject.SetActive(!string.IsNullOrWhiteSpace(_statusText.text));
    }

    private void UpdateOverview()
    {
        string currentForm = string.IsNullOrWhiteSpace(_exoFormCurrentForm)
            ? "None"
            : HudUtilities.SplitPascalCase(_exoFormCurrentForm);

        if (_overviewCurrentText != null)
        {
            _overviewCurrentText.text = $"Current Form: {currentForm}";
        }

        if (_overviewMetaText != null)
        {
            _overviewMetaText.text = $"Exo Prestiges: {_exoFormPrestiges} | Minimum to enter: {MinExoEntryDurationSeconds:0.0}s";
        }

        float ratio = _exoFormMaxDuration > 0f ? Mathf.Clamp01(_exoFormCharge / _exoFormMaxDuration) : 0f;
        if (_chargeFill != null)
        {
            _chargeFill.fillAmount = ratio;
        }

        if (_chargeLabelText != null)
        {
            string maxLabel = _exoFormMaxDuration > 0f ? $"{_exoFormMaxDuration:0.0}s" : "--";
            _chargeLabelText.text = $"Stored Energy: {_exoFormCharge:0.0} / {maxLabel}";
        }

        bool hasUnlockedForm = _exoFormEntries != null && _exoFormEntries.Any(entry => entry.Unlocked);
        bool canToggleTaunt = _exoFormPrestiges > 0 && hasUnlockedForm;
        string tauntState = _exoFormTauntEnabled ? "On" : "Off";

        if (_tauntStatusText != null)
        {
            _tauntStatusText.text = tauntState;
            _tauntStatusText.color = _exoFormTauntEnabled ? ToggleEnabledTextColor : ToggleDisabledTextColor;
        }

        ConfigureCommandButton(_tauntButton, ".prestige exoform", canToggleTaunt);
    }

    private void UpdateForms()
    {
        int count = _exoFormEntries?.Count ?? 0;
        if (_formsListRoot == null)
        {
            return;
        }

        if (count != _renderedFormCount)
        {
            RebuildFormRows();
            _renderedFormCount = count;
        }

        for (int i = 0; i < _formRows.Count; i++)
        {
            FormRow row = _formRows[i];
            if (row == null || row.Label == null || row.Status == null || row.Background == null)
            {
                continue;
            }

            if (_exoFormEntries == null || i >= _exoFormEntries.Count)
            {
                row.Root.SetActive(false);
                continue;
            }

            ExoFormEntry form = _exoFormEntries[i];
            string displayName = HudUtilities.SplitPascalCase(form.FormName);
            bool isSelected = !string.IsNullOrWhiteSpace(_exoFormCurrentForm)
                              && form.FormName.Equals(_exoFormCurrentForm, StringComparison.OrdinalIgnoreCase);

            row.Root.SetActive(true);
            row.Label.text = displayName;
            row.Label.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
            row.Label.color = Color.white;

            if (form.Unlocked)
            {
                row.Status.text = isSelected ? "Selected" : "Unlocked";
                row.Status.color = isSelected ? ToggleEnabledTextColor : MetaColor;
                row.Background.color = isSelected ? PrimaryRowBackgroundColor : RowBackgroundColor;
                ConfigureCommandButton(row.Button, $".prestige sf {form.FormName}", enabled: true);
            }
            else
            {
                row.Status.text = "Locked";
                row.Status.color = ToggleDisabledTextColor;
                row.Background.color = RowBackgroundColor;
                ConfigureCommandButton(row.Button, string.Empty, enabled: false);
            }

            if (row.Note != null)
            {
                string note = ResolveFormUnlockNote(form.FormName);
                row.Note.text = note;
                row.Note.gameObject.SetActive(!string.IsNullOrWhiteSpace(note));
            }
        }
    }

    private void RebuildFormRows()
    {
        _formRows.Clear();
        _renderedFormCount = 0;

        UIFactory.ClearChildren(_formsListRoot);

        if (_exoFormEntries == null || _exoFormEntries.Count == 0)
        {
            if (_referenceText != null)
            {
                _ = CreateText(_formsListRoot, _referenceText, "No forms available.", _referenceText.fontSize * MetaFontScale, FontStyles.Italic, MetaColor);
            }
            return;
        }

        for (int i = 0; i < _exoFormEntries.Count; i++)
        {
            FormRow row = CreateFormRow(_formsListRoot, _referenceText);
            if (row != null)
            {
                _formRows.Add(row);
            }
        }
    }

    private void UpdateAbilities()
    {
        if (_abilitiesListRoot == null)
        {
            return;
        }

        ExoFormEntry activeForm = ResolveSelectedExoFormEntry();
        string activeName = activeForm?.FormName ?? string.Empty;

        if (_abilitiesHeaderText != null)
        {
            string displayName = string.IsNullOrWhiteSpace(activeName) ? "None" : HudUtilities.SplitPascalCase(activeName);
            _abilitiesHeaderText.text = string.IsNullOrWhiteSpace(activeName)
                ? "Current: None (select a form to view abilities)"
                : $"Current: {displayName}";
        }

        if (activeForm == null)
        {
            _lastAbilitiesFormName = string.Empty;
            _renderedAbilityCount = 0;
            _abilityRows.Clear();
            UIFactory.ClearChildren(_abilitiesListRoot);
            return;
        }

        int abilityCount = activeForm?.Abilities?.Count ?? 0;
        bool needsRebuild = !activeName.Equals(_lastAbilitiesFormName, StringComparison.OrdinalIgnoreCase)
                            || abilityCount != _renderedAbilityCount;

        if (needsRebuild)
        {
            _lastAbilitiesFormName = activeName;
            _renderedAbilityCount = abilityCount;
            RebuildAbilityRows(activeForm);
        }
        else
        {
            // Update cooldowns (in case they change without count change)
            for (int i = 0; i < _abilityRows.Count; i++)
            {
                if (activeForm == null || activeForm.Abilities == null || i >= activeForm.Abilities.Count)
                {
                    _abilityRows[i].Root.SetActive(false);
                    continue;
                }

                ExoFormAbilityData ability = activeForm.Abilities[i];
                UpdateAbilityRow(_abilityRows[i], ability);
            }
        }
    }

    private void RebuildAbilityRows(ExoFormEntry activeForm)
    {
        _abilityRows.Clear();
        UIFactory.ClearChildren(_abilitiesListRoot);

        if (activeForm == null || activeForm.Abilities == null || activeForm.Abilities.Count == 0)
        {
            if (_referenceText != null)
            {
                _ = CreateText(_abilitiesListRoot, _referenceText, "No abilities available.", _referenceText.fontSize * MetaFontScale, FontStyles.Italic, MetaColor);
            }
            return;
        }

        for (int i = 0; i < activeForm.Abilities.Count; i++)
        {
            AbilityRow row = CreateAbilityRow(_abilitiesListRoot, _referenceText);
            if (row != null)
            {
                _abilityRows.Add(row);
                UpdateAbilityRow(row, activeForm.Abilities[i]);
            }
        }
    }

    private void UpdateAbilityRow(AbilityRow row, ExoFormAbilityData ability)
    {
        if (row == null)
        {
            return;
        }

        row.Root.SetActive(true);
        row.Name.text = ResolveAbilityName(ability.AbilityId);
        row.Cooldown.text = $"{ability.Cooldown:0.0}s";
    }

    #endregion

    #region Data Helpers

    private static ExoFormEntry ResolveSelectedExoFormEntry()
    {
        if (_exoFormEntries == null || _exoFormEntries.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_exoFormCurrentForm))
        {
            return null;
        }

        return _exoFormEntries.FirstOrDefault(entry =>
            entry.FormName.Equals(_exoFormCurrentForm, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveFormUnlockNote(string formName)
    {
        if (string.IsNullOrWhiteSpace(formName))
        {
            return string.Empty;
        }

        if (formName.Equals("EvolvedVampire", StringComparison.OrdinalIgnoreCase))
        {
            return "Unlocked by consuming Dracula essence.";
        }

        if (formName.Equals("CorruptedSerpent", StringComparison.OrdinalIgnoreCase))
        {
            return "Requires Megara essence.";
        }

        return string.Empty;
    }

    private static string ResolveAbilityName(int abilityId)
    {
        PrefabGUID abilityGuid = new(abilityId);
        string abilityName = abilityGuid.GetLocalizedName();
        if (string.IsNullOrEmpty(abilityName) || abilityName.Equals("LocalizationKey.Empty"))
        {
            abilityName = abilityGuid.GetPrefabName();
        }

        if (string.IsNullOrEmpty(abilityName))
        {
            return $"Ability {abilityId}";
        }

        var match = HudData.AbilitySpellRegex.Match(abilityName);
        if (match.Success)
        {
            return match.Value.Replace('_', ' ');
        }

        return abilityName;
    }

    #endregion

    #region UI Helpers

    private static RectTransform CreateCard(Transform parent, string name, bool stretchHeight, bool enforceMinimumHeight = true)
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

        LayoutElement layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
        if (enforceMinimumHeight)
        {
            layoutElement.minHeight = 120f;
        }
        layoutElement.flexibleHeight = stretchHeight ? 1f : 0f;
        layoutElement.flexibleWidth = 1f;

        ContentSizeFitter fitter = rectTransform.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rectTransform;
    }

    private static RectTransform CreateCardHeader(Transform parent, TextMeshProUGUI reference, string title, string[] iconSprites)
    {
        RectTransform rectTransform = CreateRectTransformObject("ExoCardHeader", parent);
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

        LayoutElement layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 28f;
        layoutElement.preferredHeight = 28f;

        Image icon = UIFactory.CreateImage(rectTransform, "HeaderIcon", new Vector2(HeaderIconSize, HeaderIconSize), new Color(1f, 1f, 1f, 0.9f));
        if (icon != null)
        {
            ApplySprite(icon, iconSprites);
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

    private static RectTransform CreateDivider(Transform parent)
    {
        RectTransform rectTransform = CreateRectTransformObject("ExoDivider", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(image, DividerSpriteNames);
        image.color = new Color(1f, 1f, 1f, 0.18f);
        image.raycastTarget = false;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 6f;
        layout.preferredHeight = 6f;

        return rectTransform;
    }

    private static RectTransform CreateColumnDivider(Transform parent)
    {
        RectTransform rectTransform = CreateRectTransformObject("ExoColumnDivider", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(ColumnDividerWidth, 0f);
        rectTransform.anchoredPosition = Vector2.zero;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = ColumnDividerWidth;
        layout.minWidth = ColumnDividerWidth;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 1f;

        RectTransform imageRect = CreateRectTransformObject("ExoColumnDividerImage", rectTransform);
        if (imageRect == null)
        {
            return rectTransform;
        }

        float inset = (1f - ColumnDividerHeightPercent) * 0.5f;
        imageRect.anchorMin = new Vector2(0f, inset);
        imageRect.anchorMax = new Vector2(1f, 1f - inset);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        Image image = imageRect.gameObject.AddComponent<Image>();
        ApplySprite(image, ColumnDividerSpriteNames);
        image.type = Image.Type.Tiled;
        image.color = ColumnDividerColor;
        image.raycastTarget = false;

        return rectTransform;
    }

    private static RectTransform CreateChargeBar(Transform parent, out Image fill)
    {
        RectTransform rectTransform = CreateRectTransformObject("ExoChargeBar", parent);
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
        ApplySprite(background, ProgressBackgroundSpriteNames);
        background.color = ChargeBackgroundColor;
        background.type = Image.Type.Sliced;
        background.raycastTarget = false;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = ProgressHeight;
        layout.minHeight = ProgressHeight;
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
        Sprite fillSprite = ResolveSprite(ProgressFillSpriteNames);
        if (fillSprite != null)
        {
            fill.sprite = fillSprite;
        }
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        fill.color = ChargeFillColor;
        fill.raycastTarget = false;

        return rectTransform;
    }

    private static Transform CreateListRoot(Transform parent, string name, float spacing)
    {
        Transform root = UIFactory.CreateListRoot(parent, name, spacing);
        if (root != null)
        {
            ContentSizeFitter fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        return root;
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

            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

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

    private static SimpleStunButton CreateActionRowWithStatus(
        Transform parent,
        TextMeshProUGUI reference,
        string label,
        string[] iconSprites,
        out TextMeshProUGUI statusText,
        bool isPrimary)
    {
        statusText = null;

        RectTransform rectTransform = CreateRectTransformObject("ExoActionRow", parent);
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
        ApplySprite(background, RowSpriteNames);
        background.color = isPrimary ? PrimaryRowBackgroundColor : RowBackgroundColor;
        background.raycastTarget = true;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = ActionRowSpacing;
        layout.padding = CreatePadding(ActionRowPaddingHorizontal, ActionRowPaddingHorizontal, ActionRowPaddingVertical, ActionRowPaddingVertical);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = ActionRowHeight;
        rowLayout.preferredHeight = ActionRowHeight;

        Image icon = UIFactory.CreateImage(rectTransform, "Icon", new Vector2(ActionIconSize, ActionIconSize), new Color(1f, 1f, 1f, 0.9f));
        if (icon != null)
        {
            ApplySprite(icon, iconSprites);
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;

            if (icon.sprite == null)
            {
                icon.gameObject.SetActive(false);
            }
        }

        TextMeshProUGUI labelText = CreateText(rectTransform, reference, label, reference.fontSize * 0.56f, FontStyles.Normal, Color.white);
        if (labelText != null)
        {
            LayoutElement labelLayout = labelText.GetComponent<LayoutElement>() ?? labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
        }

        statusText = CreateText(rectTransform, reference, "--", reference.fontSize * 0.52f, FontStyles.Bold, MetaColor);
        if (statusText != null)
        {
            statusText.alignment = TextAlignmentOptions.Right;
            statusText.enableWordWrapping = false;
            LayoutElement statusLayout = statusText.GetComponent<LayoutElement>() ?? statusText.gameObject.AddComponent<LayoutElement>();
            statusLayout.preferredWidth = 52f;
            statusLayout.minWidth = 52f;
            statusLayout.flexibleWidth = 0f;
        }

        SimpleStunButton button = rectTransform.gameObject.AddComponent<SimpleStunButton>();
        ConfigureCommandButton(button, string.Empty, enabled: false);
        return button;
    }

    private static FormRow CreateFormRow(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform wrapper = CreateRectTransformObject("ExoFormRow", parent);
        if (wrapper == null)
        {
            return null;
        }

        wrapper.anchorMin = new Vector2(0f, 1f);
        wrapper.anchorMax = new Vector2(1f, 1f);
        wrapper.pivot = new Vector2(0f, 1f);
        wrapper.offsetMin = Vector2.zero;
        wrapper.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = wrapper.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = 2f;
        layout.padding = CreatePadding(0, 0, 0, 0);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        ContentSizeFitter fitter = wrapper.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform row = CreateRectTransformObject("RowButton", wrapper);
        if (row == null)
        {
            return null;
        }

        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0f, 1f);
        row.offsetMin = Vector2.zero;
        row.offsetMax = Vector2.zero;

        Image background = row.gameObject.AddComponent<Image>();
        ApplySprite(background, RowSpriteNames);
        background.color = RowBackgroundColor;
        background.raycastTarget = true;

        HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.spacing = ActionRowSpacing;
        rowLayout.padding = CreatePadding(ActionRowPaddingHorizontal, ActionRowPaddingHorizontal, ActionRowPaddingVertical, ActionRowPaddingVertical);
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;

        LayoutElement height = row.gameObject.AddComponent<LayoutElement>();
        height.minHeight = ActionRowHeight;
        height.preferredHeight = ActionRowHeight;

        Image icon = UIFactory.CreateImage(row, "Icon", new Vector2(ActionIconSize, ActionIconSize), new Color(1f, 1f, 1f, 0.9f));
        if (icon != null)
        {
            ApplySprite(icon, FormIconSpriteNames);
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;

            if (icon.sprite == null)
            {
                icon.gameObject.SetActive(false);
            }
        }

        TextMeshProUGUI labelText = CreateText(row, reference, "--", reference.fontSize * 0.56f, FontStyles.Normal, Color.white);
        if (labelText != null)
        {
            LayoutElement labelLayout = labelText.GetComponent<LayoutElement>() ?? labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
        }

        TextMeshProUGUI statusText = CreateText(row, reference, "--", reference.fontSize * 0.52f, FontStyles.Bold, MetaColor);
        if (statusText != null)
        {
            statusText.alignment = TextAlignmentOptions.Right;
            LayoutElement statusLayout = statusText.GetComponent<LayoutElement>() ?? statusText.gameObject.AddComponent<LayoutElement>();
            statusLayout.preferredWidth = 64f;
            statusLayout.minWidth = 64f;
            statusLayout.flexibleWidth = 0f;
        }

        SimpleStunButton button = row.gameObject.AddComponent<SimpleStunButton>();
        ConfigureCommandButton(button, string.Empty, enabled: false);

        TextMeshProUGUI noteText = CreateText(wrapper, reference, string.Empty, reference.fontSize * HintFontScale, FontStyles.Normal, HintColor, allowWrap: true);
        if (noteText != null)
        {
            LayoutElement noteLayout = noteText.GetComponent<LayoutElement>() ?? noteText.gameObject.AddComponent<LayoutElement>();
            noteLayout.minHeight = 0f;
        }

        return new FormRow(wrapper.gameObject, background, labelText, statusText, noteText, button);
    }

    private static AbilityRow CreateAbilityRow(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform rectTransform = CreateRectTransformObject("ExoAbilityRow", parent);
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
        ApplySprite(background, RowSpriteNames);
        background.color = RowBackgroundColor;
        background.raycastTarget = false;

        HorizontalLayoutGroup layout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = ActionRowSpacing;
        layout.padding = CreatePadding(ActionRowPaddingHorizontal, ActionRowPaddingHorizontal, ActionRowPaddingVertical, ActionRowPaddingVertical);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = ActionRowHeight;
        rowLayout.preferredHeight = ActionRowHeight;

        Image icon = UIFactory.CreateImage(rectTransform, "Icon", new Vector2(ActionIconSize, ActionIconSize), new Color(1f, 1f, 1f, 0.9f));
        if (icon != null)
        {
            ApplySprite(icon, AbilityIconSpriteNames);
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;

            if (icon.sprite == null)
            {
                icon.gameObject.SetActive(false);
            }
        }

        TextMeshProUGUI nameText = CreateText(rectTransform, reference, "--", reference.fontSize * 0.56f, FontStyles.Normal, Color.white);
        if (nameText != null)
        {
            LayoutElement nameLayout = nameText.GetComponent<LayoutElement>() ?? nameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;
        }

        TextMeshProUGUI cooldownText = CreateText(rectTransform, reference, "--", reference.fontSize * 0.52f, FontStyles.Normal, MetaColor);
        if (cooldownText != null)
        {
            cooldownText.alignment = TextAlignmentOptions.Right;
            LayoutElement cooldownLayout = cooldownText.GetComponent<LayoutElement>() ?? cooldownText.gameObject.AddComponent<LayoutElement>();
            cooldownLayout.preferredWidth = 56f;
            cooldownLayout.minWidth = 56f;
            cooldownLayout.flexibleWidth = 0f;
        }

        return new AbilityRow(rectTransform.gameObject, nameText, cooldownText);
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

    #endregion

    #region Sprite Helpers

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
            // Rule: hide missing icons to avoid placeholders.
            image.sprite = null;
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
    }

    #endregion

    #region UIFactory Wrappers

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
            DebugToolsBridge.TryLogWarning($"[Exoform Tab] Failed to configure vertical layout: {ex.Message}");
        }
    }

    #endregion

    #region Nested UI Row Types

    private sealed class FormRow
    {
        public GameObject Root { get; }
        public Image Background { get; }
        public TextMeshProUGUI Label { get; }
        public TextMeshProUGUI Status { get; }
        public TextMeshProUGUI Note { get; }
        public SimpleStunButton Button { get; }

        public FormRow(GameObject root, Image background, TextMeshProUGUI label, TextMeshProUGUI status, TextMeshProUGUI note, SimpleStunButton button)
        {
            Root = root;
            Background = background;
            Label = label;
            Status = status;
            Note = note;
            Button = button;
        }
    }

    private sealed class AbilityRow
    {
        public GameObject Root { get; }
        public TextMeshProUGUI Name { get; }
        public TextMeshProUGUI Cooldown { get; }

        public AbilityRow(GameObject root, TextMeshProUGUI name, TextMeshProUGUI cooldown)
        {
            Root = root;
            Name = name;
            Cooldown = cooldown;
        }
    }

    #endregion
}
