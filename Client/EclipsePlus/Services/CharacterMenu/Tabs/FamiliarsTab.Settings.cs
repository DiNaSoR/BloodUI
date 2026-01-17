using Eclipse.Services.CharacterMenu.Shared;
using Eclipse.Utilities;
using ProjectM.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Services.CharacterMenu.Tabs;

internal partial class FamiliarsTab
{
    private static readonly string[] FamiliarHeaderSettingsIconSpriteNames = ["Stunlock_Icon_NewStar", "IconBackground"];

    private TextMeshProUGUI _resetFamiliarLabel;
    private bool _resetFamiliarConfirmArmed;
    private float _resetFamiliarConfirmUntil;

    private void CreateFamiliarSettingsCard(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform card = CreateFamiliarCard(parent, "FamiliarSettingsCard", stretchHeight: false, enforceMinimumHeight: false);
        if (card == null)
        {
            return;
        }

        _ = CreateFamiliarSectionLabel(card, reference, "Shiny & Options", FamiliarHeaderSettingsIconSpriteNames);

        Transform listRoot = CreateFamiliarActionList(card);
        if (listRoot != null)
        {
            _toggleShinyLabel = CreateFamiliarActionRow(listRoot, reference, "Toggle Shiny Familiars", ".fam option shiny", FamiliarActionIconToggleSpriteNames, false);
            _toggleVBloodEmotesLabel = CreateFamiliarActionRow(listRoot, reference, "Toggle VBlood Emotes", ".fam option vbloodemotes", FamiliarActionIconEmoteSpriteNames, false);
        }

        _ = CreateFamiliarSubHeaderRow(card, reference, "Apply / Change Shiny Buff");
        CreateShinyBuffGrid(card, reference);

        _resetFamiliarLabel = CreateFamiliarActionRow(card, reference, "Reset Familiar State", ResetFamiliarMaybeConfirm, FamiliarActionIconUnbindSpriteNames, false);
    }

    private void CreateShinyBuffGrid(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform gridRoot = CreateRectTransformObject("ShinyBuffGrid", parent);
        if (gridRoot == null)
        {
            return;
        }
        gridRoot.anchorMin = new Vector2(0f, 1f);
        gridRoot.anchorMax = new Vector2(1f, 1f);
        gridRoot.pivot = new Vector2(0f, 1f);
        gridRoot.offsetMin = Vector2.zero;
        gridRoot.offsetMax = Vector2.zero;

        GridLayoutGroup grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.spacing = new Vector2(6f, 6f);
        grid.cellSize = new Vector2(92f, FamiliarActionRowHeight);
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.MiddleCenter;

        LayoutElement layout = gridRoot.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = (FamiliarActionRowHeight * 2f) + 6f;
        layout.preferredHeight = layout.minHeight;

        // Create 6 chip buttons (3x2)
        CreateShinyChip(gridRoot, reference, "Blood", "blood");
        CreateShinyChip(gridRoot, reference, "Storm", "storm");
        CreateShinyChip(gridRoot, reference, "Chaos", "chaos");
        CreateShinyChip(gridRoot, reference, "Frost", "frost");
        CreateShinyChip(gridRoot, reference, "Illusion", "illusion");
        CreateShinyChip(gridRoot, reference, "Unholy", "unholy");
    }

    private void CreateShinyChip(Transform parent, TextMeshProUGUI reference, string label, string spellSchool)
    {
        RectTransform rectTransform = CreateRectTransformObject($"ShinyChip_{label}", parent);
        if (rectTransform == null)
        {
            return;
        }
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image background = rectTransform.gameObject.AddComponent<Image>();
        ApplySprite(background, FamiliarRowSpriteNames);
        background.color = FamiliarActionBackgroundColor;
        background.raycastTarget = true;

        Outline outline = rectTransform.gameObject.AddComponent<Outline>();
        outline.effectColor = FamiliarModeTabBorderColor;
        outline.effectDistance = new Vector2(1f, -1f);

        SimpleStunButton button = rectTransform.gameObject.AddComponent<SimpleStunButton>();
        ConfigureCommandButton(button, $".fam shiny {spellSchool}", true);

        TextMeshProUGUI text = UIFactory.CreateSubTabLabel(rectTransform, reference, label, FamiliarModeTabFontScale) as TextMeshProUGUI;
        if (text != null)
        {
            text.color = FamiliarModeTabInactiveTextColor;
        }
    }

    private void ResetFamiliarMaybeConfirm()
    {
        float now = Time.realtimeSinceStartup;
        if (!_resetFamiliarConfirmArmed || now > _resetFamiliarConfirmUntil)
        {
            _resetFamiliarConfirmArmed = true;
            _resetFamiliarConfirmUntil = now + FamiliarConfirmWindowSeconds;
            if (_resetFamiliarLabel != null)
            {
                _resetFamiliarLabel.text = "Confirm Reset Familiar State";
                _resetFamiliarLabel.color = FamiliarToggleDisabledTextColor;
            }
            return;
        }

        _resetFamiliarConfirmArmed = false;
        if (_resetFamiliarLabel != null)
        {
            _resetFamiliarLabel.text = "Reset Familiar State";
            _resetFamiliarLabel.color = Color.white;
        }

        Quips.SendCommand(".fam reset");
    }

    private void UpdateSettingsConfirmations()
    {
        if (_resetFamiliarConfirmArmed && Time.realtimeSinceStartup > _resetFamiliarConfirmUntil)
        {
            _resetFamiliarConfirmArmed = false;
            if (_resetFamiliarLabel != null)
            {
                _resetFamiliarLabel.text = "Reset Familiar State";
                _resetFamiliarLabel.color = Color.white;
            }
        }
    }
}


