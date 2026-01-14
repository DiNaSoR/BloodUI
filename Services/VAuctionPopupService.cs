using Eclipse.Services.CharacterMenu.Shared;
using Eclipse.Utilities;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM.UI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static Eclipse.Services.DataService;

namespace Eclipse.Services;

/// <summary>
/// Manages a floating VAuction popup window that can be toggled with F7.
/// Uses proper IL2CPP patterns for UI creation.
/// </summary>
internal static class VAuctionPopupService
{
    const string PopupName = "VAuctionPopup";

    static GameObject _popupRoot;
    static RectTransform _popupRect;
    static bool _isVisible;
    static bool _initialized;

    // Template references
    static TextMeshProUGUI _textReference;
    static SimpleStunButton _buttonTemplate;

    // UI elements
    static TextMeshProUGUI _headerText;
    static Transform _tabBar;
    static readonly List<SimpleStunButton> _tabButtons = [];
    static readonly string[] _tabNames = ["Browse", "My Listings", "My Bids", "Sell Item"];
    static int _activeTab = 0;

    // Browse tab
    static Transform _browseRoot;
    static Transform _categoryBar;
    static readonly List<SimpleStunButton> _categoryButtons = [];
    static readonly List<string> _categoryKeys = ["all", "weapons", "armor", "resources"];
    static Transform _pagerBar;
    static SimpleStunButton _prevButton;
    static SimpleStunButton _nextButton;
    static TextMeshProUGUI _pageLabel;
    static Transform _entriesRoot;
    static readonly List<TextMeshProUGUI> _entries = [];
    static readonly List<SimpleStunButton> _entryButtons = [];

    // Detail panel
    static Transform _detailRoot;
    static TextMeshProUGUI _detailText;
    static SimpleStunButton _minBidButton;
    static SimpleStunButton _buyNowButton;

    // Sell tab
    static Transform _sellRoot;
    static TextMeshProUGUI _selectedItemText;
    static int _selectedPrefabGuid;
    static int _selectedQuantity;
    static SimpleStunButton _listItemButton;

    /// <summary>
    /// Toggle the popup visibility (called from InputActionSystemPatch on F7).
    /// </summary>
    public static void TogglePopup()
    {
        if (!_initialized)
        {
            TryInitialize();
        }

        if (_popupRoot == null)
        {
            Core.Log.LogWarning("[VAuctionPopup] Failed to initialize popup.");
            return;
        }

        _isVisible = !_isVisible;
        _popupRoot.SetActive(_isVisible);

        if (_isVisible)
        {
            // Refresh auction data when opening
            Quips.SendCommand($".auction browse {VAuctionCategory} {Math.Max(1, VAuctionPage)}");
        }
    }

    /// <summary>
    /// Call this when an inventory slot is Ctrl+Clicked to select an item for selling.
    /// </summary>
    public static void SetSelectedItem(int prefabGuid, int quantity, string displayName)
    {
        _selectedPrefabGuid = prefabGuid;
        _selectedQuantity = quantity;

        if (_selectedItemText != null)
        {
            _selectedItemText.text = $"Selected: {displayName} x{quantity}";
        }
    }

    /// <summary>
    /// Update loop called from CanvasService.
    /// </summary>
    public static void Update()
    {
        if (!_initialized || !_isVisible || _popupRoot == null)
        {
            return;
        }

        RenderCurrentTab();
    }

    /// <summary>
    /// Reset state on scene change.
    /// </summary>
    public static void Reset()
    {
        if (_popupRoot != null)
        {
            UnityEngine.Object.Destroy(_popupRoot);
        }

        _popupRoot = null;
        _popupRect = null;
        _isVisible = false;
        _initialized = false;
        _activeTab = 0;

        _textReference = null;
        _buttonTemplate = null;
        _headerText = null;
        _tabBar = null;
        _tabButtons.Clear();

        _browseRoot = null;
        _categoryBar = null;
        _categoryButtons.Clear();
        _pagerBar = null;
        _prevButton = null;
        _nextButton = null;
        _pageLabel = null;
        _entriesRoot = null;
        _entries.Clear();
        _entryButtons.Clear();

        _detailRoot = null;
        _detailText = null;
        _minBidButton = null;
        _buyNowButton = null;

        _sellRoot = null;
        _selectedItemText = null;
        _selectedPrefabGuid = 0;
        _selectedQuantity = 0;
        _listItemButton = null;
    }

    static void TryInitialize()
    {
        if (_initialized) return;

        // Find template references first
        if (!FindTemplates())
        {
            Core.Log.LogWarning("[VAuctionPopup] Could not find UI templates.");
            return;
        }

        Transform popupParent = FindPopupParent();
        if (popupParent == null)
        {
            Core.Log.LogWarning("[VAuctionPopup] Could not find PopupParent.");
            return;
        }

        // Remove existing popup if any
        Transform existing = popupParent.Find(PopupName);
        if (existing != null)
        {
            UnityEngine.Object.Destroy(existing.gameObject);
        }

        // Create popup root using IL2CPP pattern
        _popupRoot = CreateGameObjectWithRect(PopupName, popupParent);
        _popupRoot.SetActive(false);

        _popupRect = _popupRoot.GetComponent<RectTransform>();
        ConfigurePopupRect();

        // Add background panel
        Image bgImage = _popupRoot.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
        bgImage.raycastTarget = true;

        // Add vertical layout for content
        VerticalLayoutGroup layout = _popupRoot.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = UIFactory.CreatePadding(16, 16, 16, 16);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Build UI
        CreateHeader();
        CreateTabBar();
        CreateBrowseTab();
        CreateSellTab();

        // Select first tab
        SwitchTab(0);

        _initialized = true;
        Core.Log.LogInfo("[VAuctionPopup] Popup initialized successfully.");
    }

    static bool FindTemplates()
    {
        // Find any existing TextMeshProUGUI for font reference
        TextMeshProUGUI[] allText = UnityEngine.Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (var tmp in allText)
        {
            if (tmp != null && tmp.font != null)
            {
                _textReference = tmp;
                break;
            }
        }

        // Find any existing button for template
        SimpleStunButton[] allButtons = UnityEngine.Resources.FindObjectsOfTypeAll<SimpleStunButton>();
        foreach (var btn in allButtons)
        {
            if (btn != null && btn.gameObject != null)
            {
                _buttonTemplate = btn;
                break;
            }
        }

        return _textReference != null;
    }

    static Transform FindPopupParent()
    {
        // Try to find MainMenuCanvas/Canvas/PopupParent
        foreach (Transform t in UnityEngine.Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t == null) continue;
            if (t.name.Equals("PopupParent", StringComparison.OrdinalIgnoreCase))
            {
                if (t.parent != null && t.parent.name.Contains("Canvas"))
                {
                    return t;
                }
            }
        }

        // Fallback: find any active Canvas
        Canvas[] canvases = UnityEngine.Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (Canvas c in canvases)
        {
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            if (c.name.Contains("HUD") || c.name.Contains("MainMenu"))
            {
                return c.transform;
            }
        }

        return null;
    }

    static GameObject CreateGameObjectWithRect(string name, Transform parent)
    {
        var components = new Il2CppReferenceArray<Il2CppSystem.Type>(1);
        components[0] = Il2CppType.Of<RectTransform>();
        GameObject obj = new(name, components);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject obj = CreateGameObjectWithRect(name, parent);
        return obj.GetComponent<RectTransform>();
    }

    static void ConfigurePopupRect()
    {
        // Center the popup, 650x550 size
        _popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        _popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        _popupRect.pivot = new Vector2(0.5f, 0.5f);
        _popupRect.sizeDelta = new Vector2(650f, 550f);
        _popupRect.anchoredPosition = Vector2.zero;
    }

    static void CreateHeader()
    {
        RectTransform header = CreateRect("Header", _popupRoot.transform);

        LayoutElement headerLE = header.gameObject.AddComponent<LayoutElement>();
        headerLE.preferredHeight = 45f;
        headerLE.minHeight = 45f;

        HorizontalLayoutGroup headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 8f;
        headerLayout.childControlWidth = true;
        headerLayout.childForceExpandWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandHeight = true;
        headerLayout.padding = UIFactory.CreatePadding(0, 0, 0, 0);

        // Title
        RectTransform titleRect = CreateRect("Title", header);
        _headerText = titleRect.gameObject.AddComponent<TextMeshProUGUI>();
        UIFactory.CopyTextStyle(_textReference, _headerText);
        _headerText.text = "V AUCTION";
        _headerText.fontSize = 26f;
        _headerText.fontStyle = FontStyles.Bold;
        _headerText.color = Color.white;
        _headerText.alignment = TextAlignmentOptions.Left;
        _headerText.raycastTarget = false;

        LayoutElement titleLE = titleRect.gameObject.AddComponent<LayoutElement>();
        titleLE.flexibleWidth = 1f;

        // Close button
        RectTransform closeRect = CreateRect("CloseButton", header);
        Image closeImg = closeRect.gameObject.AddComponent<Image>();
        closeImg.color = new Color(0.75f, 0.2f, 0.2f, 0.95f);

        SimpleStunButton closeBtn = closeRect.gameObject.AddComponent<SimpleStunButton>();
        closeBtn.onClick.AddListener((UnityAction)(() => { _isVisible = false; _popupRoot?.SetActive(false); }));

        LayoutElement closeLE = closeRect.gameObject.AddComponent<LayoutElement>();
        closeLE.preferredWidth = 45f;
        closeLE.preferredHeight = 45f;
        closeLE.minWidth = 45f;

        RectTransform closeTextRect = CreateRect("CloseText", closeRect);
        TextMeshProUGUI closeText = closeTextRect.gameObject.AddComponent<TextMeshProUGUI>();
        UIFactory.CopyTextStyle(_textReference, closeText);
        closeText.text = "X";
        closeText.fontSize = 22f;
        closeText.fontStyle = FontStyles.Bold;
        closeText.color = Color.white;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.raycastTarget = false;

        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.sizeDelta = Vector2.zero;
    }

    static void CreateTabBar()
    {
        RectTransform tabBar = CreateRect("TabBar", _popupRoot.transform);
        _tabBar = tabBar;

        LayoutElement tabBarLE = tabBar.gameObject.AddComponent<LayoutElement>();
        tabBarLE.preferredHeight = 40f;
        tabBarLE.minHeight = 40f;

        HorizontalLayoutGroup tabLayout = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 4f;
        tabLayout.childControlWidth = true;
        tabLayout.childForceExpandWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandHeight = true;

        _tabButtons.Clear();
        for (int i = 0; i < _tabNames.Length; i++)
        {
            int tabIndex = i;
            SimpleStunButton btn = CreateButton(_tabBar, _tabNames[i], () => SwitchTab(tabIndex));
            _tabButtons.Add(btn);
        }
    }

    static SimpleStunButton CreateButton(Transform parent, string label, Action onClick, float preferredWidth = -1f)
    {
        RectTransform btnRect = CreateRect($"Btn_{label}", parent);

        Image btnImg = btnRect.gameObject.AddComponent<Image>();
        btnImg.color = new Color(0.22f, 0.22f, 0.28f, 0.95f);

        SimpleStunButton btn = btnRect.gameObject.AddComponent<SimpleStunButton>();
        btn.onClick.AddListener((UnityAction)(() => onClick?.Invoke()));

        if (preferredWidth > 0)
        {
            LayoutElement btnLE = btnRect.gameObject.AddComponent<LayoutElement>();
            btnLE.preferredWidth = preferredWidth;
            btnLE.minWidth = preferredWidth;
        }

        RectTransform textRect = CreateRect("Text", btnRect);
        TextMeshProUGUI text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        UIFactory.CopyTextStyle(_textReference, text);
        text.text = label;
        text.fontSize = 15f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return btn;
    }

    static void SwitchTab(int tabIndex)
    {
        _activeTab = tabIndex;

        // Update tab button visuals
        for (int i = 0; i < _tabButtons.Count; i++)
        {
            if (_tabButtons[i] == null) continue;
            Image img = _tabButtons[i].GetComponent<Image>();
            if (img != null)
            {
                img.color = i == _activeTab
                    ? new Color(0.35f, 0.5f, 0.7f, 0.98f)
                    : new Color(0.22f, 0.22f, 0.28f, 0.95f);
            }
        }

        // Show/hide content
        if (_browseRoot != null) _browseRoot.gameObject.SetActive(_activeTab == 0 || _activeTab == 1 || _activeTab == 2);
        if (_sellRoot != null) _sellRoot.gameObject.SetActive(_activeTab == 3);
    }

    static void CreateBrowseTab()
    {
        RectTransform browse = CreateRect("BrowseTab", _popupRoot.transform);
        _browseRoot = browse;

        VerticalLayoutGroup browseLayout = browse.gameObject.AddComponent<VerticalLayoutGroup>();
        browseLayout.spacing = 6f;
        browseLayout.childControlWidth = true;
        browseLayout.childControlHeight = false;
        browseLayout.childForceExpandWidth = true;
        browseLayout.childForceExpandHeight = false;

        LayoutElement browseLE = browse.gameObject.AddComponent<LayoutElement>();
        browseLE.flexibleHeight = 1f;

        CreateCategoryBar();
        CreateListingsArea();
        CreatePagerBar();
        CreateDetailPanel();
    }

    static void CreateCategoryBar()
    {
        RectTransform catBar = CreateRect("CategoryBar", _browseRoot);
        _categoryBar = catBar;

        LayoutElement catBarLE = catBar.gameObject.AddComponent<LayoutElement>();
        catBarLE.preferredHeight = 32f;
        catBarLE.minHeight = 32f;

        HorizontalLayoutGroup catLayout = catBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        catLayout.spacing = 4f;
        catLayout.childControlWidth = true;
        catLayout.childForceExpandWidth = true;
        catLayout.childControlHeight = true;
        catLayout.childForceExpandHeight = true;

        _categoryButtons.Clear();
        foreach (string cat in _categoryKeys)
        {
            string captured = cat;
            SimpleStunButton btn = CreateButton(_categoryBar, cat.ToUpperInvariant(), () =>
            {
                Quips.SendCommand($".auction browse {captured} 1");
            });
            _categoryButtons.Add(btn);
        }
    }

    static void CreateListingsArea()
    {
        RectTransform listings = CreateRect("Listings", _browseRoot);
        _entriesRoot = listings;

        VerticalLayoutGroup listLayout = listings.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 3f;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = false;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;

        LayoutElement listLE = listings.gameObject.AddComponent<LayoutElement>();
        listLE.flexibleHeight = 1f;
        listLE.preferredHeight = 180f;
        listLE.minHeight = 100f;

        // Pre-create some entry slots
        for (int i = 0; i < 8; i++)
        {
            CreateEntrySlot(i);
        }
    }

    static void CreateEntrySlot(int index)
    {
        RectTransform entry = CreateRect($"Entry_{index}", _entriesRoot);

        Image entryBg = entry.gameObject.AddComponent<Image>();
        entryBg.color = new Color(0.18f, 0.18f, 0.22f, 0.85f);

        SimpleStunButton btn = entry.gameObject.AddComponent<SimpleStunButton>();

        LayoutElement entryLE = entry.gameObject.AddComponent<LayoutElement>();
        entryLE.preferredHeight = 26f;
        entryLE.minHeight = 26f;

        RectTransform textRect = CreateRect("Text", entry);
        TextMeshProUGUI text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        UIFactory.CopyTextStyle(_textReference, text);
        text.text = "";
        text.fontSize = 13f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 0f);
        textRect.offsetMax = new Vector2(-10f, 0f);

        entry.gameObject.SetActive(false);
        _entries.Add(text);
        _entryButtons.Add(btn);
    }

    static void CreatePagerBar()
    {
        RectTransform pager = CreateRect("PagerBar", _browseRoot);
        _pagerBar = pager;

        LayoutElement pagerLE = pager.gameObject.AddComponent<LayoutElement>();
        pagerLE.preferredHeight = 32f;
        pagerLE.minHeight = 32f;

        HorizontalLayoutGroup pagerLayout = pager.gameObject.AddComponent<HorizontalLayoutGroup>();
        pagerLayout.spacing = 8f;
        pagerLayout.childControlWidth = false;
        pagerLayout.childForceExpandWidth = false;
        pagerLayout.childControlHeight = true;
        pagerLayout.childForceExpandHeight = true;
        pagerLayout.childAlignment = TextAnchor.MiddleCenter;

        _prevButton = CreateButton(_pagerBar, "< Prev", () =>
        {
            int prev = Math.Max(1, VAuctionPage - 1);
            Quips.SendCommand($".auction browse {VAuctionCategory} {prev}");
        }, 90f);

        // Page label
        RectTransform labelRect = CreateRect("PageLabel", _pagerBar);
        _pageLabel = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        UIFactory.CopyTextStyle(_textReference, _pageLabel);
        _pageLabel.text = "Page 1/1";
        _pageLabel.fontSize = 14f;
        _pageLabel.color = Color.white;
        _pageLabel.alignment = TextAlignmentOptions.Center;
        _pageLabel.raycastTarget = false;

        LayoutElement labelLE = labelRect.gameObject.AddComponent<LayoutElement>();
        labelLE.flexibleWidth = 1f;

        _nextButton = CreateButton(_pagerBar, "Next >", () =>
        {
            int next = Math.Min(Math.Max(1, VAuctionPageCount), VAuctionPage + 1);
            Quips.SendCommand($".auction browse {VAuctionCategory} {next}");
        }, 90f);
    }

    static void CreateDetailPanel()
    {
        RectTransform detail = CreateRect("DetailPanel", _browseRoot);
        _detailRoot = detail;

        VerticalLayoutGroup detailLayout = detail.gameObject.AddComponent<VerticalLayoutGroup>();
        detailLayout.spacing = 4f;
        detailLayout.childControlWidth = true;
        detailLayout.childControlHeight = false;
        detailLayout.childForceExpandWidth = true;
        detailLayout.childForceExpandHeight = false;

        LayoutElement detailLE = detail.gameObject.AddComponent<LayoutElement>();
        detailLE.preferredHeight = 100f;

        // Detail text
        RectTransform textRect = CreateRect("DetailText", _detailRoot);
        _detailText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        UIFactory.CopyTextStyle(_textReference, _detailText);
        _detailText.text = "Select a listing to view details.";
        _detailText.fontSize = 13f;
        _detailText.color = new Color(0.8f, 0.8f, 0.8f);
        _detailText.raycastTarget = false;

        LayoutElement textLE = textRect.gameObject.AddComponent<LayoutElement>();
        textLE.preferredHeight = 60f;

        // Action buttons row
        RectTransform actions = CreateRect("Actions", _detailRoot);
        HorizontalLayoutGroup actionsLayout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
        actionsLayout.spacing = 8f;
        actionsLayout.childControlWidth = true;
        actionsLayout.childForceExpandWidth = true;
        actionsLayout.childControlHeight = true;
        actionsLayout.childForceExpandHeight = true;

        LayoutElement actionsLE = actions.gameObject.AddComponent<LayoutElement>();
        actionsLE.preferredHeight = 32f;

        _minBidButton = CreateButton(actions, "Place Min Bid", () =>
        {
            if (!VAuctionSelectedDetail.HasValue) return;
            var d = VAuctionSelectedDetail.Value;
            if (string.IsNullOrWhiteSpace(d.AuctionKey)) return;
            Quips.SendCommand($".auction bid {d.AuctionKey} {d.MinNextBid}");
        });

        _buyNowButton = CreateButton(actions, "Buy Now", () =>
        {
            if (!VAuctionSelectedDetail.HasValue) return;
            var d = VAuctionSelectedDetail.Value;
            if (string.IsNullOrWhiteSpace(d.AuctionKey)) return;
            Quips.SendCommand($".auction buy {d.AuctionKey}");
        });
    }

    static void CreateSellTab()
    {
        RectTransform sell = CreateRect("SellTab", _popupRoot.transform);
        _sellRoot = sell;
        sell.gameObject.SetActive(false);

        VerticalLayoutGroup sellLayout = sell.gameObject.AddComponent<VerticalLayoutGroup>();
        sellLayout.spacing = 10f;
        sellLayout.childControlWidth = true;
        sellLayout.childControlHeight = false;
        sellLayout.childForceExpandWidth = true;
        sellLayout.childForceExpandHeight = false;

        LayoutElement sellLE = sell.gameObject.AddComponent<LayoutElement>();
        sellLE.flexibleHeight = 1f;

        // Instructions
        RectTransform instrRect = CreateRect("Instructions", _sellRoot);
        TextMeshProUGUI instrText = instrRect.gameObject.AddComponent<TextMeshProUGUI>();
        UIFactory.CopyTextStyle(_textReference, instrText);
        instrText.text = "Ctrl+Click an inventory item to select it for selling.";
        instrText.fontSize = 15f;
        instrText.color = new Color(0.7f, 0.7f, 0.7f);
        instrText.raycastTarget = false;

        LayoutElement instrLE = instrRect.gameObject.AddComponent<LayoutElement>();
        instrLE.preferredHeight = 30f;

        // Selected item display
        RectTransform selectedRect = CreateRect("SelectedItem", _sellRoot);
        _selectedItemText = selectedRect.gameObject.AddComponent<TextMeshProUGUI>();
        UIFactory.CopyTextStyle(_textReference, _selectedItemText);
        _selectedItemText.text = "No item selected.";
        _selectedItemText.fontSize = 18f;
        _selectedItemText.fontStyle = FontStyles.Bold;
        _selectedItemText.color = new Color(1f, 0.9f, 0.4f);
        _selectedItemText.raycastTarget = false;

        LayoutElement selectedLE = selectedRect.gameObject.AddComponent<LayoutElement>();
        selectedLE.preferredHeight = 35f;

        // Spacer
        RectTransform spacer = CreateRect("Spacer", _sellRoot);
        LayoutElement spacerLE = spacer.gameObject.AddComponent<LayoutElement>();
        spacerLE.flexibleHeight = 1f;

        // List Item button
        RectTransform listBtnRect = CreateRect("ListItemButton", _sellRoot);

        Image listBtnImg = listBtnRect.gameObject.AddComponent<Image>();
        listBtnImg.color = new Color(0.2f, 0.6f, 0.2f, 0.95f);

        _listItemButton = listBtnRect.gameObject.AddComponent<SimpleStunButton>();
        _listItemButton.onClick.AddListener((UnityAction)OnListItemClicked);

        LayoutElement listBtnLE = listBtnRect.gameObject.AddComponent<LayoutElement>();
        listBtnLE.preferredHeight = 45f;
        listBtnLE.minHeight = 45f;

        RectTransform listBtnTextRect = CreateRect("Text", listBtnRect);
        TextMeshProUGUI listBtnText = listBtnTextRect.gameObject.AddComponent<TextMeshProUGUI>();
        UIFactory.CopyTextStyle(_textReference, listBtnText);
        listBtnText.text = "LIST ITEM";
        listBtnText.fontSize = 20f;
        listBtnText.fontStyle = FontStyles.Bold;
        listBtnText.color = Color.white;
        listBtnText.alignment = TextAlignmentOptions.Center;
        listBtnText.raycastTarget = false;

        listBtnTextRect.anchorMin = Vector2.zero;
        listBtnTextRect.anchorMax = Vector2.one;
        listBtnTextRect.sizeDelta = Vector2.zero;
    }

    static void OnListItemClicked()
    {
        if (_selectedPrefabGuid == 0)
        {
            Core.Log.LogWarning("[VAuctionPopup] No item selected to list.");
            return;
        }

        int quantity = _selectedQuantity > 0 ? _selectedQuantity : 1;
        int startBid = 100;
        int buyNow = 0;
        int hours = 24;

        string command = $".auction sellitem {_selectedPrefabGuid} {quantity} {startBid} {buyNow} {hours}";
        Quips.SendCommand(command);

        // Reset selection after listing
        _selectedPrefabGuid = 0;
        _selectedQuantity = 0;
        if (_selectedItemText != null)
        {
            _selectedItemText.text = "No item selected.";
        }
    }

    static void RenderCurrentTab()
    {
        if (_activeTab >= 0 && _activeTab <= 2)
        {
            RenderBrowseTab();
        }
    }

    static void RenderBrowseTab()
    {
        // Update category button visuals
        for (int i = 0; i < _categoryButtons.Count && i < _categoryKeys.Count; i++)
        {
            if (_categoryButtons[i] == null) continue;
            bool isActive = string.Equals(VAuctionCategory, _categoryKeys[i], StringComparison.OrdinalIgnoreCase);
            Image img = _categoryButtons[i].GetComponent<Image>();
            if (img != null)
            {
                img.color = isActive
                    ? new Color(0.35f, 0.5f, 0.7f, 0.98f)
                    : new Color(0.22f, 0.22f, 0.28f, 0.95f);
            }
        }

        // Update page label
        if (_pageLabel != null)
        {
            int pc = Math.Max(1, VAuctionPageCount);
            int p = Math.Clamp(VAuctionPage, 1, pc);
            _pageLabel.text = $"Page {p}/{pc} ({VAuctionTotal} items)";
        }

        // Update entries
        int listingCount = VAuctionListings.Count;
        for (int i = 0; i < _entries.Count; i++)
        {
            bool hasData = i < listingCount;
            if (_entries[i] == null) continue;

            _entries[i].transform.parent.gameObject.SetActive(hasData);

            if (hasData)
            {
                VAuctionListingSummary s = VAuctionListings[i];
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long remaining = Math.Max(0, s.ExpiresUnix - now);
                long mins = remaining / 60;

                string buyStr = s.BuyNow > 0 ? $" | Buy: {s.BuyNow}" : "";
                _entries[i].text = $"{s.ItemName} x{s.Quantity} | Bid: {s.CurrentBid}{buyStr} | {mins}m left";

                if (i < _entryButtons.Count && _entryButtons[i] != null)
                {
                    _entryButtons[i].onClick.RemoveAllListeners();
                    string key = s.AuctionKey;
                    _entryButtons[i].onClick.AddListener((UnityAction)(() =>
                    {
                        Quips.SendCommand($".auction view {key}");
                    }));
                }
            }
        }

        // Update detail panel
        if (_detailText != null)
        {
            if (!VAuctionSelectedDetail.HasValue)
            {
                _detailText.text = "Select a listing to view details.";
            }
            else
            {
                VAuctionListingDetail d = VAuctionSelectedDetail.Value;
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long remaining = Math.Max(0, d.ExpiresUnix - now);
                long mins = remaining / 60;

                _detailText.text =
                    $"{d.ItemName} x{d.Quantity}\n" +
                    $"Seller: {d.SellerName}\n" +
                    $"Start: {d.StartingBid} | Current: {d.CurrentBid} | Buy: {d.BuyNow}\n" +
                    $"Min next bid: {d.MinNextBid} | Time left: {mins}m";
            }
        }

        // Show/hide action buttons
        bool canBid = VAuctionSelectedDetail.HasValue && VAuctionSelectedDetail.Value.MinNextBid > 0;
        if (_minBidButton != null)
        {
            _minBidButton.gameObject.SetActive(canBid);
        }
        if (_buyNowButton != null)
        {
            bool canBuy = VAuctionSelectedDetail.HasValue && VAuctionSelectedDetail.Value.BuyNow > 0;
            _buyNowButton.gameObject.SetActive(canBuy);
        }
    }
}
