using BloodUI.Services.CharacterMenu.Interfaces;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static BloodUI.Services.DataService;
using BloodUI.Services;

namespace BloodUI.Services.CharacterMenu;

/// <summary>
/// Orchestrates all character menu tabs, managing their lifecycle and updates.
/// </summary>
internal class CharacterMenuOrchestrator
{
    #region Constants

    private const string CharacterMenuName = "CharacterInventorySubMenu(Clone)";
    private const string CharacterMenuRootName = "CharacterMenu(Clone)";
    private const string CharacterMenuRootAltName = "CharacterMenu";
    private const string TabButtonsPath = "MotionRoot/TabButtons";
    private const string TabContentPath = "Scroll View/Viewport/AttributesTabContent";
    private const string AttributesTabButtonName = "AttributesTabButton";
    private const string BloodcraftTabButtonName = "BloodcraftTabButton";
    private const string BloodcraftTabName = "BloodcraftTab";

    #endregion

    #region Fields

    private readonly Dictionary<BloodcraftTab, ICharacterMenuTab> _tabs = new();
    private readonly List<ICharacterMenuTab> _tabOrder = [];

    private InventorySubMenu _inventorySubMenu;
    private RectTransform _bloodcraftTab;
    private Transform _contentRoot;
    private TextMeshProUGUI _entryStyle;

    private BloodcraftTab _activeTab = BloodcraftTab.Prestige;
    private int _bloodcraftTabIndex = -1;
    private int _lastKnownTabIndex = -1;
    private bool _initialized;
    private bool _manualActive;

    #endregion

    #region Properties

    /// <summary>
    /// Singleton instance of the CharacterMenuOrchestrator.
    /// </summary>
    public static CharacterMenuOrchestrator Instance { get; private set; }

    /// <summary>
    /// Whether the orchestrator has been initialized.
    /// </summary>
    public bool IsInitialized => _initialized;

    /// <summary>
    /// The currently active tab type.
    /// </summary>
    public BloodcraftTab ActiveTab => _activeTab;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new CharacterMenuOrchestrator.
    /// </summary>
    public CharacterMenuOrchestrator()
    {
        Instance = this;
    }

    #endregion

    #region Tab Registration

    /// <summary>
    /// Registers a tab with the orchestrator.
    /// </summary>
    public void RegisterTab(ICharacterMenuTab tab)
    {
        if (tab == null) return;

        _tabs[tab.TabType] = tab;
        _tabOrder.Add(tab);
        _tabOrder.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
    }

    /// <summary>
    /// Gets a tab by its type.
    /// </summary>
    public T GetTab<T>(BloodcraftTab tabType) where T : class, ICharacterMenuTab
    {
        return _tabs.TryGetValue(tabType, out var tab) ? tab as T : null;
    }

    /// <summary>
    /// Gets all registered tabs.
    /// </summary>
    public IEnumerable<ICharacterMenuTab> GetTabs()
    {
        return _tabOrder;
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Attempts to initialize the Bloodcraft tab in the Character menu.
    /// </summary>
    public void TryInitialize(InventorySubMenu menu)
    {
        if (menu == null || !IsCharacterMenu(menu))
        {
            return;
        }

        if (_initialized && _inventorySubMenu == menu)
        {
            return;
        }

        Reset();
        _inventorySubMenu = menu;

        Il2CppReferenceArray<RectTransform> tabs = menu.Tabs;
        Il2CppReferenceArray<SimpleStunButton> tabButtons = menu.TabButtons;

        if (tabs == null || tabButtons == null || tabs.Length == 0 || tabButtons.Length == 0)
        {
            DebugToolsBridge.TryLogWarning("[Bloodcraft Tab] Failed to read existing tabs.");
            return;
        }

        Transform tabButtonsRoot = menu.transform.Find(TabButtonsPath);
        SimpleStunButton templateButton = FindTemplateButton(tabButtonsRoot);

        if (templateButton == null)
        {
            DebugToolsBridge.TryLogWarning("[Bloodcraft Tab] Failed to locate tab button template.");
            return;
        }

        _bloodcraftTabIndex = tabs.Length;

        // Initialize all registered tabs
        foreach (var tab in _tabOrder)
        {
            try
            {
                if (_contentRoot != null && _entryStyle != null)
                {
                    tab.Initialize(_contentRoot, _entryStyle);
                }
            }
            catch (Exception e)
            {
                DebugToolsBridge.TryLogError($"Failed to initialize tab {tab.TabId}: {e}");
            }
        }

        _initialized = true;
        DebugToolsBridge.TryLogInfo("[Bloodcraft Tab] CharacterMenuOrchestrator initialized.");
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates the active tab's content.
    /// </summary>
    public void Update()
    {
        if (!_initialized || _inventorySubMenu == null || _bloodcraftTab == null)
        {
            return;
        }

        bool isActive = IsBloodcraftTabActive();
        _bloodcraftTab.gameObject.SetActive(isActive);

        if (!isActive) return;

        // Update the active tab
        if (_tabs.TryGetValue(_activeTab, out var activeTab))
        {
            try
            {
                activeTab.Update();
            }
            catch (Exception e)
            {
                DebugToolsBridge.TryLogError($"Error updating tab {activeTab.TabId}: {e}");
            }
        }
    }

    #endregion

    #region Tab Management

    /// <summary>
    /// Sets the active tab.
    /// </summary>
    public void SetActiveTab(BloodcraftTab tabType)
    {
        if (_activeTab != tabType && _tabs.TryGetValue(_activeTab, out var currentTab))
        {
            currentTab.OnDeactivated();
        }

        _activeTab = tabType;

        if (_tabs.TryGetValue(tabType, out var newTab))
        {
            newTab.OnActivated();
        }
    }

    /// <summary>
    /// Gets the labels for all registered tabs.
    /// </summary>
    public Dictionary<BloodcraftTab, string> GetTabLabels()
    {
        return _tabs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.TabLabel);
    }

    #endregion

    #region Reset

    /// <summary>
    /// Resets the orchestrator and all tabs.
    /// </summary>
    public void Reset()
    {
        foreach (var tab in _tabOrder)
        {
            try
            {
                tab.Reset();
            }
            catch (Exception e)
            {
                DebugToolsBridge.TryLogError($"Error resetting tab {tab.TabId}: {e}");
            }
        }

        _inventorySubMenu = null;
        _bloodcraftTab = null;
        _contentRoot = null;
        _entryStyle = null;
        _activeTab = BloodcraftTab.Prestige;
        _bloodcraftTabIndex = -1;
        _lastKnownTabIndex = -1;
        _initialized = false;
        _manualActive = false;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Checks if the given menu is a Character menu.
    /// </summary>
    public static bool IsCharacterMenu(InventorySubMenu menu)
    {
        if (menu == null) return false;

        string name = menu.gameObject.name;
        if (!name.Equals(CharacterMenuName)) return false;

        Transform parent = menu.transform.parent;
        if (parent == null) return false;

        string parentName = parent.gameObject.name;
        return parentName.Equals(CharacterMenuRootName) || parentName.Equals(CharacterMenuRootAltName);
    }

    private SimpleStunButton FindTemplateButton(Transform tabButtonsRoot)
    {
        if (tabButtonsRoot == null) return null;

        foreach (Transform child in tabButtonsRoot)
        {
            if (child.name == AttributesTabButtonName)
            {
                return child.GetComponent<SimpleStunButton>();
            }
        }

        return null;
    }

    private bool IsBloodcraftTabActive()
    {
        if (_inventorySubMenu == null) return false;

        int currentTab = _inventorySubMenu.CurrentTab;

        if (currentTab != _lastKnownTabIndex)
        {
            _lastKnownTabIndex = currentTab;

            if (currentTab != _bloodcraftTabIndex)
            {
                _manualActive = false;
            }
        }

        return currentTab == _bloodcraftTabIndex || _manualActive;
    }

    #endregion
}
