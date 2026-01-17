using Eclipse.Services.CharacterMenu.Base;
using Eclipse.Services.CharacterMenu.Interfaces;
using Eclipse.Services.CharacterMenu.Shared;
using Eclipse.Services.HUD.Shared;
using ProjectM.UI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Eclipse.Services.CanvasService.DataHUD;
using static Eclipse.Services.DataService;
using Eclipse.Services;

namespace Eclipse.Services.CharacterMenu.Tabs;

/// <summary>
/// Progression tab that contains sub-tabs for Professions and Class views.
/// </summary>
internal class ProgressionTab : CharacterMenuTabBase, ICharacterMenuTabWithPanel
{
    #region Constants

    private const float SubTabSpacing = 0f;
    private const float ContentSpacing = 8f;

    #endregion

    #region Nested Types

    private enum ProgressionSubTab
    {
        Professions,
        Class
    }

    #endregion

    #region Fields

    private RectTransform _panelRoot;
    private TextMeshProUGUI _referenceText;
    
    // Sub-tab navigation
    private Transform _subTabRoot;
    private readonly List<SimpleStunButton> _subTabButtons = [];
    private ProgressionSubTab _activeSubTab = ProgressionSubTab.Professions;

    // Content panels
    private Transform _professionsContent;
    private Transform _classContent;

    // Embedded tabs
    private readonly ProfessionsTab _professionsTab = new();
    private readonly ClassTab _classTab = new();

    // Sub-tab button labels
    private static readonly Dictionary<ProgressionSubTab, string> SubTabLabels = new()
    {
        { ProgressionSubTab.Professions, "Professions" },
        { ProgressionSubTab.Class, "Class" }
    };

    #endregion

    #region Properties

    public override string TabId => "Progression";
    public override string TabLabel => "Progression";
    public override string SectionTitle => "Progression";
    public override BloodcraftTab TabType => BloodcraftTab.Progression;

    /// <summary>
    /// Gets the embedded ProfessionsTab for external access if needed.
    /// </summary>
    public ProfessionsTab ProfessionsTabInstance => _professionsTab;

    /// <summary>
    /// Gets the embedded ClassTab for external access if needed.
    /// </summary>
    public ClassTab ClassTabInstance => _classTab;

    #endregion

    #region ICharacterMenuTabWithPanel

    public Transform CreatePanel(Transform parent, TextMeshProUGUI reference)
    {
        Reset();
        _referenceText = reference;

        RectTransform rectTransform = CreateRectTransformObject("BloodcraftProgression", parent);
        if (rectTransform == null)
        {
            return null;
        }

        UIFactory.ConfigureTopLeftAnchoring(rectTransform);
        EnsureVerticalLayout(rectTransform, spacing: ContentSpacing);

        // Create sub-tab navigation bar
        _subTabRoot = CreateSubTabBar(rectTransform, reference);

        // Create content containers
        Transform contentContainer = CreateContentContainer(rectTransform);

        // Create Professions content (using embedded tab)
        _professionsContent = _professionsTab.CreatePanel(contentContainer, reference);

        // Create Class content (using embedded tab)
        _classContent = _classTab.CreatePanel(contentContainer, reference);

        // Set initial visibility
        ApplySubTabVisibility();

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

        // Update sub-tab button states
        UpdateSubTabSelection();

        // Update the active content panel
        switch (_activeSubTab)
        {
            case ProgressionSubTab.Professions:
                _professionsTab.UpdatePanel();
                break;
            case ProgressionSubTab.Class:
                _classTab.UpdatePanel();
                break;
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
        _referenceText = null;
        _subTabRoot = null;
        _subTabButtons.Clear();
        _professionsContent = null;
        _classContent = null;
        _activeSubTab = ProgressionSubTab.Professions;

        _professionsTab.Reset();
        _classTab.Reset();
    }

    #endregion

    #region Private Methods - Layout

    private Transform CreateSubTabBar(Transform parent, TextMeshProUGUI reference)
    {
        RectTransform rectTransform = CreateRectTransformObject("ProgressionSubTabs", parent);
        if (rectTransform == null)
        {
            return null;
        }

        UIFactory.ConfigureTopLeftAnchoring(rectTransform);

        HorizontalLayoutGroup hLayout = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.spacing = SubTabSpacing;
        hLayout.childForceExpandWidth = true;
        hLayout.childForceExpandHeight = false;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 36f;
        layout.minHeight = 36f;

        // Create sub-tab buttons
        foreach (ProgressionSubTab subTab in Enum.GetValues(typeof(ProgressionSubTab)))
        {
            SimpleStunButton button = CreateSubTabButton(rectTransform, subTab, reference);
            if (button != null)
            {
                _subTabButtons.Add(button);
            }
        }

        return rectTransform;
    }

    private SimpleStunButton CreateSubTabButton(Transform parent, ProgressionSubTab subTab, TextMeshProUGUI reference)
    {
        RectTransform buttonRect = CreateRectTransformObject($"SubTab_{subTab}", parent);
        if (buttonRect == null || reference == null)
        {
            return null;
        }

        UIFactory.ConfigureTopLeftAnchoring(buttonRect);

        // Button background
        Image background = buttonRect.gameObject.AddComponent<Image>();
        Sprite tabSprite = ResolveSprite("Tab_Normal", "Window_Box_Background");
        if (tabSprite != null)
        {
            background.sprite = tabSprite;
            background.type = Image.Type.Sliced;
        }
        background.color = new Color(0.84f, 0.82f, 0.78f, 1f); // tab-color equivalent
        background.raycastTarget = true;

        // Button layout
        LayoutElement layoutElement = buttonRect.gameObject.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        layoutElement.preferredHeight = 36f;
        layoutElement.minHeight = 36f;

        // Content layout
        HorizontalLayoutGroup contentLayout = buttonRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.MiddleCenter;
        contentLayout.spacing = 8f;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.padding = UIFactory.CreatePadding(16, 16, 10, 10);

        // Icon
        RectTransform iconRect = CreateRectTransformObject("Icon", buttonRect);
        if (iconRect != null)
        {
            iconRect.sizeDelta = new Vector2(20f, 20f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            
            // Different icon based on sub-tab
            string iconSpriteName = subTab switch
            {
                ProgressionSubTab.Professions => "IconBackground",
                ProgressionSubTab.Class => "spell_icon",
                _ => "IconBackground"
            };
            
            Sprite iconSprite = ResolveSprite(iconSpriteName);
            if (iconSprite != null)
            {
                icon.sprite = iconSprite;
            }
            icon.color = new Color(1f, 1f, 1f, 0.9f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            LayoutElement iconLayout = iconRect.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 20f;
            iconLayout.minWidth = 20f;
            iconLayout.preferredHeight = 20f;
            iconLayout.minHeight = 20f;
        }

        // Label
        TextMeshProUGUI label = UIFactory.CreateTextElement(buttonRect, "Label", reference, 0.8f, FontStyles.Normal);
        if (label != null)
        {
            label.text = SubTabLabels.GetValueOrDefault(subTab, subTab.ToString());
            label.color = new Color(0.84f, 0.82f, 0.78f, 1f);
            label.alignment = TextAlignmentOptions.Center;
        }

        // Add button component  
        SimpleStunButton button = buttonRect.gameObject.AddComponent<SimpleStunButton>();
        
        // Wire up click handler
        int subTabIndex = (int)subTab;
        if (button != null)
        {
            button.onClick.AddListener((UnityEngine.Events.UnityAction)(() => OnSubTabClicked(subTabIndex)));
        }

        return button;
    }

    private static Transform CreateContentContainer(Transform parent)
    {
        RectTransform rectTransform = CreateRectTransformObject("ProgressionContent", parent);
        if (rectTransform == null)
        {
            return null;
        }

        UIFactory.ConfigureTopLeftAnchoring(rectTransform);

        LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.flexibleHeight = 1f;

        return rectTransform;
    }

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
            DebugToolsBridge.TryLogWarning($"[Progression Tab] Failed to configure vertical layout: {ex.Message}");
        }
    }

    #endregion

    #region Private Methods - Sub-tab Navigation

    private void OnSubTabClicked(int subTabIndex)
    {
        if (subTabIndex < 0 || subTabIndex >= Enum.GetValues(typeof(ProgressionSubTab)).Length)
        {
            return;
        }

        ProgressionSubTab newTab = (ProgressionSubTab)subTabIndex;
        if (newTab == _activeSubTab)
        {
            return;
        }

        _activeSubTab = newTab;
        ApplySubTabVisibility();
        UpdateSubTabSelection();

        DebugToolsBridge.TryLogInfo($"[Progression Tab] Switched to sub-tab: {_activeSubTab}");
    }

    private void ApplySubTabVisibility()
    {
        if (_professionsContent != null)
        {
            _professionsContent.gameObject.SetActive(_activeSubTab == ProgressionSubTab.Professions);
        }

        if (_classContent != null)
        {
            _classContent.gameObject.SetActive(_activeSubTab == ProgressionSubTab.Class);
        }
    }

    private void UpdateSubTabSelection()
    {
        Color activeColor = new(0.5f, 0.05f, 0.06f, 0.45f); // accent color
        Color normalColor = new(0.84f, 0.82f, 0.78f, 1f); // tab-color

        for (int i = 0; i < _subTabButtons.Count; i++)
        {
            SimpleStunButton button = _subTabButtons[i];
            if (button == null || button.gameObject == null)
            {
                continue;
            }

            bool isActive = i == (int)_activeSubTab;
            
            Image background = button.GetComponent<Image>();
            if (background != null)
            {
                // For active tab, apply accent overlay
                if (isActive)
                {
                    background.color = new Color(0.96f, 0.89f, 0.89f, 1f); // #f4e3e3
                }
                else
                {
                    background.color = normalColor;
                }
            }

            // Update label color
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.color = isActive 
                    ? new Color(0.96f, 0.89f, 0.89f, 1f) 
                    : new Color(0.84f, 0.82f, 0.78f, 1f);
            }
        }
    }

    #endregion

    #region Helpers

    private static RectTransform CreateRectTransformObject(string name, Transform parent)
        => UIFactory.CreateRectTransformObject(name, parent);

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

    #endregion
}
