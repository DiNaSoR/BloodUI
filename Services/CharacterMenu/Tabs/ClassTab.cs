using Eclipse.Services.CharacterMenu.Base;
using Eclipse.Services.CharacterMenu.Interfaces;
using Eclipse.Services.CharacterMenu.Shared;
using Eclipse.Services.HUD.Shared;
using Eclipse.Utilities;
using ProjectM.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static Eclipse.Services.CanvasService.DataHUD;
using static Eclipse.Services.DataService;

namespace Eclipse.Services.CharacterMenu.Tabs;

/// <summary>
/// Character menu tab for displaying class selection, spells, and stat synergies.
/// </summary>
internal class ClassTab : CharacterMenuTabBase, ICharacterMenuTabWithPanel
{
    #region Constants

    private const float SectionSpacing = 8f;
    private const float RowSpacing = 4f;
    private const float RowHeight = 32f;
    private const float SpellRowHeight = 28f;
    private const float ClassIconSize = 14f;
    private const float SpellIndexSize = 20f;
    private const int CardPadding = 10;
    private const float CardInnerSpacing = 6f;

    private const float TitleFontScale = 0.82f;
    private const float RowFontScale = 0.75f;
    private const float SmallFontScale = 0.65f;
    private const float ChipFontScale = 0.55f;

    private static readonly Color CardBackgroundColor = new(0f, 0f, 0f, 0.32f);
    private static readonly Color HeaderBackgroundColor = new(0.1f, 0.1f, 0.12f, 0.95f);
    private static readonly Color RowBackgroundColor = new(0.05f, 0.05f, 0.08f, 0.75f);
    private static readonly Color ActiveRowBackgroundColor = new(0.5f, 0.05f, 0.06f, 0.35f);
    private static readonly Color TitleColor = new(0.95f, 0.84f, 0.7f, 1f);
    private static readonly Color MetaColor = new(1f, 1f, 1f, 0.55f);
    private static readonly Color HintColor = new(1f, 1f, 1f, 0.48f);
    private static readonly Color ActiveColor = new(0.62f, 0.95f, 0.71f, 1f);

    private static readonly string[] CardSpriteNames = ["Window_Box", "Window_Box_Background", "SimpleBox_Normal"];
    private static readonly string[] HeaderSpriteNames = ["Act_BG", "TabGradient", "Window_Box_Background"];

    private static readonly Dictionary<PlayerClass, Color> ClassColors = new()
    {
        { PlayerClass.None, Color.gray },
        { PlayerClass.BloodKnight, new Color(1f, 0f, 0f, 1f) },
        { PlayerClass.DemonHunter, new Color(1f, 0.8f, 0f, 1f) },
        { PlayerClass.VampireLord, new Color(0f, 1f, 1f, 1f) },
        { PlayerClass.ShadowBlade, new Color(0.6f, 0.2f, 1f, 1f) },
        { PlayerClass.ArcaneSorcerer, new Color(0f, 0.5f, 0.5f, 1f) },
        { PlayerClass.DeathMage, new Color(0f, 1f, 0f, 1f) }
    };

    private static readonly Dictionary<PlayerClass, string> ClassOnHitEffects = new()
    {
        { PlayerClass.None, "" },
        { PlayerClass.BloodKnight, "Leech → Blood Curse" },
        { PlayerClass.DemonHunter, "Static → Storm Charge" },
        { PlayerClass.VampireLord, "Chill → Frost Weapon" },
        { PlayerClass.ShadowBlade, "Ignite → Chaos Heated" },
        { PlayerClass.ArcaneSorcerer, "Weaken → Illusion Shield" },
        { PlayerClass.DeathMage, "Condemn → Unholy Amplify" }
    };

    #endregion

    #region Fields

    private RectTransform _panelRoot;
    private TextMeshProUGUI _referenceText;
    private Transform _contentRoot;

    // Class Selection
    private TextMeshProUGUI _currentClassText;
    private TextMeshProUGUI _onHitEffectText;
    private Transform _classListRoot;
    private readonly List<ClassRowUI> _classRows = [];

    // Class Spells
    private TextMeshProUGUI _shiftSlotText;
    private TextMeshProUGUI _shiftToggleText;
    private Image _shiftToggleImage;
    private Transform _spellListRoot;
    private readonly List<SpellRowUI> _spellRows = [];

    // Stat Synergies
    private TextMeshProUGUI _weaponSynergyText;
    private TextMeshProUGUI _bloodSynergyText;

    #endregion

    #region Properties

    public override string TabId => "Class";
    public override string TabLabel => "Class";
    public override string SectionTitle => "Class System";
    public override BloodcraftTab TabType => BloodcraftTab.Progression;

    #endregion

    #region Nested Types

    private sealed class ClassRowUI(GameObject root, PlayerClass playerClass, TextMeshProUGUI nameText, TextMeshProUGUI statusText, Image background)
    {
        public GameObject Root { get; } = root;
        public PlayerClass PlayerClass { get; } = playerClass;
        public TextMeshProUGUI NameText { get; } = nameText;
        public TextMeshProUGUI StatusText { get; } = statusText;
        public Image Background { get; } = background;
    }

    private sealed class SpellRowUI(GameObject root, int index, TextMeshProUGUI nameText, TextMeshProUGUI reqText, Image background)
    {
        public GameObject Root { get; } = root;
        public int Index { get; } = index;
        public TextMeshProUGUI NameText { get; } = nameText;
        public TextMeshProUGUI ReqText { get; } = reqText;
        public Image Background { get; } = background;
    }

    #endregion

    #region ICharacterMenuTabWithPanel

    public Transform CreatePanel(Transform parent, TextMeshProUGUI reference)
    {
        Reset();
        _referenceText = reference;

        RectTransform rectTransform = CreateRectTransformObject("BloodcraftClass", parent);
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

        _contentRoot = CreateContentRoot(rectTransform, reference);
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

        UpdateClassSelection();
        UpdateClassSpells();
        UpdateStatSynergies();
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
        _referenceText = null;
        _contentRoot = null;
        _currentClassText = null;
        _onHitEffectText = null;
        _classListRoot = null;
        _classRows.Clear();
        _shiftSlotText = null;
        _shiftToggleText = null;
        _shiftToggleImage = null;
        _spellListRoot = null;
        _spellRows.Clear();
        _weaponSynergyText = null;
        _bloodSynergyText = null;
    }

    #endregion

    #region Panel Construction

    private Transform CreateContentRoot(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform root = CreateRectTransformObject("ClassContentRoot", parent);
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

        // Class Selection Card
        CreateClassSelectionCard(root, reference);

        // Divider
        _ = CreateDivider(root);

        // Class Spells Card
        CreateClassSpellsCard(root, reference);

        // Divider
        _ = CreateDivider(root);

        // Stat Synergies Card
        CreateStatSynergiesCard(root, reference);

        return root;
    }

    private void CreateClassSelectionCard(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform card = CreateCard(parent, "ClassSelectionCard");
        if (card == null)
        {
            return;
        }

        _ = CreateCardHeader(card, reference, "CLASS SELECTION");

        _currentClassText = CreateTextRow(card, reference, "Current: None", TitleFontScale, FontStyles.Bold, TitleColor);
        _onHitEffectText = CreateTextRow(card, reference, "On-Hit: ", SmallFontScale, FontStyles.Normal, MetaColor);

        _classListRoot = CreateListRoot(card, "ClassList", RowSpacing);

        // Create class rows
        foreach (PlayerClass playerClass in Enum.GetValues(typeof(PlayerClass)))
        {
            if (playerClass == PlayerClass.None)
            {
                continue;
            }

            ClassRowUI row = CreateClassRow(_classListRoot, reference, playerClass);
            if (row != null)
            {
                _classRows.Add(row);
            }
        }

        _ = CreateTextRow(card, reference, "Click a class to select or change.", ChipFontScale, FontStyles.Italic, HintColor);
    }

    private ClassRowUI CreateClassRow(Transform parent, TextMeshProUGUI reference, PlayerClass playerClass)
    {
        RectTransform rectTransform = CreateRectTransformObject($"ClassRow_{playerClass}", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // Background
        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, CardSpriteNames);
        background.color = RowBackgroundColor;
        background.type = Image.Type.Sliced;
        background.raycastTarget = true;

        // Layout
        HorizontalLayoutGroup hLayout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.spacing = 10f;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;
        hLayout.padding = CreatePadding(10, 10, 6, 6);

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = RowHeight;
        rowLayout.minHeight = RowHeight;
        rowLayout.flexibleWidth = 1f;

        // Button
        SimpleStunButton button = rectTransform.gameObject.AddComponent<SimpleStunButton>();
        if (button != null)
        {
            string className = playerClass.ToString();
            button.onClick.AddListener((UnityAction)(() =>
            {
                string cmd = _classType == PlayerClass.None
                    ? $".class s {className}"
                    : $".class c {className}";
                Quips.SendCommand(cmd);
            }));
        }

        // Icon
        RectTransform iconRect = CreateRectTransformObject("ClassIcon", rectTransform);
        if (iconRect != null)
        {
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.color = ClassColors.GetValueOrDefault(playerClass, Color.gray);
            icon.raycastTarget = false;

            LayoutElement iconLayout = iconRect.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = ClassIconSize;
            iconLayout.minWidth = ClassIconSize;
            iconLayout.preferredHeight = ClassIconSize;
            iconLayout.minHeight = ClassIconSize;
        }

        // Class Name
        TextMeshProUGUI nameText = CreateRowText(rectTransform, reference, HudUtilities.SplitPascalCase(playerClass.ToString()), RowFontScale, FontStyles.Normal);
        if (nameText != null)
        {
            nameText.color = ClassColors.GetValueOrDefault(playerClass, Color.white);
            LayoutElement nameLayout = nameText.GetComponent<LayoutElement>() ?? nameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;
        }

        // Status Text
        TextMeshProUGUI statusText = CreateRowText(rectTransform, reference, "", SmallFontScale, FontStyles.Bold);
        if (statusText != null)
        {
            statusText.color = ActiveColor;
            statusText.alignment = TextAlignmentOptions.Right;
            LayoutElement statusLayout = statusText.GetComponent<LayoutElement>() ?? statusText.gameObject.AddComponent<LayoutElement>();
            statusLayout.preferredWidth = 80f;
            statusLayout.minWidth = 80f;
        }

        return new ClassRowUI(rectTransform.gameObject, playerClass, nameText, statusText, background);
    }

    private void CreateClassSpellsCard(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform card = CreateCard(parent, "ClassSpellsCard");
        if (card == null)
        {
            return;
        }

        _ = CreateCardHeader(card, reference, "CLASS SPELLS");

        // Shift toggle row with button
        RectTransform shiftRow = CreateRectTransformObject("ShiftRow", card);
        if (shiftRow != null)
        {
            shiftRow.anchorMin = new Vector2(0f, 1f);
            shiftRow.anchorMax = new Vector2(1f, 1f);
            shiftRow.pivot = new Vector2(0f, 1f);
            shiftRow.offsetMin = Vector2.zero;
            shiftRow.offsetMax = Vector2.zero;

            HorizontalLayoutGroup shiftLayout = shiftRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            shiftLayout.childAlignment = TextAnchor.MiddleLeft;
            shiftLayout.spacing = 10f;
            shiftLayout.childControlWidth = true;
            shiftLayout.childControlHeight = true;
            shiftLayout.childForceExpandWidth = false;
            shiftLayout.childForceExpandHeight = false;

            LayoutElement shiftRowLayout = shiftRow.gameObject.AddComponent<LayoutElement>();
            shiftRowLayout.preferredHeight = 28f;

            _shiftSlotText = CreateRowText(shiftRow, reference, "Shift Slot: Ready", SmallFontScale, FontStyles.Normal);
            if (_shiftSlotText != null)
            {
                _shiftSlotText.color = MetaColor;
                LayoutElement textLayout = _shiftSlotText.GetComponent<LayoutElement>() ?? _shiftSlotText.gameObject.AddComponent<LayoutElement>();
                textLayout.flexibleWidth = 1f;
            }

            // Toggle button
            RectTransform toggleBtn = CreateRectTransformObject("ShiftToggleBtn", shiftRow);
            if (toggleBtn != null)
            {
                _shiftToggleImage = toggleBtn.gameObject.AddComponent<Image>();
                // Set initial appearance based on current state
                bool isShiftOn = _classShiftSlotEnabled;
                _shiftToggleImage.color = isShiftOn 
                    ? new Color(0.2f, 0.7f, 0.2f, 1f)  // Bright green for ON
                    : new Color(0.6f, 0.2f, 0.2f, 1f); // Dark red for OFF
                _shiftToggleImage.raycastTarget = true;

                LayoutElement btnLayout = toggleBtn.gameObject.AddComponent<LayoutElement>();
                btnLayout.preferredWidth = 50f;
                btnLayout.minWidth = 50f;
                btnLayout.preferredHeight = 24f;

                _shiftToggleText = CreateRowText(toggleBtn, reference, isShiftOn ? "ON" : "OFF", ChipFontScale, FontStyles.Bold);
                if (_shiftToggleText != null)
                {
                    _shiftToggleText.alignment = TextAlignmentOptions.Center;
                    _shiftToggleText.color = Color.white;
                }

                SimpleStunButton toggleButton = toggleBtn.gameObject.AddComponent<SimpleStunButton>();
                if (toggleButton != null)
                {
                    toggleButton.onClick.AddListener((UnityAction)(() =>
                    {
                        // Optimistic UI update - toggle visual state immediately
                        _classShiftSlotEnabled = !_classShiftSlotEnabled;
                        UpdateClassSpells(); // Refresh the toggle display
                        Quips.SendCommand(".class shift");
                    }));
                }
            }
        }

        _spellListRoot = CreateListRoot(card, "SpellList", RowSpacing);

        // Create spell rows
        for (int i = 0; i < 4; i++)
        {
            SpellRowUI row = CreateSpellRow(_spellListRoot, reference, i);
            if (row != null)
            {
                _spellRows.Add(row);
            }
        }

        _ = CreateTextRow(card, reference, "Click a spell to set your Shift ability.", ChipFontScale, FontStyles.Italic, HintColor);
    }

    private SpellRowUI CreateSpellRow(Transform parent, TextMeshProUGUI reference, int index)
    {
        RectTransform rectTransform = CreateRectTransformObject($"SpellRow_{index}", parent);
        if (rectTransform == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // Background
        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, CardSpriteNames);
        background.color = RowBackgroundColor;
        background.type = Image.Type.Sliced;
        background.raycastTarget = true;

        // Layout
        HorizontalLayoutGroup hLayout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.spacing = 10f;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;
        hLayout.padding = CreatePadding(10, 10, 4, 4);

        LayoutElement rowLayout = rectTransform.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = SpellRowHeight;
        rowLayout.minHeight = SpellRowHeight;
        rowLayout.flexibleWidth = 1f;

        // Button
        SimpleStunButton button = rectTransform.gameObject.AddComponent<SimpleStunButton>();
        if (button != null)
        {
            int spellIndex = index;
            button.onClick.AddListener((UnityAction)(() =>
            {
                Quips.SendCommand($".class csp {spellIndex}");
            }));
        }

        // Index badge
        RectTransform indexRect = CreateRectTransformObject("SpellIndex", rectTransform);
        if (indexRect != null)
        {
            Image indexBg = indexRect.gameObject.AddComponent<Image>();
            indexBg.color = new Color(0.31f, 0.31f, 0.35f, 0.6f);
            indexBg.raycastTarget = false;

            TextMeshProUGUI indexText = CreateRowText(indexRect, reference, index.ToString(), ChipFontScale, FontStyles.Normal);
            if (indexText != null)
            {
                indexText.alignment = TextAlignmentOptions.Center;
                indexText.color = new Color(1f, 1f, 1f, 0.7f);
            }

            LayoutElement indexLayout = indexRect.gameObject.AddComponent<LayoutElement>();
            indexLayout.preferredWidth = SpellIndexSize;
            indexLayout.minWidth = SpellIndexSize;
            indexLayout.preferredHeight = SpellIndexSize;
            indexLayout.minHeight = SpellIndexSize;
        }

        // Spell Name
        TextMeshProUGUI nameText = CreateRowText(rectTransform, reference, $"Spell {index + 1}", SmallFontScale, FontStyles.Normal);
        if (nameText != null)
        {
            nameText.color = new Color(0.91f, 0.89f, 0.84f, 1f);
            LayoutElement nameLayout = nameText.GetComponent<LayoutElement>() ?? nameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;
        }

        // Requirement Text
        TextMeshProUGUI reqText = CreateRowText(rectTransform, reference, "", ChipFontScale, FontStyles.Normal);
        if (reqText != null)
        {
            reqText.color = new Color(1f, 0.5f, 0.5f, 0.8f);
            reqText.alignment = TextAlignmentOptions.Right;
            LayoutElement reqLayout = reqText.GetComponent<LayoutElement>() ?? reqText.gameObject.AddComponent<LayoutElement>();
            reqLayout.preferredWidth = 40f;
        }

        return new SpellRowUI(rectTransform.gameObject, index, nameText, reqText, background);
    }

    private void CreateStatSynergiesCard(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform card = CreateCard(parent, "StatSynergiesCard");
        if (card == null)
        {
            return;
        }

        _ = CreateCardHeader(card, reference, "STAT SYNERGIES");

        _ = CreateTextRow(card, reference, "Your class gives bonuses to specific stats from weapon and blood types.", SmallFontScale, FontStyles.Normal, MetaColor);

        _weaponSynergyText = CreateTextRow(card, reference, "Weapon Stats: Loading...", SmallFontScale, FontStyles.Normal, new Color(1f, 0.9f, 0.8f, 1f));
        _bloodSynergyText = CreateTextRow(card, reference, "Blood Stats: Loading...", SmallFontScale, FontStyles.Normal, new Color(0.9f, 0.8f, 0.8f, 1f));
    }

    #endregion

    #region Panel Updates

    private void UpdateClassSelection()
    {
        PlayerClass currentClass = _classType;

        if (_currentClassText != null)
        {
            string className = currentClass == PlayerClass.None
                ? "None"
                : HudUtilities.SplitPascalCase(currentClass.ToString());
            Color classColor = ClassColors.GetValueOrDefault(currentClass, Color.white);
            _currentClassText.text = $"Current: <color=#{ColorUtility.ToHtmlStringRGB(classColor)}>{className}</color>";
        }

        if (_onHitEffectText != null)
        {
            string effect = ClassOnHitEffects.GetValueOrDefault(currentClass, "");
            _onHitEffectText.text = $"On-Hit: {effect}";
            _onHitEffectText.gameObject.SetActive(!string.IsNullOrEmpty(effect));
        }

        foreach (ClassRowUI row in _classRows)
        {
            bool isActive = row.PlayerClass == currentClass;

            if (row.StatusText != null)
            {
                row.StatusText.text = isActive ? "ACTIVE" : "";
            }

            if (row.Background != null)
            {
                row.Background.color = isActive ? ActiveRowBackgroundColor : RowBackgroundColor;
            }
        }
    }

    private void UpdateClassSpells()
    {
        // Update toggle button state
        bool isShiftEnabled = _classShiftSlotEnabled;
        if (_shiftToggleText != null)
        {
            _shiftToggleText.text = isShiftEnabled ? "ON" : "OFF";
        }
        if (_shiftToggleImage != null)
        {
            if (isShiftEnabled)
            {
                _shiftToggleImage.color = new Color(0.2f, 0.7f, 0.2f, 1f); // Bright green for ON
            }
            else
            {
                _shiftToggleImage.color = new Color(0.6f, 0.2f, 0.2f, 1f); // Dark red for OFF
            }
        }

        if (_shiftSlotText != null)
        {
            bool hasShift = _shiftSpellIndex >= 0;
            string statusColor = hasShift ? "#9ef2b5" : "#ffffff";
            string statusText = hasShift ? "Equipped" : isShiftEnabled ? "Ready" : "Disabled";
            _shiftSlotText.text = $"Shift Slot: <color={statusColor}>{statusText}</color>";
        }

        // Get spells for current class
        List<int> classSpells = null;
        if (_classSpells != null && _classType != PlayerClass.None)
        {
            _classSpells.TryGetValue(_classType, out classSpells);
        }

        int spellCount = classSpells?.Count ?? 0;
        int unlockCount = _classSpellUnlockLevels?.Count ?? 0;

        for (int i = 0; i < _spellRows.Count; i++)
        {
            SpellRowUI row = _spellRows[i];
            bool hasSpell = classSpells != null && i < spellCount;

            if (row.NameText != null)
            {
                if (hasSpell)
                {
                    int spellId = classSpells[i];
                    string spellName = ResolveSpellName(spellId);
                    row.NameText.text = spellName;
                }
                else
                {
                    row.NameText.text = "---";
                }
            }

            if (row.ReqText != null)
            {
                if (i < unlockCount && _classSpellUnlockLevels != null)
                {
                    int req = _classSpellUnlockLevels[i];
                    row.ReqText.text = req > 0 ? $"(P{req})" : "";
                }
                else
                {
                    row.ReqText.text = "";
                }
            }

            if (row.Background != null)
            {
                bool isSelected = i == _shiftSpellIndex;
                row.Background.color = isSelected ? ActiveRowBackgroundColor : RowBackgroundColor;
            }
        }
    }

    private void UpdateStatSynergies()
    {
        // Get synergies for current class
        (List<WeaponStatType> WeaponStats, List<BloodStatType> BloodStats) synergies = (null, null);
        bool hasSynergies = _classStatSynergies != null && _classType != PlayerClass.None
            && _classStatSynergies.TryGetValue(_classType, out synergies);

        if (_weaponSynergyText != null)
        {
            if (hasSynergies && synergies.WeaponStats != null && synergies.WeaponStats.Count > 0)
            {
                var statNames = synergies.WeaponStats.Select(s => HudUtilities.SplitPascalCase(s.ToString()));
                _weaponSynergyText.text = $"Weapon Stats (1.5x): {string.Join(", ", statNames)}";
            }
            else
            {
                _weaponSynergyText.text = "Weapon Stats: None";
            }
        }

        if (_bloodSynergyText != null)
        {
            if (hasSynergies && synergies.BloodStats != null && synergies.BloodStats.Count > 0)
            {
                var statNames = synergies.BloodStats.Select(s => HudUtilities.SplitPascalCase(s.ToString()));
                _bloodSynergyText.text = $"Blood Stats (1.5x): {string.Join(", ", statNames)}";
            }
            else
            {
                _bloodSynergyText.text = "Blood Stats: None";
            }
        }
    }

    private static string ResolveSpellName(int spellId)
    {
        if (spellId == 0)
        {
            return "None";
        }

        Stunlock.Core.PrefabGUID spellGuid = new(spellId);
        string spellName = spellGuid.GetLocalizedName();
        if (string.IsNullOrEmpty(spellName) || spellName.Equals("LocalizationKey.Empty"))
        {
            spellName = spellGuid.GetPrefabName();
        }

        if (string.IsNullOrEmpty(spellName))
        {
            return $"Spell {spellId}";
        }

        var match = HudData.AbilitySpellRegex.Match(spellName);
        if (match.Success)
        {
            return match.Value.Replace('_', ' ');
        }

        return spellName;
    }

    #endregion

    #region UI Helpers

    private static RectTransform CreateCard(Transform parent, string name)
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
        layout.padding = CreatePadding(CardPadding, CardPadding, CardPadding, CardPadding);
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
        RectTransform rectTransform = CreateRectTransformObject("CardHeader", parent);
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

        LayoutElement layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 28f;
        layoutElement.preferredHeight = 28f;

        // Create child text element that fills the header
        RectTransform textRect = CreateRectTransformObject("HeaderText", rectTransform);
        if (textRect != null)
        {
            // Stretch to fill parent
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (text != null)
            {
                if (reference != null)
                {
                    UIFactory.CopyTextStyle(reference, text);
                }
                text.text = title;
                text.fontSize = reference != null ? reference.fontSize * 0.7f : 12f;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color(0.95f, 0.9f, 0.85f, 1f);
                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        return rectTransform;
    }

    private static TextMeshProUGUI CreateTextRow(Transform parent, TextMeshProUGUI reference, string text, float fontScale, FontStyles style, Color color)
    {
        RectTransform rectTransform = CreateRectTransformObject("TextRow", parent);
        if (rectTransform == null || reference == null)
        {
            return null;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI textComponent = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        UIFactory.CopyTextStyle(reference, textComponent);
        textComponent.text = text;
        textComponent.fontSize = reference.fontSize * fontScale;
        textComponent.fontStyle = style;
        textComponent.alignment = TextAlignmentOptions.Left;
        textComponent.color = color;
        textComponent.enableWordWrapping = true;
        textComponent.overflowMode = TextOverflowModes.Ellipsis;

        float height = textComponent.fontSize * 1.4f;
        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;

        return textComponent;
    }

    private static TextMeshProUGUI CreateRowText(Transform parent, TextMeshProUGUI reference, string text, float fontScale, FontStyles style)
    {
        RectTransform rectTransform = CreateRectTransformObject("RowText", parent);
        if (rectTransform == null || reference == null)
        {
            return null;
        }

        TextMeshProUGUI textComponent = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        UIFactory.CopyTextStyle(reference, textComponent);
        textComponent.text = text;
        textComponent.fontSize = reference.fontSize * fontScale;
        textComponent.fontStyle = style;
        textComponent.alignment = TextAlignmentOptions.Left;
        textComponent.color = Color.white;
        textComponent.enableWordWrapping = false;
        textComponent.overflowMode = TextOverflowModes.Ellipsis;

        // Add layout element so text gets proper size in layout groups
        float height = textComponent.fontSize * 1.3f;
        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.minHeight = height;

        return textComponent;
    }

    private static Transform CreateListRoot(Transform parent, string name, float spacing)
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
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        ContentSizeFitter fitter = rectTransform.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rectTransform;
    }

    private static RectTransform CreateDivider(Transform parent)
    {
        RectTransform rectTransform = CreateRectTransformObject("Divider", parent);
        if (rectTransform == null)
        {
            return null;
        }

        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.15f);
        image.raycastTarget = false;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 1f;
        layout.flexibleWidth = 1f;

        return rectTransform;
    }

    private static RectOffset CreatePadding(int left, int right, int top, int bottom)
    {
        RectOffset padding = new()
        {
            left = left,
            right = right,
            top = top,
            bottom = bottom
        };
        return padding;
    }

    private static void EnsureVerticalLayout(Transform root, float spacing = 0f)
    {
        if (root == null)
        {
            return;
        }

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = root.gameObject.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = root.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static RectTransform CreateRectTransformObject(string name, Transform parent)
        => UIFactory.CreateRectTransformObject(name, parent);

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

    #endregion
}
