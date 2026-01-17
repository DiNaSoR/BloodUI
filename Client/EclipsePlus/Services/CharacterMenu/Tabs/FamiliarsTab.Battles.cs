using Eclipse.Services.CharacterMenu.Shared;
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

namespace Eclipse.Services.CharacterMenu.Tabs;

internal partial class FamiliarsTab
{
    private const float BattleSlotRowHeight = 28f;
    private const float BattleConfirmWindowSeconds = 2.5f;

    private TextMeshProUGUI _battlesStatusText;

    private TextMeshProUGUI _battleGroupSelectedText;
    private RectTransform _battleGroupDropdownListRoot;
    private readonly List<FamiliarBoxOptionRow> _battleGroupOptionRows = [];

    private Transform _battleSlotsListRoot;
    private readonly List<BattleSlotRow> _battleSlotRows = [];

    private TextMeshProUGUI _deleteBattleGroupLabel;
    private bool _deleteBattleGroupConfirmArmed;
    private float _deleteBattleGroupConfirmUntil;

    private void CreateFamiliarBattlesPanel(Transform parent, TextMeshProUGUI reference)
    {
        if (parent == null || reference == null)
        {
            return;
        }

        _battlesStatusText = UIFactory.CreateSectionSubHeader(parent, reference, string.Empty);
        if (_battlesStatusText != null)
        {
            _battlesStatusText.alignment = TextAlignmentOptions.Left;
            _battlesStatusText.color = FamiliarStatusTextColor;
        }

        RectTransform topRow = CreateRectTransformObject("FamiliarBattlesTopRow", parent);
        if (topRow == null)
        {
            return;
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

        Transform leftColumn = CreateFamiliarColumn(topRow, "FamiliarBattlesLeftColumn", FamiliarSectionSpacing);
        _ = CreateFamiliarColumnDivider(topRow);
        Transform rightColumn = CreateFamiliarColumn(topRow, "FamiliarBattlesRightColumn", FamiliarRightColumnSpacing);

        CreateBattleGroupsCard(leftColumn, reference);
        CreateBattleSlotsCard(rightColumn, reference);
    }

    private void CreateBattleGroupsCard(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform card = CreateFamiliarCard(parent, "BattleGroupsCard", stretchHeight: false, enforceMinimumHeight: false);
        if (card == null)
        {
            return;
        }

        _ = CreateFamiliarSectionLabel(card, reference, "Battle Groups", FamiliarHeaderBattleIconSpriteNames);

        _ = CreateFamiliarSubHeaderRow(card, reference, "Active Group");
        _ = CreateFamiliarDropdownRow(card, reference, out _battleGroupSelectedText, ToggleBattleGroupDropdown);
        _battleGroupDropdownListRoot = CreateFamiliarBattleGroupDropdownList(card);

        Transform listRoot = CreateFamiliarActionList(card);
        if (listRoot == null)
        {
            return;
        }

        _ = CreateFamiliarActionRow(
            listRoot,
            reference,
            "Create Group (Auto Name)",
            CreateBattleGroupAuto,
            FamiliarActionIconSearchSpriteNames,
            false);

        _deleteBattleGroupLabel = CreateFamiliarActionRow(
            listRoot,
            reference,
            "Delete Active Group",
            DeleteBattleGroupMaybeConfirm,
            FamiliarActionIconUnbindSpriteNames,
            false);

        _ = CreateFamiliarActionRow(
            listRoot,
            reference,
            "Queue Status",
            ".fam challenge",
            FamiliarActionIconOverflowSpriteNames,
            false);

        _ = CreateFamiliarActionRow(
            listRoot,
            reference,
            "Set Arena Here (Admin)",
            ".fam sba",
            FamiliarActionIconToggleSpriteNames,
            false);
    }

    private void CreateBattleSlotsCard(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform card = CreateFamiliarCard(parent, "BattleSlotsCard", stretchHeight: true);
        if (card == null)
        {
            return;
        }

        _ = CreateFamiliarSectionLabel(card, reference, "Active Group Slots", FamiliarHeaderBattleIconSpriteNames);

        _battleSlotsListRoot = CreateRectTransformObject("BattleSlotsList", card);
        if (_battleSlotsListRoot == null)
        {
            return;
        }

        VerticalLayoutGroup layout = _battleSlotsListRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = FamiliarActionSpacing;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        EnsureBattleSlotRows(3, reference);

        _ = CreateFamiliarDivider(card);

        Transform actionsRoot = CreateFamiliarActionList(card);
        if (actionsRoot == null)
        {
            return;
        }

        _ = CreateFamiliarActionRow(actionsRoot, reference, "Assign Active Familiar → Slot 1", () => Quips.SendCommand(".fam sbg 1"), FamiliarActionIconCallSpriteNames, true);
        _ = CreateFamiliarActionRow(actionsRoot, reference, "Assign Active Familiar → Slot 2", () => Quips.SendCommand(".fam sbg 2"), FamiliarActionIconCallSpriteNames, false);
        _ = CreateFamiliarActionRow(actionsRoot, reference, "Assign Active Familiar → Slot 3", () => Quips.SendCommand(".fam sbg 3"), FamiliarActionIconCallSpriteNames, false);
    }

    private static RectTransform CreateFamiliarBattleGroupDropdownList(Transform parent)
    {
        // Same styling as the box dropdown list (but separate instance + storage)
        return CreateFamiliarBoxDropdownList(parent);
    }

    private void ToggleBattleGroupDropdown()
    {
        if (_battleGroupDropdownListRoot == null)
        {
            return;
        }

        bool isVisible = _battleGroupDropdownListRoot.gameObject.activeSelf;
        SetBattleGroupDropdownVisible(!isVisible);
    }

    private void SetBattleGroupDropdownVisible(bool visible)
    {
        if (_battleGroupDropdownListRoot == null)
        {
            return;
        }

        _battleGroupDropdownListRoot.gameObject.SetActive(visible);
        if (visible)
        {
            UpdateBattleGroupDropdownOptions();
        }
    }

    private void UpdateBattleGroupDropdownOptions()
    {
        if (_battleGroupDropdownListRoot == null)
        {
            return;
        }

        List<string> groupNames = _familiarBattleGroups.Select(bg => bg.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToList();
        if (groupNames.Count == 0)
        {
            EnsureBattleGroupOptionRows(1, _referenceText);
            FamiliarBoxOptionRow placeholder = _battleGroupOptionRows[0];
            if (placeholder.NameText != null)
            {
                placeholder.NameText.text = "No battle groups";
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

        EnsureBattleGroupOptionRows(groupNames.Count, _referenceText);

        for (int i = 0; i < groupNames.Count && i < _battleGroupOptionRows.Count; i++)
        {
            FamiliarBoxOptionRow row = _battleGroupOptionRows[i];
            string groupName = groupNames[i];
            bool isSelected = !string.IsNullOrWhiteSpace(_familiarActiveBattleGroup)
                && groupName.Equals(_familiarActiveBattleGroup, StringComparison.OrdinalIgnoreCase);

            if (row.NameText != null)
            {
                row.NameText.text = groupName;
                row.NameText.alpha = 1f;
            }

            if (row.Background != null)
            {
                row.Background.color = isSelected ? FamiliarPrimaryActionBackgroundColor : FamiliarActionBackgroundColor;
                row.Background.raycastTarget = true;
            }

            if (row.Button != null
                && (!string.Equals(row.LastBoxName, groupName, StringComparison.Ordinal)
                    || row.LastIsSelected != isSelected
                    || row.LastButtonEnabled != true))
            {
                ConfigureActionButton(row.Button, () => SelectBattleGroup(groupName), true);
                row.LastBoxName = groupName;
                row.LastIsSelected = isSelected;
                row.LastButtonEnabled = true;
            }
        }
    }

    private void EnsureBattleGroupOptionRows(int count, TextMeshProUGUI reference)
    {
        if (_battleGroupDropdownListRoot == null || reference == null)
        {
            return;
        }

        while (_battleGroupOptionRows.Count < count)
        {
            FamiliarBoxOptionRow row = CreateBattleGroupOptionRow(_battleGroupDropdownListRoot, reference, _battleGroupOptionRows.Count);
            if (row?.Root == null)
            {
                break;
            }

            _battleGroupOptionRows.Add(row);
        }

        for (int i = 0; i < _battleGroupOptionRows.Count; i++)
        {
            var row = _battleGroupOptionRows[i];
            if (row?.Root != null)
            {
                row.Root.SetActive(i < count);
            }
        }
    }

    private static FamiliarBoxOptionRow CreateBattleGroupOptionRow(Transform parent, TextMeshProUGUI reference, int index)
    {
        RectTransform rectTransform = CreateRectTransformObject($"BattleGroupOption_{index + 1}", parent);
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

    private void SelectBattleGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return;
        }

        SetBattleGroupDropdownVisible(false);
        _familiarActiveBattleGroup = groupName;
        Quips.SendCommand($".fam cbg {QuoteChatArgument(groupName)}");
    }

    private void CreateBattleGroupAuto()
    {
        string name = GenerateAutoBattleGroupName();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        Quips.SendCommand($".fam abg {QuoteChatArgument(name)}");
    }

    private string GenerateAutoBattleGroupName()
    {
        HashSet<string> existing = new(_familiarBattleGroups.Select(bg => bg.Name), StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i <= 999; i++)
        {
            string candidate = $"group{i.ToString(CultureInfo.InvariantCulture)}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"group{DateTime.UtcNow.ToString("HHmmss", CultureInfo.InvariantCulture)}";
    }

    private void DeleteBattleGroupMaybeConfirm()
    {
        string activeGroup = _familiarActiveBattleGroup;
        if (string.IsNullOrWhiteSpace(activeGroup))
        {
            return;
        }

        float now = Time.realtimeSinceStartup;
        if (!_deleteBattleGroupConfirmArmed || now > _deleteBattleGroupConfirmUntil)
        {
            _deleteBattleGroupConfirmArmed = true;
            _deleteBattleGroupConfirmUntil = now + BattleConfirmWindowSeconds;
            if (_deleteBattleGroupLabel != null)
            {
                _deleteBattleGroupLabel.text = "Confirm Delete Active Group";
                _deleteBattleGroupLabel.color = FamiliarToggleDisabledTextColor;
            }
            return;
        }

        _deleteBattleGroupConfirmArmed = false;
        if (_deleteBattleGroupLabel != null)
        {
            _deleteBattleGroupLabel.text = "Delete Active Group";
            _deleteBattleGroupLabel.color = Color.white;
        }

        Quips.SendCommand($".fam dbg {QuoteChatArgument(activeGroup)}");
    }

    private void UpdateFamiliarBattlesPanel()
    {
        if (_battlesStatusText != null)
        {
            if (!_familiarBattlesEnabled)
            {
                _battlesStatusText.text = "Familiar battles are disabled on this server.";
                _battlesStatusText.gameObject.SetActive(true);
            }
            else if (!_familiarBattleDataReady)
            {
                _battlesStatusText.text = "Loading battle groups...";
                _battlesStatusText.gameObject.SetActive(true);
            }
            else
            {
                _battlesStatusText.text = string.Empty;
                _battlesStatusText.gameObject.SetActive(false);
            }
        }

        if (_battleGroupSelectedText != null)
        {
            string label = string.IsNullOrWhiteSpace(_familiarActiveBattleGroup)
                ? (_familiarBattleGroups.Count > 0 ? _familiarBattleGroups[0].Name : "No groups")
                : _familiarActiveBattleGroup;
            _battleGroupSelectedText.text = label;
        }

        if (_battleGroupDropdownListRoot != null && _battleGroupDropdownListRoot.gameObject.activeSelf)
        {
            UpdateBattleGroupDropdownOptions();
        }

        UpdateBattleSlots();

        // Reset delete confirmation if it timed out
        if (_deleteBattleGroupConfirmArmed && Time.realtimeSinceStartup > _deleteBattleGroupConfirmUntil)
        {
            _deleteBattleGroupConfirmArmed = false;
            if (_deleteBattleGroupLabel != null)
            {
                _deleteBattleGroupLabel.text = "Delete Active Group";
                _deleteBattleGroupLabel.color = Color.white;
            }
        }
    }

    private void UpdateBattleSlots()
    {
        if (_battleSlotsListRoot == null)
        {
            return;
        }

        FamiliarBattleGroupData group = ResolveActiveBattleGroup();
        IReadOnlyList<FamiliarBattleSlotData> slots = (IReadOnlyList<FamiliarBattleSlotData>)group?.Slots ?? Array.Empty<FamiliarBattleSlotData>();

        EnsureBattleSlotRows(3, _referenceText);

        for (int i = 0; i < _battleSlotRows.Count; i++)
        {
            int slotNumber = i + 1;
            FamiliarBattleSlotData slot = i < slots.Count ? slots[i] : null;
            BattleSlotRow row = _battleSlotRows[i];

            bool isEmpty = slot == null || slot.Id == 0;
            string name = isEmpty ? $"Slot {slotNumber} — (empty)" : $"Slot {slotNumber} — {slot.Name}";
            string levelText = isEmpty ? string.Empty : $"Lv.{slot.Level}";

            if (row.NameText != null)
            {
                row.NameText.text = name;
                row.NameText.alpha = isEmpty ? 0.6f : 1f;
            }
            if (row.LevelText != null)
            {
                row.LevelText.text = levelText;
                row.LevelText.alpha = isEmpty ? 0.4f : 1f;
            }
            if (row.Icon != null)
            {
                if (isEmpty)
                {
                    row.Icon.enabled = false;
                }
                else
                {
                    ApplySprite(row.Icon, FamiliarBoxIconSpriteNames);
                    row.Icon.type = Image.Type.Simple;
                    row.Icon.preserveAspect = true;
                    row.Icon.color = new Color(1f, 1f, 1f, 0.9f);
                    row.Icon.enabled = row.Icon.sprite != null;
                }
            }

            if (row.Background != null)
            {
                row.Background.color = FamiliarActionBackgroundColor;
            }

            if (row.Button != null)
            {
                // Clicking a slot row assigns the active familiar into that slot.
                ConfigureActionButton(row.Button, () => Quips.SendCommand($".fam sbg {slotNumber.ToString(CultureInfo.InvariantCulture)}"), _familiarBattlesEnabled);
                if (row.Background != null)
                {
                    row.Background.raycastTarget = _familiarBattlesEnabled;
                }
            }
        }
    }

    private FamiliarBattleGroupData ResolveActiveBattleGroup()
    {
        if (_familiarBattleGroups.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(_familiarActiveBattleGroup))
        {
            FamiliarBattleGroupData match = _familiarBattleGroups
                .FirstOrDefault(bg => bg.Name.Equals(_familiarActiveBattleGroup, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                return match;
            }
        }

        return _familiarBattleGroups[0];
    }

    private void EnsureBattleSlotRows(int count, TextMeshProUGUI reference)
    {
        if (_battleSlotsListRoot == null || reference == null)
        {
            return;
        }

        while (_battleSlotRows.Count < count)
        {
            int slotNumber = _battleSlotRows.Count + 1;
            BattleSlotRow row = CreateBattleSlotRow(_battleSlotsListRoot, reference, slotNumber);
            if (row?.Root == null)
            {
                break;
            }
            _battleSlotRows.Add(row);
        }

        for (int i = 0; i < _battleSlotRows.Count; i++)
        {
            var row = _battleSlotRows[i];
            if (row?.Root != null)
            {
                row.Root.SetActive(i < count);
            }
        }
    }

    private static BattleSlotRow CreateBattleSlotRow(Transform parent, TextMeshProUGUI reference, int slotNumber)
    {
        RectTransform rectTransform = CreateRectTransformObject($"BattleSlotRow_{slotNumber}", parent);
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
        rowLayout.preferredHeight = BattleSlotRowHeight;
        rowLayout.minHeight = BattleSlotRowHeight;

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
        ConfigureActionButton(button, null, false);

        return new BattleSlotRow(rectTransform.gameObject, background, icon, nameText, levelText, button);
    }

    private sealed class BattleSlotRow
    {
        public GameObject Root { get; }
        public Image Background { get; }
        public Image Icon { get; }
        public TextMeshProUGUI NameText { get; }
        public TextMeshProUGUI LevelText { get; }
        public SimpleStunButton Button { get; }

        public BattleSlotRow(GameObject root, Image background, Image icon, TextMeshProUGUI nameText, TextMeshProUGUI levelText, SimpleStunButton button)
        {
            Root = root;
            Background = background;
            Icon = icon;
            NameText = nameText;
            LevelText = levelText;
            Button = button;
        }
    }
}


