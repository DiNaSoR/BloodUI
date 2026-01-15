using BepInEx;
using Il2CppInterop.Runtime.Injection;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VDebug.Services;

namespace VDebug;

/// <summary>
/// Creates a floating uGUI debug panel with buttons for VDebug functionality.
/// </summary>
internal static class DebugPanelService
{
    static GameObject _panelObject;
    static bool _initialized;
    static bool _visible = true;

    // Panel configuration
    const float PanelWidth = 200f;
    const float PanelHeight = 280f;

    const float ButtonHeight = 28f;
    const float Padding = 8f;
    const float Spacing = 4f;

    /// <summary>
    /// Initialize the debug panel. Called when a suitable canvas is found.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized && _panelObject != null)
            return;

        try
        {
            CreatePanel();
            
            // Only mark as initialized if panel was actually created
            if (_panelObject != null)
            {
                _initialized = true;
                VDebugLog.Log.LogInfo("[VDebug] Debug panel initialized.");
            }
            else
            {
                VDebugLog.Log.LogWarning("[VDebug] Debug panel creation deferred - no canvas available yet.");
            }
        }
        catch (Exception ex)
        {
            VDebugLog.Log.LogWarning($"[VDebug] Failed to create debug panel: {ex}");
        }
    }

    /// <summary>
    /// Toggle panel visibility via API.
    /// </summary>
    public static void TogglePanel()
    {
        if (_panelObject == null)
            return;

        _visible = !_visible;
        _panelObject.SetActive(_visible);
    }

    /// <summary>
    /// Show the panel.
    /// </summary>
    public static void ShowPanel()
    {
        if (_panelObject == null)
            return;

        _visible = true;
        _panelObject.SetActive(true);
    }

    /// <summary>
    /// Hide the panel.
    /// </summary>
    public static void HidePanel()
    {
        if (_panelObject == null)
            return;

        _visible = false;
        _panelObject.SetActive(false);
    }

    /// <summary>
    /// Destroy and reset the panel.
    /// </summary>
    public static void Reset()
    {
        if (_panelObject != null)
        {
            UnityEngine.Object.Destroy(_panelObject);
            _panelObject = null;
        }
        _initialized = false;
    }

    static void CreatePanel()
    {
        // Find or create a canvas
        Canvas canvas = FindOrCreateCanvas();
        if (canvas == null)
        {
            VDebugLog.Log.LogWarning("[VDebug] No suitable canvas found for debug panel.");
            return;
        }

        // Create panel container
        _panelObject = new GameObject("VDebugPanel");
        _panelObject.transform.SetParent(canvas.transform, false);

        // Add RectTransform
        RectTransform panelRect = _panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);  // Top-left
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(20, -20);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        // Add background image
        Image bgImage = _panelObject.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

        // Add vertical layout group
        VerticalLayoutGroup layout = _panelObject.AddComponent<VerticalLayoutGroup>();
        RectOffset pad = new RectOffset();
        pad.left = (int)Padding;
        pad.right = (int)Padding;
        pad.top = (int)Padding;
        pad.bottom = (int)Padding;
        layout.padding = pad;
        layout.spacing = Spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Create header
        CreateHeader(_panelObject.transform, "VDebug Panel");

        // Create separator
        CreateSeparator(_panelObject.transform, "Asset Dumps");

        // Create dump buttons
        CreateButton(_panelObject.transform, "Dump All Menus", () => AssetDumpService.DumpMenuAssets());
        CreateButton(_panelObject.transform, "Dump Character Menu", () => AssetDumpService.DumpCharacterMenu());
        CreateButton(_panelObject.transform, "Dump HUD Menu", () => AssetDumpService.DumpHudMenu());
        CreateButton(_panelObject.transform, "Dump Main Menu", () => AssetDumpService.DumpMainMenu());

        // Inspector section
        CreateSeparator(_panelObject.transform, "Inspector Tools");
        CreateButton(_panelObject.transform, "UI Inspector", () => {
            UIInspectorService.Initialize(canvas);
            UIInspectorService.Toggle();
        });

        // Make it draggable
        AddDragHandler(_panelObject);

        // Initialize inspector
        UIInspectorService.Initialize(canvas);

        // Note: DontDestroyOnLoad is called on the canvas, which persists the panel
        VDebugLog.Log.LogInfo($"[VDebug] Panel created and active: {_panelObject.activeSelf}");
    }

    static GameObject _canvasObject;

    static Canvas FindOrCreateCanvas()
    {
        // Always use our own canvas for reliability
        if (_canvasObject != null)
        {
            Canvas existingCanvas = _canvasObject.GetComponent<Canvas>();
            if (existingCanvas != null)
                return existingCanvas;
        }

        // Create our own canvas
        _canvasObject = new GameObject("VDebugCanvas");
        Canvas canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;  // Maximum sorting order to be on top

        CanvasScaler scaler = _canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        _canvasObject.AddComponent<GraphicRaycaster>();

        // Persist across scenes
        UnityEngine.Object.DontDestroyOnLoad(_canvasObject);

        VDebugLog.Log.LogInfo($"[VDebug] Created VDebugCanvas with sorting order {canvas.sortingOrder}");

        return canvas;
    }

    static void CreateHeader(Transform parent, string text)
    {
        GameObject headerGo = new GameObject("Header");
        headerGo.transform.SetParent(parent, false);

        RectTransform rect = headerGo.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 24f);

        TextMeshProUGUI tmp = headerGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        TMP_FontAsset font = FontService.GetFont();
        if (font != null) tmp.font = font;
        tmp.fontSize = 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.9f, 0.7f, 0.2f);  // Gold color
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
    }

    static void CreateButton(Transform parent, string label, Action onClick)
    {
        GameObject buttonGo = new GameObject($"Button_{label.Replace(" ", "")}");
        buttonGo.transform.SetParent(parent, false);

        RectTransform rect = buttonGo.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, ButtonHeight);

        // Background
        Image bgImage = buttonGo.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.3f, 0.5f);

        // Button component
        Button button = buttonGo.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.3f, 0.5f);
        colors.highlightedColor = new Color(0.3f, 0.5f, 0.7f);
        colors.pressedColor = new Color(0.15f, 0.25f, 0.4f);
        colors.selectedColor = new Color(0.25f, 0.4f, 0.6f);
        button.colors = colors;

        button.onClick.AddListener((UnityAction)(() =>
        {
            try
            {
                onClick?.Invoke();
            }
            catch (Exception ex)
            {
                VDebugLog.Log.LogWarning($"[VDebug] Button action failed: {ex.Message}");
            }
        }));

        // Label
        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(buttonGo.transform, false);

        RectTransform labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        TMP_FontAsset font = FontService.GetFont();
        if (font != null) tmp.font = font;
        tmp.fontSize = 12;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
    }

    static void CreateSeparator(Transform parent, string label)
    {
        // Container for the separator
        GameObject sepGo = new GameObject($"Separator_{label}");
        sepGo.transform.SetParent(parent, false);

        RectTransform sepRect = sepGo.AddComponent<RectTransform>();
        sepRect.sizeDelta = new Vector2(0, 18f);

        // Horizontal layout
        HorizontalLayoutGroup hlg = sepGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;

        // Left line
        GameObject leftLine = new GameObject("LeftLine");
        leftLine.transform.SetParent(sepGo.transform, false);
        RectTransform leftRect = leftLine.AddComponent<RectTransform>();
        leftRect.sizeDelta = new Vector2(0, 1);
        LayoutElement leftLE = leftLine.AddComponent<LayoutElement>();
        leftLE.flexibleWidth = 1;
        leftLE.preferredHeight = 1;
        Image leftImg = leftLine.AddComponent<Image>();
        leftImg.color = new Color(0.4f, 0.4f, 0.5f, 0.6f);

        // Label
        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(sepGo.transform, false);
        RectTransform labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(0, 18f);
        LayoutElement labelLE = labelGo.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 80;
        TextMeshProUGUI labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        TMP_FontAsset font = FontService.GetFont();
        if (font != null) labelTmp.font = font;
        labelTmp.fontSize = 9;
        labelTmp.color = new Color(0.6f, 0.6f, 0.65f);
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.enableWordWrapping = false;

        // Right line
        GameObject rightLine = new GameObject("RightLine");
        rightLine.transform.SetParent(sepGo.transform, false);
        RectTransform rightRect = rightLine.AddComponent<RectTransform>();
        rightRect.sizeDelta = new Vector2(0, 1);
        LayoutElement rightLE = rightLine.AddComponent<LayoutElement>();
        rightLE.flexibleWidth = 1;
        rightLE.preferredHeight = 1;
        Image rightImg = rightLine.AddComponent<Image>();
        rightImg.color = new Color(0.4f, 0.4f, 0.5f, 0.6f);
    }


    static void AddDragHandler(GameObject panel)
    {
        // Register our drag handler type if needed
        if (!ClassInjector.IsTypeRegisteredInIl2Cpp(typeof(PanelDragHandler)))
            ClassInjector.RegisterTypeInIl2Cpp<PanelDragHandler>();

        panel.AddComponent<PanelDragHandler>();
    }

    /// <summary>
    /// Simple drag handler for the panel.
    /// </summary>
    class PanelDragHandler : MonoBehaviour
    {
        RectTransform _rect;
        bool _dragging;
        Vector2 _offset;
        const float HeaderHeight = 30f;

        void Start()
        {
            _rect = GetComponent<RectTransform>();
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = Input.mousePosition;
                Vector3[] corners = new Vector3[4];
                _rect.GetWorldCorners(corners);

                // Check if mouse is in header area (top 30px)
                Rect headerRect = new Rect(corners[0].x, corners[1].y - HeaderHeight, _rect.rect.width, HeaderHeight);
                if (RectTransformUtility.RectangleContainsScreenPoint(_rect, mousePos) && 
                    mousePos.y > corners[1].y - HeaderHeight)
                {
                    _dragging = true;
                    _offset = (Vector2)_rect.position - mousePos;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                _dragging = false;
            }

            if (_dragging)
            {
                _rect.position = (Vector2)Input.mousePosition + _offset;
            }
        }
    }
}
