using BepInEx;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;
using Eclipse.Services;

namespace Eclipse.Services;

internal static class LayoutService
{
    const string WindowTitle = "Eclipse Layout";
    const string LayoutFileName = "layout.json";

    static readonly string LayoutPath = Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.{LayoutFileName}");
    static readonly Dictionary<string, LayoutElement> Elements = new();
    static Dictionary<string, LayoutEntry> SavedLayouts = new();
    static Dictionary<string, LayoutEntry> DefaultLayouts = new();
    static LayoutOptions Options = new();

    static LayoutModeBehaviour _behaviour;

    internal static LayoutOptions CurrentOptions => Options;
    internal static bool IsLayoutModeActive => _behaviour != null && _behaviour.IsActive;

    /// <summary>
    /// Called by InputAdaptiveManager when input device changes.
    /// </summary>
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
        
        // Store default layout if not already stored
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
        const float GridAlpha = 0.1f;
        static readonly Color OutlineColor = new(1f, 1f, 1f, 0.6f);
        static readonly Color SelectedColor = new(0.2f, 1f, 1f, 0.9f);
        static readonly Color HoverColor = new(1f, 0.8f, 0.2f, 0.8f);

        Rect _windowRect = new(20f, 20f, 260f, 280f);
        bool _active;
        string _draggingKey = string.Empty;
        RectTransform _draggingRect;
        Vector2 _dragStartMouse;
        Vector2 _dragStartPos;
        bool _draggingPanel;
        Vector2 _panelDragOffset;
        string _hoveredKey = string.Empty;

        public bool IsActive => _active;

        public void ResetState()
        {
            _draggingKey = string.Empty;
            _draggingRect = null;
            _draggingPanel = false;
            _hoveredKey = string.Empty;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                _active = !_active;
                DebugToolsBridge.TryLogInfo($"[Layout] {WindowTitle}: {(_active ? "ON" : "OFF")} - {Elements.Count} elements registered");
                ResetState();
            }

            if (!_active)
                return;

            UpdateHover();

            // Mouse wheel resize
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.001f && !_draggingPanel && !IsPointerOverWindow())
                ResizeHovered(scroll);

            // Drag handling
            if (Input.GetMouseButtonDown(0))
            {
                if (TryBeginPanelDrag())
                    return;

                if (!IsPointerOverWindow())
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
                _draggingPanel = false;
            }
        }

        void OnGUI()
        {
            if (!_active)
                return;

            DrawPanel();
            if (Options.ShowGrid)
                DrawGrid();
            DrawOutlines();
        }

        void DrawPanel()
        {
            // Background
            GUI.color = new Color(0f, 0f, 0f, 0.9f);
            GUI.DrawTexture(_windowRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            float x = _windowRect.x + 10f;
            float y = _windowRect.y + 8f;
            float w = _windowRect.width - 20f;

            // Title
            GUI.Label(new Rect(x, y, w, 20f), WindowTitle);
            y += 24f;

            // Instructions
            GUI.Label(new Rect(x, y, w, 16f), "LMB drag • Wheel resize • F8 toggle");
            y += 20f;

            // Element count
            GUI.Label(new Rect(x, y, w, 16f), $"Elements: {Elements.Count}");
            y += 20f;

            // Debug: show hovered element
            string hoverInfo = string.IsNullOrEmpty(_hoveredKey) ? "(none)" : _hoveredKey;
            GUI.Label(new Rect(x, y, w, 16f), $"Hover: {hoverInfo}");
            y += 20f;

            // Debug: show dragging element
            string dragInfo = string.IsNullOrEmpty(_draggingKey) ? "(none)" : _draggingKey;
            GUI.Label(new Rect(x, y, w, 16f), $"Drag: {dragInfo}");
            y += 22f;

            // Grid options
            bool snapGrid = GUI.Toggle(new Rect(x, y, w / 2f, 18f), Options.SnapToGrid, "Snap Grid");
            bool showGrid = GUI.Toggle(new Rect(x + w / 2f, y, w / 2f, 18f), Options.ShowGrid, "Show Grid");
            y += 22f;

            GUI.Label(new Rect(x, y, 70f, 18f), "Grid Size:");
            float gridSize = Options.GridSize;
            if (GUI.Button(new Rect(x + 75f, y, 30f, 18f), "-"))
                gridSize = Mathf.Max(5f, gridSize - 5f);
            GUI.Label(new Rect(x + 110f, y, 40f, 18f), gridSize.ToString("F0"));
            if (GUI.Button(new Rect(x + 155f, y, 30f, 18f), "+"))
                gridSize = Mathf.Min(100f, gridSize + 5f);
            y += 28f;

            Options.SnapToGrid = snapGrid;
            Options.ShowGrid = showGrid;
            Options.GridSize = gridSize;

            // Main action buttons
            float halfW = (w - 6f) / 2f;
            
            if (GUI.Button(new Rect(x, y, halfW, 24f), "Save"))
                SaveLayout();
            if (GUI.Button(new Rect(x + halfW + 6f, y, halfW, 24f), "Load"))
                LoadLayout();
            y += 28f;

            if (GUI.Button(new Rect(x, y, halfW, 24f), "Delete"))
                DeleteLayout();
            if (GUI.Button(new Rect(x + halfW + 6f, y, halfW, 24f), "Reset Default"))
                ResetToDefaults();
        }

        void DrawGrid()
        {
            float size = Options.GridSize;
            if (size < 5f)
                return;

            GUI.color = new Color(1f, 1f, 1f, GridAlpha);

            for (float gx = 0; gx < Screen.width; gx += size)
                GUI.DrawTexture(new Rect(gx, 0, 1, Screen.height), Texture2D.whiteTexture);

            for (float gy = 0; gy < Screen.height; gy += size)
                GUI.DrawTexture(new Rect(0, gy, Screen.width, 1), Texture2D.whiteTexture);

            GUI.color = Color.white;
        }

        void DrawOutlines()
        {
            foreach (var kvp in Elements)
            {
                var rect = kvp.Value.Rect;
                if (rect == null)
                    continue;

                Rect screenRect = GetScreenRect(rect);
                Color c;
                if (kvp.Key == _draggingKey)
                    c = SelectedColor;
                else if (kvp.Key == _hoveredKey)
                    c = HoverColor;
                else
                    c = OutlineColor;
                    
                DrawRectOutline(screenRect, c, 2f);
            }
        }

        Rect GetScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera ?? Camera.main;

            Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

            // Convert to GUI coordinates (Y is inverted)
            float guiMinY = Screen.height - max.y;
            float guiMaxY = Screen.height - min.y;

            return new Rect(min.x, guiMinY, max.x - min.x, guiMaxY - guiMinY);
        }

        void DrawRectOutline(Rect r, Color c, float thickness)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void UpdateHover()
        {
            if (_draggingRect != null)
                return;

            Vector2 mouseGui = new(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            _hoveredKey = string.Empty;

            foreach (var kvp in Elements)
            {
                var rect = kvp.Value.Rect;
                if (rect == null)
                    continue;

                Rect screenRect = GetScreenRect(rect);
                if (screenRect.Contains(mouseGui))
                {
                    _hoveredKey = kvp.Key;
                    break;
                }
            }
        }

        bool TryBeginPanelDrag()
        {
            Rect header = new(_windowRect.x, _windowRect.y, _windowRect.width, 24f);
            Vector2 mouse = new(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (header.Contains(mouse))
            {
                _draggingPanel = true;
                _panelDragOffset = mouse - new Vector2(_windowRect.x, _windowRect.y);
                return true;
            }
            return false;
        }

        void DragPanel()
        {
            Vector2 mouse = new(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            _windowRect.x = mouse.x - _panelDragOffset.x;
            _windowRect.y = mouse.y - _panelDragOffset.y;
        }

        void TryBeginDrag()
        {
            if (string.IsNullOrEmpty(_hoveredKey))
                return;

            if (!Elements.TryGetValue(_hoveredKey, out var element) || element.Rect == null)
                return;

            _draggingKey = _hoveredKey;
            _draggingRect = element.Rect;
            _dragStartMouse = Input.mousePosition;
            _dragStartPos = _draggingRect.anchoredPosition;
            
            DebugToolsBridge.TryLogInfo($"[Layout] Started dragging: {_draggingKey}");
        }

        void DragElement()
        {
            if (_draggingRect == null)
                return;

            Vector2 mouseDelta = (Vector2)Input.mousePosition - _dragStartMouse;
            Vector2 newPos = _dragStartPos + mouseDelta;

            // Apply grid snapping
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

        bool IsPointerOverWindow()
        {
            Vector2 mouse = new(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            return _windowRect.Contains(mouse);
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
                ScaleX = rect.localScale.x,
                ScaleY = rect.localScale.y,
                ScaleZ = rect.localScale.z
            };
        }
    }

    sealed class LayoutConfig
    {
        public Dictionary<string, LayoutEntry> Layouts { get; set; } = new();
        public LayoutOptions Options { get; set; } = new();
    }

    internal sealed class LayoutOptions
    {
        public bool SnapToGrid { get; set; } = true;
        public bool ShowGrid { get; set; } = true;
        public float GridSize { get; set; } = 10f;

        // Kept for backward compatibility with configurators
        public bool VerticalBars { get; set; }
        public bool CompactQuests { get; set; }
    }
}
