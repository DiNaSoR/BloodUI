using Eclipse.Utilities;
using ProjectM.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Eclipse.Services.DataService;

namespace Eclipse.Services.CharacterMenu.Tabs;

internal partial class FamiliarsTab
{
    private Transform _overflowListRoot;
    private readonly List<OverflowRow> _overflowRows = [];
    private int _selectedOverflowIndex = -1;
    private float _overflowLastRefreshTime = -1000f;

    private void CreateOverflowSection(Transform parent, TextMeshProUGUI reference)
    {
        if (parent == null || reference == null)
        {
            return;
        }

        _ = CreateFamiliarSubHeaderRow(parent, reference, "Overflow");

        _overflowListRoot = CreateRectTransformObject("FamiliarOverflowList", parent);
        if (_overflowListRoot == null)
        {
            return;
        }

        VerticalLayoutGroup layout = _overflowListRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = FamiliarActionSpacing;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        Transform actionsRoot = CreateFamiliarActionList(parent);
        if (actionsRoot != null)
        {
            _ = CreateFamiliarActionRow(actionsRoot, reference, "Move Selected Overflow → Destination", MoveSelectedOverflowToDestination, FamiliarActionIconOverflowSpriteNames, false);
            _ = CreateFamiliarActionRow(actionsRoot, reference, "Refresh Overflow", () => Quips.SendCommand(".fam of"), FamiliarActionIconSearchSpriteNames, false);
        }
    }

    private void UpdateOverflowPanel()
    {
        if (_overflowListRoot == null || _referenceText == null)
        {
            return;
        }

        // Opportunistic refresh if we haven't loaded overflow yet.
        if (!_familiarOverflowDataReady)
        {
            float now = Time.realtimeSinceStartup;
            if (now - _overflowLastRefreshTime > FamiliarBoxRefreshCooldownSeconds)
            {
                _overflowLastRefreshTime = now;
                Quips.SendCommand(".fam of");
            }
        }

        int entryCount = _familiarOverflowEntries.Count;
        if (_selectedOverflowIndex > entryCount)
        {
            _selectedOverflowIndex = -1;
        }

        int rowCount = Math.Max(1, entryCount);
        EnsureOverflowRows(rowCount, _referenceText);

        if (entryCount == 0)
        {
            OverflowRow row = _overflowRows[0];
            ApplyOverflowRow(
                row,
                overflowIndex: 0,
                name: _familiarOverflowDataReady ? "Overflow storage is empty." : "Loading overflow...",
                levelText: string.Empty,
                isSelected: false,
                isPlaceholder: true,
                isShiny: false);
            return;
        }

        for (int i = 0; i < _overflowRows.Count; i++)
        {
            if (i >= entryCount)
            {
                _overflowRows[i].Root.SetActive(false);
                continue;
            }

            FamiliarOverflowEntryData entry = _familiarOverflowEntries[i];
            bool isSelected = entry != null && entry.Index == _selectedOverflowIndex;
            string name = entry != null ? entry.Name : string.Empty;
            string levelText = entry != null && entry.Level > 0 ? $"Lv.{entry.Level.ToString(CultureInfo.InvariantCulture)}" : string.Empty;

            ApplyOverflowRow(
                _overflowRows[i],
                overflowIndex: entry?.Index ?? 0,
                name: name,
                levelText: levelText,
                isSelected: isSelected,
                isPlaceholder: false,
                isShiny: entry?.IsShiny ?? false);
        }
    }

    private void MoveSelectedOverflowToDestination()
    {
        if (_selectedOverflowIndex < 1)
        {
            return;
        }

        string destination = ResolveDestinationBoxName();
        if (string.IsNullOrWhiteSpace(destination))
        {
            return;
        }

        Quips.SendCommand($".fam om {_selectedOverflowIndex.ToString(CultureInfo.InvariantCulture)} {QuoteChatArgument(destination)}");
        Quips.SendCommand(".fam of");
        Quips.SendCommand(".fam l");
    }

    private void EnsureOverflowRows(int count, TextMeshProUGUI reference)
    {
        if (_overflowListRoot == null || reference == null)
        {
            return;
        }

        while (_overflowRows.Count < count)
        {
            int rowIndex = _overflowRows.Count + 1;
            OverflowRow row = CreateOverflowRow(_overflowListRoot, reference, rowIndex);
            if (row?.Root == null)
            {
                break;
            }
            _overflowRows.Add(row);
        }

        for (int i = 0; i < _overflowRows.Count; i++)
        {
            _overflowRows[i].Root.SetActive(i < count);
        }
    }

    private OverflowRow CreateOverflowRow(Transform parent, TextMeshProUGUI reference, int rowIndex)
    {
        RectTransform rectTransform = CreateRectTransformObject($"OverflowRow_{rowIndex}", parent);
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
        ConfigureActionButton(button, null, false);

        return new OverflowRow(rectTransform.gameObject, background, icon, nameText, levelText, button);
    }

    private void ApplyOverflowRow(
        OverflowRow row,
        int overflowIndex,
        string name,
        string levelText,
        bool isSelected,
        bool isPlaceholder,
        bool isShiny)
    {
        if (row == null)
        {
            return;
        }

        if (row.NameText != null)
        {
            row.NameText.text = name ?? string.Empty;
            row.NameText.alpha = isPlaceholder ? 0.65f : 1f;

            if (!isPlaceholder && isShiny)
            {
                float t = 0.5f + (0.5f * Mathf.Sin((Time.realtimeSinceStartup * FamiliarShinyPulseSpeed) + (overflowIndex * 0.35f)));
                row.NameText.color = Color.Lerp(FamiliarShinyNameColorA, FamiliarShinyNameColorB, t);
            }
            else
            {
                row.NameText.color = FamiliarBoxRowTextColor;
            }
        }

        if (row.LevelText != null)
        {
            row.LevelText.text = levelText ?? string.Empty;
            row.LevelText.alpha = isPlaceholder ? 0.4f : 1f;
        }

        if (row.Icon != null)
        {
            row.Icon.enabled = !isPlaceholder && row.Icon.sprite != null;
        }

        if (row.Background != null)
        {
            row.Background.color = isSelected ? FamiliarPrimaryActionBackgroundColor : FamiliarActionBackgroundColor;
            row.Background.raycastTarget = !isPlaceholder;
        }

        if (row.Button != null)
        {
            bool enabled = !isPlaceholder && overflowIndex > 0;
            ConfigureActionButton(row.Button, enabled ? () => SelectOverflowIndex(overflowIndex) : null, enabled);
        }
    }

    private void SelectOverflowIndex(int overflowIndex)
    {
        if (overflowIndex < 1)
        {
            return;
        }

        _selectedOverflowIndex = overflowIndex;
        UpdateOverflowPanel();
    }

    private sealed class OverflowRow
    {
        public GameObject Root { get; }
        public Image Background { get; }
        public Image Icon { get; }
        public TextMeshProUGUI NameText { get; }
        public TextMeshProUGUI LevelText { get; }
        public SimpleStunButton Button { get; }

        public OverflowRow(GameObject root, Image background, Image icon, TextMeshProUGUI nameText, TextMeshProUGUI levelText, SimpleStunButton button)
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


