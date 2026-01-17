using BepInEx;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Services;

internal static class LayoutService
{
    const string WindowTitle = "EclipsePLUS Layout";
    const string LayoutFileName = "layout.json";

    static readonly string LayoutPath = Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.{LayoutFileName}");
    static readonly Dictionary<string, LayoutElement> Elements = new();
    static Dictionary<string, LayoutEntry> SavedLayouts = new();
    static Dictionary<string, LayoutEntry> DefaultLayouts = new();
    static LayoutOptions Options = new();

    static LayoutModeBehaviour _behaviour;

    internal static LayoutOptions CurrentOptions => Options;
    internal static bool IsLayoutModeActive => _behaviour != null && _behaviour.IsActive;

    public static void ApplyLayoutsForInput(bool isGamepad)
    {
        ApplyAllLayouts();
    }

    public static void Initialize()
    {
        if (_behaviour != null)
            return;

        LoadLayout();

        if (!ClassInjector.IsTypeRegisteredInIl2Cpp(typeof(LayoutModeBehaviour)))
            ClassInjector.RegisterTypeInIl2Cpp<LayoutModeBehaviour>();

        var go = new GameObject("EclipseLayoutMode");
        UnityEngine.Object.DontDestroyOnLoad(go);
        _behaviour = go.AddComponent<LayoutModeBehaviour>();
    }

    public static void RegisterElement(string key, RectTransform rect)
    {
        if (string.IsNullOrWhiteSpace(key) || rect == null)
            return;

        Initialize();

        var element = new LayoutElement(key, rect);
        Elements[key] = element;

        if (!DefaultLayouts.ContainsKey(key))
            DefaultLayouts[key] = LayoutEntry.FromRect(rect);

        ApplySavedLayout(key, rect);
        DebugToolsBridge.TryLogInfo($"[Layout] Registered: {key}");
    }

    public static void Reset()
    {
        Elements.Clear();
        _behaviour?.ResetState();
    }

    static void SaveLayout()
    {
        CacheLayouts();
        var config = new LayoutConfig
        {
            Layouts = SavedLayouts,
            Defaults = DefaultLayouts,
            Options = Options
        };

        try
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(LayoutPath, json);
            DebugToolsBridge.TryLogInfo($"[Layout] Saved layout to: {LayoutPath}");
        }
        catch (Exception ex)
        {
            DebugToolsBridge.TryLogWarning($"[Layout] Failed to save: {ex.Message}");
        }
    }

    static void LoadLayout()
    {
        if (!File.Exists(LayoutPath))
            return;

        try
        {
            string json = File.ReadAllText(LayoutPath);
            var config = JsonSerializer.Deserialize<LayoutConfig>(json);
            if (config != null)
            {
                SavedLayouts = config.Layouts ?? new();
                if (config.Defaults != null && config.Defaults.Count > 0)
                    DefaultLayouts = config.Defaults;
                Options = config.Options ?? new();
                ApplyAllLayouts();
                DebugToolsBridge.TryLogInfo($"[Layout] Loaded layout from: {LayoutPath}");
            }
        }
        catch (Exception ex)
        {
            DebugToolsBridge.TryLogWarning($"[Layout] Failed to load: {ex.Message}");
        }
    }

    static void DeleteLayout()
    {
        if (File.Exists(LayoutPath))
        {
            File.Delete(LayoutPath);
            SavedLayouts.Clear();
            DebugToolsBridge.TryLogInfo($"[Layout] Deleted layout file");
        }
    }

    static void ResetToDefaults()
    {
        SavedLayouts.Clear();
        foreach (var kvp in Elements)
        {
            if (kvp.Value.Rect == null)
                continue;

            if (DefaultLayouts.TryGetValue(kvp.Key, out var defaultEntry))
                ApplyLayoutEntry(kvp.Value.Rect, defaultEntry);
        }
        DebugToolsBridge.TryLogInfo($"[Layout] Reset to defaults");
    }

    static void ApplyAllLayouts()
    {
        foreach (var element in Elements.Values)
        {
            if (element.Rect == null)
                continue;

            if (SavedLayouts.TryGetValue(element.Key, out var entry))
                ApplyLayoutEntry(element.Rect, entry);
        }
    }

    static void ApplySavedLayout(string key, RectTransform rect)
    {
        if (SavedLayouts.TryGetValue(key, out var entry))
            ApplyLayoutEntry(rect, entry);
    }

    static void ApplyLayoutEntry(RectTransform rect, LayoutEntry entry)
    {
        rect.anchorMin = new Vector2(entry.AnchorMinX, entry.AnchorMinY);
        rect.anchorMax = new Vector2(entry.AnchorMaxX, entry.AnchorMaxY);
        rect.pivot = new Vector2(entry.PivotX, entry.PivotY);
        rect.anchoredPosition = new Vector2(entry.AnchoredPosX, entry.AnchoredPosY);
        rect.sizeDelta = new Vector2(entry.SizeDeltaX, entry.SizeDeltaY);
        rect.localScale = new Vector3(entry.ScaleX, entry.ScaleY, entry.ScaleZ);
    }

    static void CacheLayouts()
    {
        foreach (var kvp in Elements)
        {
            if (kvp.Value.Rect == null)
                continue;

            SavedLayouts[kvp.Key] = LayoutEntry.FromRect(kvp.Value.Rect);
        }
    }

    sealed class LayoutModeBehaviour : MonoBehaviour
    {
        public LayoutModeBehaviour(IntPtr ptr) : base(ptr) { }

        // uGUI Panel elements
        GameObject _panelRoot;
        Canvas _panelCanvas;
        RectTransform _panelRect;
        TextMeshProUGUI _hoverText;
        TextMeshProUGUI _dragText;
        TextMeshProUGUI _elementsText;
        Toggle _snapToggle;
        Toggle _gridToggle;
        TextMeshProUGUI _gridSizeText;

        // State
        bool _active;
        string _draggingKey = string.Empty;
        RectTransform _draggingRect;
        Vector2 _dragStartMouse;
        Vector2 _dragStartPos;
        Vector2 _dragStartLocalPoint;
        RectTransform _draggingParentRect;
        Camera _draggingCamera;
        bool _draggingPanel;
        Vector2 _panelDragOffset;
        string _hoveredKey = string.Empty;

        // Outline rendering
        readonly List<GameObject> _outlineObjects = new();

        public bool IsActive => _active;

        public void ResetState()
        {
            _draggingKey = string.Empty;
            _draggingRect = null;
            _draggingParentRect = null;
            _draggingCamera = null;
            _dragStartLocalPoint = Vector2.zero;
            _draggingPanel = false;
            _hoveredKey = string.Empty;
        }

        void Awake()
        {
            CreatePanel();
        }

        void CreatePanel()
        {
            // Create canvas for the panel
            _panelRoot = new GameObject("LayoutPanel");
            _panelRoot.transform.SetParent(transform);

            _panelCanvas = _panelRoot.AddComponent<Canvas>();
            _panelCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _panelCanvas.sortingOrder = 9999;

            _panelRoot.AddComponent<CanvasScaler>();
            _panelRoot.AddComponent<GraphicRaycaster>();

            // Panel background
            var panel = new GameObject("Panel");
            panel.transform.SetParent(_panelRoot.transform);
            _panelRect = panel.AddComponent<RectTransform>();
            _panelRect.anchorMin = Vector2.zero;
            _panelRect.anchorMax = Vector2.zero;
            _panelRect.pivot = Vector2.zero;
            _panelRect.anchoredPosition = new Vector2(20f, Screen.height - 320f);
            _panelRect.sizeDelta = new Vector2(280f, 300f);

            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

            // Title
            CreateText(panel.transform, "Title", WindowTitle, 18, FontStyles.Bold,
                new Vector2(140f, -20f), new Vector2(260f, 30f), TextAlignmentOptions.Center);

            // Instructions
            CreateText(panel.transform, "Instructions", "LMB drag \u2022 Wheel resize \u2022 F8 toggle", 12, FontStyles.Normal,
                new Vector2(140f, -45f), new Vector2(260f, 20f), TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));

            // Elements count
            _elementsText = CreateText(panel.transform, "Elements", "Elements: 0", 14, FontStyles.Normal,
                new Vector2(140f, -70f), new Vector2(260f, 20f), TextAlignmentOptions.Center);

            // Hover info
            _hoverText = CreateText(panel.transform, "Hover", "Hover: (none)", 13, FontStyles.Normal,
                new Vector2(140f, -92f), new Vector2(260f, 20f), TextAlignmentOptions.Center, new Color(1f, 0.8f, 0.2f));

            // Drag info
            _dragText = CreateText(panel.transform, "Drag", "Drag: (none)", 13, FontStyles.Normal,
                new Vector2(140f, -114f), new Vector2(260f, 20f), TextAlignmentOptions.Center, new Color(0.2f, 1f, 1f));

            // Snap Grid toggle
            _snapToggle = CreateToggle(panel.transform, "SnapToggle", "Snap Grid", Options.SnapToGrid,
                new Vector2(70f, -145f), new Vector2(120f, 24f));

            // Show Grid toggle
            _gridToggle = CreateToggle(panel.transform, "GridToggle", "Show Grid", Options.ShowGrid,
                new Vector2(200f, -145f), new Vector2(120f, 24f));

            // Grid Size label
            CreateText(panel.transform, "GridLabel", "Grid Size:", 13, FontStyles.Normal,
                new Vector2(50f, -175f), new Vector2(80f, 24f), TextAlignmentOptions.Left);

            // Grid Size minus button
            var gridMinusBtn = CreateButtonBase(panel.transform, "GridMinus", "-", new Vector2(110f, -175f), new Vector2(30f, 24f));
            gridMinusBtn.onClick.AddListener((UnityEngine.Events.UnityAction)OnGridMinus);

            // Grid Size value
            _gridSizeText = CreateText(panel.transform, "GridValue", Options.GridSize.ToString("F0"), 13, FontStyles.Normal,
                new Vector2(155f, -175f), new Vector2(40f, 24f), TextAlignmentOptions.Center);

            // Grid Size plus button
            var gridPlusBtn = CreateButtonBase(panel.transform, "GridPlus", "+", new Vector2(190f, -175f), new Vector2(30f, 24f));
            gridPlusBtn.onClick.AddListener((UnityEngine.Events.UnityAction)OnGridPlus);

            // Action buttons row 1
            var saveBtn = CreateButtonBase(panel.transform, "Save", "Save", new Vector2(75f, -210f), new Vector2(120f, 28f));
            saveBtn.onClick.AddListener((UnityEngine.Events.UnityAction)OnSave);
            var loadBtn = CreateButtonBase(panel.transform, "Load", "Load", new Vector2(205f, -210f), new Vector2(120f, 28f));
            loadBtn.onClick.AddListener((UnityEngine.Events.UnityAction)OnLoad);

            // Action buttons row 2
            var deleteBtn = CreateButtonBase(panel.transform, "Delete", "Delete", new Vector2(75f, -245f), new Vector2(120f, 28f));
            deleteBtn.onClick.AddListener((UnityEngine.Events.UnityAction)OnDelete);
            var resetBtn = CreateButtonBase(panel.transform, "Reset", "Reset Default", new Vector2(205f, -245f), new Vector2(120f, 28f));
            resetBtn.onClick.AddListener((UnityEngine.Events.UnityAction)OnReset);

            // Close button
            var closeBtn = CreateButtonBase(panel.transform, "Close", "Close (F8)", new Vector2(140f, -280f), new Vector2(260f, 28f));
            closeBtn.onClick.AddListener((UnityEngine.Events.UnityAction)OnClose);

            _panelRoot.SetActive(false);
        }

        TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, FontStyles style,
            Vector2 position, Vector2 size, TextAlignmentOptions alignment, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color ?? Color.white;
            tmp.raycastTarget = false;

            return tmp;
        }

        Toggle CreateToggle(Transform parent, string name, string label, bool isOn, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(0f, 0.5f);
            bgRect.pivot = new Vector2(0f, 0.5f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(20f, 20f);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.25f);

            // Checkmark
            var check = new GameObject("Checkmark");
            check.transform.SetParent(bg.transform);
            var checkRect = check.AddComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(4f, 4f);
            checkRect.offsetMax = new Vector2(-4f, -4f);
            var checkImage = check.AddComponent<Image>();
            checkImage.color = new Color(0.3f, 0.8f, 0.3f);

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(25f, 0f);
            labelRect.offsetMax = Vector2.zero;
            var labelText = labelGo.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 13f;
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.color = Color.white;
            labelText.raycastTarget = false;

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = isOn;

            return toggle;
        }

        Button CreateButtonBase(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.25f, 0.25f, 0.3f);

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.25f, 0.25f, 0.3f);
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.4f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.25f);
            button.colors = colors;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelText = labelGo.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 13f;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;
            labelText.raycastTarget = false;

            return button;
        }

        void UpdateGridSizeText()
        {
            if (_gridSizeText != null)
                _gridSizeText.text = Options.GridSize.ToString("F0");
        }

        // Button callbacks (instance methods to avoid IL2CPP Action issues)
        void OnGridMinus()
        {
            Options.GridSize = Mathf.Max(5f, Options.GridSize - 5f);
            UpdateGridSizeText();
        }

        void OnGridPlus()
        {
            Options.GridSize = Mathf.Min(100f, Options.GridSize + 5f);
            UpdateGridSizeText();
        }

        void OnSave() => SaveLayout();
        void OnLoad() => LoadLayout();
        void OnDelete() => DeleteLayout();
        void OnReset() => ResetToDefaults();
        void OnClose()
        {
            _active = false;
            _panelRoot?.SetActive(false);
            UpdateOutlines();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                _active = !_active;
                _panelRoot?.SetActive(_active);

                DebugToolsBridge.TryLogInfo($"[Layout] {WindowTitle}: {(_active ? "ON" : "OFF")} - {Elements.Count} elements registered");

                if (_active && Elements.Count > 0)
                {
                    foreach (var kvp in Elements)
                    {
                        var r = kvp.Value.Rect;
                        if (r != null)
                        {
                            // Get actual rect size (calculated after layout)   
                            Rect localRect = r.rect;
                            string boundsInfo = "calcBounds=(none)";
                            if (TryGetCanvasRelativeBoundsRect(r, out _, out var cam, out Rect boundsRect))
                                boundsInfo = $"calcBounds={boundsRect.size}, cam={(cam != null ? cam.name : "null")}";
                            string rectInfo = $"pos={r.anchoredPosition}, sizeDelta={r.sizeDelta}, actualSize={localRect.size}, {boundsInfo}, scale={r.localScale}";
                            DebugToolsBridge.TryLogInfo($"[Layout]   - {kvp.Key}: {rectInfo}");
                        }
                        else
                        {
                            DebugToolsBridge.TryLogInfo($"[Layout]   - {kvp.Key}: NULL");
                        }
                    }
                }

                ResetState();
                UpdateOutlines();
            }

            if (!_active)
                return;

            // Sync toggle states
            if (_snapToggle != null)
                Options.SnapToGrid = _snapToggle.isOn;
            if (_gridToggle != null)
                Options.ShowGrid = _gridToggle.isOn;

            UpdateHover();
            UpdatePanelInfo();

            // Mouse wheel resize
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.001f && !_draggingPanel && !IsPointerOverPanel())
                ResizeHovered(scroll);

            // Drag handling
            if (Input.GetMouseButtonDown(0))
            {
                if (TryBeginPanelDrag())
                    return;

                if (!IsPointerOverPanel())
                    TryBeginDrag();
            }
            else if (Input.GetMouseButton(0))
            {
                if (_draggingPanel)
                    DragPanel();
                else if (_draggingRect != null)
                    DragElement();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _draggingKey = string.Empty;
                _draggingRect = null;
                _draggingParentRect = null;
                _draggingCamera = null;
                _dragStartLocalPoint = Vector2.zero;
                _draggingPanel = false;
            }

            UpdateOutlines();
        }

        void UpdatePanelInfo()
        {
            if (_elementsText != null)
                _elementsText.text = $"Elements: {Elements.Count}";

            if (_hoverText != null)
                _hoverText.text = $"Hover: {(string.IsNullOrEmpty(_hoveredKey) ? "(none)" : _hoveredKey)}";

            if (_dragText != null)
                _dragText.text = $"Drag: {(string.IsNullOrEmpty(_draggingKey) ? "(none)" : _draggingKey)}";
        }

        void UpdateOutlines()
        {
            // Clear existing outlines
            foreach (var obj in _outlineObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            _outlineObjects.Clear();

            if (!_active)
                return;

            // Create outlines for each element
            foreach (var kvp in Elements)
            {
                var rect = kvp.Value.Rect;
                if (rect == null || !rect.gameObject.activeInHierarchy)
                    continue;

                Color outlineColor;
                if (kvp.Key == _draggingKey)
                    outlineColor = new Color(0.2f, 1f, 1f, 0.9f);
                else if (kvp.Key == _hoveredKey)
                    outlineColor = new Color(1f, 0.8f, 0.2f, 0.8f);
                else
                    outlineColor = new Color(1f, 1f, 1f, 0.5f);

                CreateOutlineForElement(rect, outlineColor, kvp.Key);
            }
        }

        void CreateOutlineForElement(RectTransform targetRect, Color color, string key)
        {
            // Create outline as a child of the target's canvas
            Canvas targetCanvas = targetRect.GetComponentInParent<Canvas>();    
            if (targetCanvas == null)
                return;

            var outline = new GameObject($"LayoutOutline_{key}");
            outline.transform.SetParent(targetCanvas.transform, false);
            _outlineObjects.Add(outline);

            var outlineRect = outline.AddComponent<RectTransform>();

            bool applied = false;
            if (TryGetCanvasRelativeBoundsRect(targetRect, out RectTransform canvasRect, out _, out Rect boundsRect))
            {
                outlineRect.anchorMin = canvasRect.pivot;
                outlineRect.anchorMax = canvasRect.pivot;
                outlineRect.pivot = new Vector2(0.5f, 0.5f);
                outlineRect.anchoredPosition = boundsRect.center;
                outlineRect.sizeDelta = boundsRect.size;
                outlineRect.localScale = Vector3.one;
                applied = true;
            }

            if (!applied)
            {
                // Copy the target's transform properties
                outlineRect.anchorMin = targetRect.anchorMin;
                outlineRect.anchorMax = targetRect.anchorMax;
                outlineRect.pivot = targetRect.pivot;
                outlineRect.anchoredPosition = targetRect.anchoredPosition;
                outlineRect.sizeDelta = targetRect.sizeDelta;
                outlineRect.localScale = targetRect.localScale;
            }

            // Create 4 border images
            float thickness = 2f;
            CreateBorderImage(outline.transform, "Top", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 0f), new Vector2(0f, thickness), color);
            CreateBorderImage(outline.transform, "Bottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, thickness), color);
            CreateBorderImage(outline.transform, "Left", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0f), new Vector2(thickness, 0f), color);
            CreateBorderImage(outline.transform, "Right", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, 0f), new Vector2(thickness, 0f), color);

            // Add label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(outline.transform);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 5f);
            labelRect.sizeDelta = new Vector2(200f, 20f);

            var labelBg = labelGo.AddComponent<Image>();
            labelBg.color = new Color(0f, 0f, 0f, 0.7f);
            labelBg.raycastTarget = false;

            var labelTextGo = new GameObject("Text");
            labelTextGo.transform.SetParent(labelGo.transform);
            var labelTextRect = labelTextGo.AddComponent<RectTransform>();
            labelTextRect.anchorMin = Vector2.zero;
            labelTextRect.anchorMax = Vector2.one;
            labelTextRect.offsetMin = Vector2.zero;
            labelTextRect.offsetMax = Vector2.zero;
            var labelText = labelTextGo.AddComponent<TextMeshProUGUI>();
            labelText.text = key;
            labelText.fontSize = 11f;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = color;
            labelText.raycastTarget = false;
        }

        void CreateBorderImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;

            // For top/bottom, stretch horizontally
            if (name == "Top" || name == "Bottom")
            {
                rect.anchorMin = new Vector2(0f, anchorMin.y);
                rect.anchorMax = new Vector2(1f, anchorMax.y);
                rect.offsetMin = new Vector2(0f, name == "Bottom" ? 0f : -sizeDelta.y);
                rect.offsetMax = new Vector2(0f, name == "Top" ? 0f : sizeDelta.y);
            }
            // For left/right, stretch vertically
            else
            {
                rect.anchorMin = new Vector2(anchorMin.x, 0f);
                rect.anchorMax = new Vector2(anchorMax.x, 1f);
                rect.offsetMin = new Vector2(name == "Left" ? 0f : -sizeDelta.x, 0f);
                rect.offsetMax = new Vector2(name == "Right" ? 0f : sizeDelta.x, 0f);
            }

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        void UpdateHover()
        {
            if (_draggingRect != null)
                return;

            Vector2 mousePos = Input.mousePosition;
            string hoveredKey = string.Empty;
            float hoveredArea = float.MaxValue;

            foreach (var kvp in Elements)
            {
                var rect = kvp.Value.Rect;
                if (rect == null || !rect.gameObject.activeInHierarchy)
                    continue;

                Camera cam = GetCanvasCamera(rect);

                // Try bounds-based detection first
                if (TryGetCanvasRelativeBoundsRect(rect, out RectTransform canvasRect, out Camera boundsCamera, out Rect boundsRect))
                {
                    Vector2 localMouse;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, boundsCamera, out localMouse))
                    {
                        // Slight padding so zero/edge cases are still selectable.
                        const float padding = 4f;
                        boundsRect.xMin -= padding;
                        boundsRect.yMin -= padding;
                        boundsRect.xMax += padding;
                        boundsRect.yMax += padding;

                        if (boundsRect.Contains(localMouse))
                        {
                            float area = boundsRect.width * boundsRect.height;
                            if (area < hoveredArea)
                            {
                                hoveredArea = area;
                                hoveredKey = kvp.Key;
                            }
                            continue;
                        }
                    }
                }

                // Fallback: Use RectangleContainsScreenPoint which works for stretch-anchored elements
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, cam))
                {
                    // Estimate area from the rect's actual rendered size
                    Rect localRect = rect.rect;
                    float area = localRect.width * localRect.height * rect.lossyScale.x * rect.lossyScale.y;
                    if (area <= 0.01f)
                        area = 10000f; // Default area for zero-sized containers
                    
                    if (area < hoveredArea)
                    {
                        hoveredArea = area;
                        hoveredKey = kvp.Key;
                    }
                    continue;
                }

                // Final fallback: Check if mouse is inside any child graphics
                if (TryHoverOnChildren(rect, mousePos, cam, out float childArea))
                {
                    if (childArea < hoveredArea)
                    {
                        hoveredArea = childArea;
                        hoveredKey = kvp.Key;
                    }
                }
            }

            _hoveredKey = hoveredKey;
        }

        bool TryHoverOnChildren(RectTransform parent, Vector2 mousePos, Camera cam, out float area)
        {
            area = float.MaxValue;
            var graphics = parent.GetComponentsInChildren<Graphic>(false);
            
            foreach (var graphic in graphics)
            {
                if (graphic == null || !graphic.enabled)
                    continue;
                
                var childRect = graphic.rectTransform;
                if (childRect == null)
                    continue;
                
                if (RectTransformUtility.RectangleContainsScreenPoint(childRect, mousePos, cam))
                {
                    Rect localRect = childRect.rect;
                    float childArea = localRect.width * localRect.height;
                    if (childArea > 1f && childArea < area)
                        area = childArea;
                }
            }
            
            return area < float.MaxValue;
        }


        Camera GetCanvasCamera(RectTransform rect)
        {
            if (rect == null)
                return null;

            Canvas canvas = rect.GetComponentInParent<Canvas>();
            if (canvas == null)
                return null;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            if (canvas.rootCanvas != null && canvas.rootCanvas.worldCamera != null)
                return canvas.rootCanvas.worldCamera;

            if (canvas.worldCamera != null)
                return canvas.worldCamera;

            return Camera.main;
        }

        bool TryGetCanvasRelativeBoundsRect(
            RectTransform targetRect,
            out RectTransform canvasRect,
            out Camera cam,
            out Rect boundsRect)
        {
            canvasRect = null;
            cam = null;
            boundsRect = default;

            if (targetRect == null)
                return false;

            Canvas canvas = targetRect.GetComponentInParent<Canvas>();
            if (canvas == null)
                return false;

            canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
                return false;

            cam = GetCanvasCamera(targetRect);

            try
            {
                Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, targetRect);
                if (bounds.size.x > 1f && bounds.size.y > 1f)
                {
                    boundsRect = new Rect(
                        new Vector2(bounds.min.x, bounds.min.y),
                        new Vector2(bounds.size.x, bounds.size.y));
                    return true;
                }
            }
            catch
            {
                // IL2CPP method unstripping issue - fallback handles this gracefully
            }

            // Fallback to screen-rect conversion if bounds calc fails.
            if (!GetScreenRectFromWorldCorners(targetRect, out Rect screenRect))
                return false;

            Vector2 localMin;
            Vector2 localMax;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, new Vector2(screenRect.xMin, screenRect.yMin), cam, out localMin))
                return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, new Vector2(screenRect.xMax, screenRect.yMax), cam, out localMax))
                return false;

            Vector2 min = new(Mathf.Min(localMin.x, localMax.x), Mathf.Min(localMin.y, localMax.y));
            Vector2 max = new(Mathf.Max(localMin.x, localMax.x), Mathf.Max(localMin.y, localMax.y));
            Vector2 size = max - min;
            if (size.x <= 1f || size.y <= 1f)
                return false;

            boundsRect = new Rect(min, size);
            return true;
        }

        bool GetScreenRectFromWorldCorners(RectTransform rect, out Rect screenRect)
        {
            screenRect = default;

            // Get canvas and camera
            Camera cam = GetCanvasCamera(rect);

            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            // Check if corners are valid (not all zeros)
            bool hasValidCorners = false;
            for (int i = 0; i < 4; i++)
            {
                if (corners[i].sqrMagnitude > 0.001f)
                {
                    hasValidCorners = true;
                    break;
                }
            }

            if (hasValidCorners)
            {
                Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

                screenRect = new Rect(
                    Mathf.Min(min.x, max.x),
                    Mathf.Min(min.y, max.y),
                    Mathf.Abs(max.x - min.x),
                    Mathf.Abs(max.y - min.y)
                );

                if (screenRect.width > 1f && screenRect.height > 1f)
                    return true;
            }

            // Fallback: check rect.rect (local rect)
            Rect localRect = rect.rect;
            if (localRect.width > 1f && localRect.height > 1f)
            {
                Vector3 worldMin = rect.TransformPoint(localRect.min);
                Vector3 worldMax = rect.TransformPoint(localRect.max);

                Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(cam, worldMin);
                Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(cam, worldMax);

                screenRect = new Rect(
                    Mathf.Min(screenMin.x, screenMax.x),
                    Mathf.Min(screenMin.y, screenMax.y),
                    Mathf.Abs(screenMax.x - screenMin.x),
                    Mathf.Abs(screenMax.y - screenMin.y)
                );

                if (screenRect.width > 1f && screenRect.height > 1f)
                    return true;
            }

            // Final fallback: compute bounds from all child RectTransforms with Graphic components
            if (GetBoundsFromChildren(rect, cam, out screenRect))
                return true;

            // Additional fallback: include RectTransforms even without Graphics
            return GetBoundsFromRectTransforms(rect, cam, out screenRect);
        }

        bool GetBoundsFromChildren(RectTransform parent, Camera cam, out Rect screenRect)
        {
            screenRect = default;

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            bool foundAny = false;

            // Get all Graphics in children (Image, Text, etc.)
            var graphics = parent.GetComponentsInChildren<Graphic>(false);
            var childCorners = new Vector3[4];

            foreach (var graphic in graphics)
            {
                if (graphic == null || !graphic.enabled)
                    continue;

                var childRect = graphic.rectTransform;
                if (childRect == null)
                    continue;

                childRect.GetWorldCorners(childCorners);

                // Check if corners are valid
                bool validCorners = false;
                for (int i = 0; i < 4; i++)
                {
                    if (childCorners[i].sqrMagnitude > 0.001f)
                    {
                        validCorners = true;
                        break;
                    }
                }

                if (!validCorners)
                    continue;

                Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(cam, childCorners[0]);
                Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(cam, childCorners[2]);

                minX = Mathf.Min(minX, Mathf.Min(screenMin.x, screenMax.x));
                minY = Mathf.Min(minY, Mathf.Min(screenMin.y, screenMax.y));
                maxX = Mathf.Max(maxX, Mathf.Max(screenMin.x, screenMax.x));
                maxY = Mathf.Max(maxY, Mathf.Max(screenMin.y, screenMax.y));
                foundAny = true;
            }

            if (!foundAny)
                return false;

            screenRect = new Rect(minX, minY, maxX - minX, maxY - minY);        
            return screenRect.width > 1f && screenRect.height > 1f;
        }

        bool GetBoundsFromRectTransforms(RectTransform parent, Camera cam, out Rect screenRect)
        {
            screenRect = default;

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            bool foundAny = false;

            var rects = parent.GetComponentsInChildren<RectTransform>(false);
            var childCorners = new Vector3[4];

            foreach (var rect in rects)
            {
                if (rect == null)
                    continue;

                Rect localRect = rect.rect;
                if (localRect.width <= 1f && localRect.height <= 1f)
                    continue;

                rect.GetWorldCorners(childCorners);

                bool validCorners = false;
                for (int i = 0; i < 4; i++)
                {
                    if (childCorners[i].sqrMagnitude > 0.001f)
                    {
                        validCorners = true;
                        break;
                    }
                }

                if (!validCorners)
                    continue;

                Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(cam, childCorners[0]);
                Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(cam, childCorners[2]);

                minX = Mathf.Min(minX, Mathf.Min(screenMin.x, screenMax.x));
                minY = Mathf.Min(minY, Mathf.Min(screenMin.y, screenMax.y));
                maxX = Mathf.Max(maxX, Mathf.Max(screenMin.x, screenMax.x));
                maxY = Mathf.Max(maxY, Mathf.Max(screenMin.y, screenMax.y));
                foundAny = true;
            }

            if (!foundAny)
                return false;

            screenRect = new Rect(minX, minY, maxX - minX, maxY - minY);
            return screenRect.width > 1f && screenRect.height > 1f;
        }

        bool TryBeginPanelDrag()
        {
            if (_panelRect == null)
                return false;

            Vector2 mousePos = Input.mousePosition;

            // Check if clicking on the title area (top 40 pixels of panel)
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_panelRect, mousePos, null, out localPoint))
            {
                // Title area is at the top
                if (localPoint.y > _panelRect.rect.height - 40f && localPoint.y <= _panelRect.rect.height &&
                    localPoint.x >= 0 && localPoint.x <= _panelRect.rect.width)
                {
                    _draggingPanel = true;
                    _panelDragOffset = mousePos - (Vector2)_panelRect.position;
                    return true;
                }
            }
            return false;
        }

        void DragPanel()
        {
            if (_panelRect == null)
                return;

            Vector2 newPos = (Vector2)Input.mousePosition - _panelDragOffset;
            _panelRect.position = new Vector3(newPos.x, newPos.y, 0f);
        }

        bool IsPointerOverPanel()
        {
            if (_panelRect == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(_panelRect, Input.mousePosition, null);
        }

        void TryBeginDrag()
        {
            if (string.IsNullOrEmpty(_hoveredKey))
                return;

            if (!Elements.TryGetValue(_hoveredKey, out var element) || element.Rect == null)
                return;

            _draggingKey = _hoveredKey;
            _draggingRect = element.Rect;
            _dragStartPos = _draggingRect.anchoredPosition;
            _draggingParentRect = _draggingRect.parent as RectTransform;
            _draggingCamera = GetCanvasCamera(_draggingRect);
            _dragStartMouse = Input.mousePosition;
            _dragStartLocalPoint = Vector2.zero;
            if (_draggingParentRect != null)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _draggingParentRect, Input.mousePosition, _draggingCamera, out localPoint))
                {
                    _dragStartLocalPoint = localPoint;
                }
                else
                {
                    _draggingParentRect = null;
                    _draggingCamera = null;
                }
            }

            DebugToolsBridge.TryLogInfo($"[Layout] Started dragging: {_draggingKey}");
        }

        void DragElement()
        {
            if (_draggingRect == null)
                return;

            Vector2 newPos;
            if (_draggingParentRect != null)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _draggingParentRect, Input.mousePosition, _draggingCamera, out localPoint))
                {
                    Vector2 localDelta = localPoint - _dragStartLocalPoint;
                    newPos = _dragStartPos + localDelta;
                }
                else
                {
                    Vector2 mouseDelta = (Vector2)Input.mousePosition - _dragStartMouse;
                    newPos = _dragStartPos + mouseDelta;
                }
            }
            else
            {
                Vector2 mouseDelta = (Vector2)Input.mousePosition - _dragStartMouse;
                newPos = _dragStartPos + mouseDelta;
            }

            if (Options.SnapToGrid && Options.GridSize > 1f)
            {
                newPos.x = Mathf.Round(newPos.x / Options.GridSize) * Options.GridSize;
                newPos.y = Mathf.Round(newPos.y / Options.GridSize) * Options.GridSize;
            }

            _draggingRect.anchoredPosition = newPos;
        }

        void ResizeHovered(float scroll)
        {
            if (string.IsNullOrEmpty(_hoveredKey))
                return;

            if (!Elements.TryGetValue(_hoveredKey, out var element) || element.Rect == null)
                return;

            float factor = 1f + scroll * 0.1f;
            element.Rect.localScale *= factor;
            DebugToolsBridge.TryLogInfo($"[Layout] Resized: {_hoveredKey} scale={element.Rect.localScale}");
        }
    }

    sealed class LayoutElement
    {
        public string Key { get; }
        public RectTransform Rect { get; }

        public LayoutElement(string key, RectTransform rect)
        {
            Key = key;
            Rect = rect;
        }
    }

    sealed class LayoutEntry
    {
        public float AnchorMinX { get; set; }
        public float AnchorMinY { get; set; }
        public float AnchorMaxX { get; set; }
        public float AnchorMaxY { get; set; }
        public float PivotX { get; set; }
        public float PivotY { get; set; }
        public float AnchoredPosX { get; set; }
        public float AnchoredPosY { get; set; }
        public float SizeDeltaX { get; set; }
        public float SizeDeltaY { get; set; }
        public float ScaleX { get; set; }
        public float ScaleY { get; set; }
        public float ScaleZ { get; set; }

        public static LayoutEntry FromRect(RectTransform rect)
        {
            return new LayoutEntry
            {
                AnchorMinX = rect.anchorMin.x,
                AnchorMinY = rect.anchorMin.y,
                AnchorMaxX = rect.anchorMax.x,
                AnchorMaxY = rect.anchorMax.y,
                PivotX = rect.pivot.x,
                PivotY = rect.pivot.y,
                AnchoredPosX = rect.anchoredPosition.x,
                AnchoredPosY = rect.anchoredPosition.y,
                SizeDeltaX = rect.sizeDelta.x,
                SizeDeltaY = rect.sizeDelta.y,
                ScaleX = rect.localScale.x,
                ScaleY = rect.localScale.y,
                ScaleZ = rect.localScale.z
            };
        }
    }

    sealed class LayoutConfig
    {
        public Dictionary<string, LayoutEntry> Layouts { get; set; } = new();
        public Dictionary<string, LayoutEntry> Defaults { get; set; } = new();
        public LayoutOptions Options { get; set; } = new();
    }

    internal sealed class LayoutOptions
    {
        public bool SnapToGrid { get; set; } = true;
        public bool ShowGrid { get; set; } = true;
        public float GridSize { get; set; } = 10f;
        public bool VerticalBars { get; set; }
        public bool CompactQuests { get; set; }
    }
}
