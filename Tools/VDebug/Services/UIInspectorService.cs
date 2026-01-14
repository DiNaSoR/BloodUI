using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VDebug.Services;

/// <summary>
/// Live UI Inspector - click any UI element to see its path, components, and properties.
/// Toggle with a button or hotkey. When active, clicking any UI element shows its details.
/// </summary>
internal static class UIInspectorService
{
    static bool _inspectorActive;
    static GameObject _inspectorPanel;
    static TextMeshProUGUI _infoText;
    static GameObject _highlightOverlay;
    static Image _highlightImage;
    static RectTransform _currentTarget;
    static bool _initialized;

    // Inspector panel configuration
    const float PanelWidth = 400f;
    const float PanelHeight = 350f;
    const float Padding = 10f;

    public static bool IsActive => _inspectorActive;

    /// <summary>
    /// Initialize the inspector system.
    /// </summary>
    public static void Initialize(Canvas canvas)
    {
        if (_initialized || canvas == null)
            return;

        try
        {
            CreateInspectorPanel(canvas);
            CreateHighlightOverlay(canvas);
            CreateInspectorBehaviour(canvas.gameObject);
            _initialized = true;
            VDebugLog.Log.LogInfo("[VDebug] UI Inspector initialized.");
        }
        catch (Exception ex)
        {
            VDebugLog.Log.LogWarning($"[VDebug] Failed to initialize UI Inspector: {ex}");
        }
    }

    /// <summary>
    /// Toggle inspector mode on/off.
    /// </summary>
    public static void Toggle()
    {
        _inspectorActive = !_inspectorActive;

        if (_inspectorPanel != null)
            _inspectorPanel.SetActive(_inspectorActive);

        if (!_inspectorActive)
        {
            ClearHighlight();
            _currentTarget = null;
        }

        VDebugLog.Log.LogInfo($"[VDebug] UI Inspector: {(_inspectorActive ? "ENABLED" : "DISABLED")}");
    }

    /// <summary>
    /// Enable inspector mode.
    /// </summary>
    public static void Enable()
    {
        _inspectorActive = true;
        if (_inspectorPanel != null)
            _inspectorPanel.SetActive(true);
    }

    /// <summary>
    /// Disable inspector mode.
    /// </summary>
    public static void Disable()
    {
        _inspectorActive = false;
        if (_inspectorPanel != null)
            _inspectorPanel.SetActive(false);
        ClearHighlight();
        _currentTarget = null;
    }

    /// <summary>
    /// Inspect a specific RectTransform and display its info.
    /// </summary>
    public static void InspectElement(RectTransform target)
    {
        if (target == null)
            return;

        _currentTarget = target;
        UpdateHighlight(target);
        UpdateInfoPanel(target);
    }

    static void CreateInspectorPanel(Canvas canvas)
    {
        // Main panel
        _inspectorPanel = new GameObject("VDebugInspectorPanel");
        _inspectorPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = _inspectorPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);  // Top-right
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-20, -20);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight + 40f); // Increase height for button

        // Background
        Image bgImage = _inspectorPanel.AddComponent<Image>();
        bgImage.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

        // Vertical layout
        VerticalLayoutGroup layout = _inspectorPanel.AddComponent<VerticalLayoutGroup>();
        RectOffset pad = new RectOffset();
        pad.left = (int)Padding;
        pad.right = (int)Padding;
        pad.top = (int)Padding;
        pad.bottom = (int)Padding;
        layout.padding = pad;
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Header
        CreateLabel(_inspectorPanel.transform, "UI Inspector", 16, new Color(0.9f, 0.7f, 0.2f), FontStyles.Bold);
        CreateLabel(_inspectorPanel.transform, "Click any UI element to inspect", 11, new Color(0.6f, 0.6f, 0.6f), FontStyles.Italic);

        // Separator
        CreateSeparator(_inspectorPanel.transform);

        // Info text (scrollable content)
        GameObject scrollViewGo = CreateScrollView(_inspectorPanel.transform);
        _infoText = scrollViewGo.GetComponentInChildren<TextMeshProUGUI>();

        // Export Button
        CreateButton(_inspectorPanel.transform, "Export Info to File", () => ExportInspection());

        _inspectorPanel.SetActive(false);

        // Add drag handler
        AddDragHandler(_inspectorPanel);
    }

    static void CreateHighlightOverlay(Canvas canvas)
    {
        _highlightOverlay = new GameObject("VDebugHighlight");
        _highlightOverlay.transform.SetParent(canvas.transform, false);

        RectTransform rect = _highlightOverlay.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(100, 100);

        _highlightImage = _highlightOverlay.AddComponent<Image>();
        _highlightImage.color = new Color(1f, 0.8f, 0.2f, 0.3f);  // Yellow tint
        _highlightImage.raycastTarget = false;

        // Add outline effect
        Outline outline = _highlightOverlay.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.8f, 0.2f, 0.8f);
        outline.effectDistance = new Vector2(2, 2);

        _highlightOverlay.SetActive(false);
    }

    static void UpdateHighlight(RectTransform target)
    {
        if (_highlightOverlay == null || target == null)
            return;

        RectTransform highlightRect = _highlightOverlay.GetComponent<RectTransform>();

        // Get world corners of target
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        // Calculate center and size
        Vector3 center = (corners[0] + corners[2]) / 2f;
        Vector3 size = corners[2] - corners[0];

        highlightRect.position = center;
        highlightRect.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));

        _highlightOverlay.SetActive(true);
    }

    static void ClearHighlight()
    {
        if (_highlightOverlay != null)
            _highlightOverlay.SetActive(false);
    }

    // Store last inspection for export
    static string _lastInspectionPlain = "";
    static string _lastInspectionPath = "";

    static void UpdateInfoPanel(RectTransform target)
    {
        if (_infoText == null || target == null)
            return;

        StringBuilder sb = new StringBuilder(8192);
        StringBuilder plainSb = new StringBuilder(8192);  // Plain text for export

        // ═══════════════════════════════════════════════════════════════════
        // HIERARCHY PATH
        // ═══════════════════════════════════════════════════════════════════
        string fullPath = GetPath(target);
        _lastInspectionPath = fullPath;

        sb.AppendLine("<color=#FFD700>══ HIERARCHY PATH ══</color>");
        sb.AppendLine($"<color=#AAAAAA>{fullPath}</color>");
        sb.AppendLine();

        plainSb.AppendLine("══ HIERARCHY PATH ══");
        plainSb.AppendLine(fullPath);
        plainSb.AppendLine();

        // ═══════════════════════════════════════════════════════════════════
        // GAMEOBJECT INFO
        // ═══════════════════════════════════════════════════════════════════
        sb.AppendLine("<color=#FFD700>══ GAMEOBJECT ══</color>");
        sb.AppendLine($"  Name: <color=#88CCFF>{target.name}</color>");
        sb.AppendLine($"  Active Self: {BoolColor(target.gameObject.activeSelf)}");
        sb.AppendLine($"  Active Hierarchy: {BoolColor(target.gameObject.activeInHierarchy)}");
        sb.AppendLine($"  Layer: {target.gameObject.layer} ({LayerMask.LayerToName(target.gameObject.layer)})");
        sb.AppendLine($"  Tag: {target.gameObject.tag}");
        sb.AppendLine($"  Instance ID: <color=#888888>{target.GetInstanceID()}</color>");
        sb.AppendLine();

        plainSb.AppendLine("══ GAMEOBJECT ══");
        plainSb.AppendLine($"  Name: {target.name}");
        plainSb.AppendLine($"  Active Self: {target.gameObject.activeSelf}");
        plainSb.AppendLine($"  Active Hierarchy: {target.gameObject.activeInHierarchy}");
        plainSb.AppendLine($"  Layer: {target.gameObject.layer} ({LayerMask.LayerToName(target.gameObject.layer)})");
        plainSb.AppendLine($"  Tag: {target.gameObject.tag}");
        plainSb.AppendLine($"  Instance ID: {target.GetInstanceID()}");
        plainSb.AppendLine();

        // ═══════════════════════════════════════════════════════════════════
        // RECTTRANSFORM DETAILS
        // ═══════════════════════════════════════════════════════════════════
        sb.AppendLine("<color=#FFD700>══ RECTTRANSFORM ══</color>");
        sb.AppendLine($"  <color=#AAFFAA>Anchors:</color>");
        sb.AppendLine($"    Min: ({target.anchorMin.x:F3}, {target.anchorMin.y:F3})");
        sb.AppendLine($"    Max: ({target.anchorMax.x:F3}, {target.anchorMax.y:F3})");
        sb.AppendLine($"  <color=#AAFFAA>Pivot:</color> ({target.pivot.x:F3}, {target.pivot.y:F3})");
        sb.AppendLine($"  <color=#AAFFAA>Anchored Pos:</color> ({target.anchoredPosition.x:F2}, {target.anchoredPosition.y:F2})");
        sb.AppendLine($"  <color=#AAFFAA>Size Delta:</color> ({target.sizeDelta.x:F2}, {target.sizeDelta.y:F2})");
        sb.AppendLine($"  <color=#AAFFAA>Offset Min:</color> ({target.offsetMin.x:F2}, {target.offsetMin.y:F2})");
        sb.AppendLine($"  <color=#AAFFAA>Offset Max:</color> ({target.offsetMax.x:F2}, {target.offsetMax.y:F2})");
        sb.AppendLine($"  <color=#AAFFAA>Local Scale:</color> ({target.localScale.x:F3}, {target.localScale.y:F3}, {target.localScale.z:F3})");
        sb.AppendLine($"  <color=#AAFFAA>Local Rotation:</color> ({target.localEulerAngles.x:F1}, {target.localEulerAngles.y:F1}, {target.localEulerAngles.z:F1})");
        sb.AppendLine($"  <color=#AAFFAA>Computed Rect:</color> {target.rect.width:F1} x {target.rect.height:F1}");
        sb.AppendLine($"  <color=#AAFFAA>World Position:</color> ({target.position.x:F1}, {target.position.y:F1}, {target.position.z:F1})");
        sb.AppendLine();

        plainSb.AppendLine("══ RECTTRANSFORM ══");
        plainSb.AppendLine($"  AnchorMin: ({target.anchorMin.x:F3}, {target.anchorMin.y:F3})");
        plainSb.AppendLine($"  AnchorMax: ({target.anchorMax.x:F3}, {target.anchorMax.y:F3})");
        plainSb.AppendLine($"  Pivot: ({target.pivot.x:F3}, {target.pivot.y:F3})");
        plainSb.AppendLine($"  AnchoredPosition: ({target.anchoredPosition.x:F2}, {target.anchoredPosition.y:F2})");
        plainSb.AppendLine($"  SizeDelta: ({target.sizeDelta.x:F2}, {target.sizeDelta.y:F2})");
        plainSb.AppendLine($"  OffsetMin: ({target.offsetMin.x:F2}, {target.offsetMin.y:F2})");
        plainSb.AppendLine($"  OffsetMax: ({target.offsetMax.x:F2}, {target.offsetMax.y:F2})");
        plainSb.AppendLine($"  LocalScale: ({target.localScale.x:F3}, {target.localScale.y:F3}, {target.localScale.z:F3})");
        plainSb.AppendLine($"  Rect: {target.rect.width:F1} x {target.rect.height:F1}");
        plainSb.AppendLine();

        // ═══════════════════════════════════════════════════════════════════
        // CANVAS INFO
        // ═══════════════════════════════════════════════════════════════════
        Canvas parentCanvas = target.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            sb.AppendLine("<color=#FFD700>══ CANVAS ══</color>");
            sb.AppendLine($"  Name: <color=#88CCFF>{parentCanvas.name}</color>");
            sb.AppendLine($"  Render Mode: <color=#FFAAFF>{parentCanvas.renderMode}</color>");
            sb.AppendLine($"  Sorting Order: {parentCanvas.sortingOrder}");
            sb.AppendLine($"  Sorting Layer: {parentCanvas.sortingLayerName}");
            sb.AppendLine($"  Pixel Perfect: {parentCanvas.pixelPerfect}");
            sb.AppendLine($"  Override Sorting: {parentCanvas.overrideSorting}");
            sb.AppendLine();

            plainSb.AppendLine("══ CANVAS ══");
            plainSb.AppendLine($"  Name: {parentCanvas.name}");
            plainSb.AppendLine($"  Render Mode: {parentCanvas.renderMode}");
            plainSb.AppendLine($"  Sorting Order: {parentCanvas.sortingOrder}");
            plainSb.AppendLine();
        }

        // ═══════════════════════════════════════════════════════════════════
        // PARENT CHAIN
        // ═══════════════════════════════════════════════════════════════════
        sb.AppendLine("<color=#FFD700>══ PARENTS ══</color>");
        plainSb.AppendLine("══ PARENTS ══");
        Transform parent = target.parent;
        int depth = 0;
        while (parent != null && depth < 10)
        {
            string indent = new string(' ', (depth + 1) * 2);
            sb.AppendLine($"{indent}↑ <color=#88AACC>{parent.name}</color>");
            plainSb.AppendLine($"{indent}↑ {parent.name}");
            parent = parent.parent;
            depth++;
        }
        sb.AppendLine();
        plainSb.AppendLine();

        // ═══════════════════════════════════════════════════════════════════
        // CHILDREN
        // ═══════════════════════════════════════════════════════════════════
        sb.AppendLine($"<color=#FFD700>══ CHILDREN ({target.childCount}) ══</color>");
        plainSb.AppendLine($"══ CHILDREN ({target.childCount}) ══");
        for (int i = 0; i < Mathf.Min(target.childCount, 15); i++)
        {
            Transform child = target.GetChild(i);
            string activeIcon = child.gameObject.activeSelf ? "●" : "○";
            sb.AppendLine($"  {activeIcon} <color=#AACCFF>{child.name}</color>");
            plainSb.AppendLine($"  {activeIcon} {child.name}");
        }
        if (target.childCount > 15)
        {
            sb.AppendLine($"  <color=#888888>... and {target.childCount - 15} more</color>");
            plainSb.AppendLine($"  ... and {target.childCount - 15} more");
        }
        sb.AppendLine();
        plainSb.AppendLine();

        // ═══════════════════════════════════════════════════════════════════
        // COMPONENTS (DEEP)
        // ═══════════════════════════════════════════════════════════════════
        Component[] components = target.GetComponents<Component>();
        sb.AppendLine($"<color=#FFD700>══ COMPONENTS ({components.Length}) ══</color>");
        plainSb.AppendLine($"══ COMPONENTS ({components.Length}) ══");

        foreach (Component comp in components)
        {
            if (comp == null) continue;
            string typeName = comp.GetType().Name;
            sb.AppendLine($"  ► <color=#CCCCFF>{typeName}</color>");
            plainSb.AppendLine($"  ► {typeName}");

            // Deep component info
            AppendComponentDetails(comp, sb, plainSb);
        }

        _infoText.text = sb.ToString();
        _lastInspectionPlain = plainSb.ToString();
    }

    static void AppendComponentDetails(Component comp, StringBuilder sb, StringBuilder plainSb)
    {
        string indent = "      ";

        if (comp is Image img)
        {
            sb.AppendLine($"{indent}Sprite: <color=#AAFFAA>{(img.sprite != null ? img.sprite.name : "null")}</color>");
            sb.AppendLine($"{indent}Color: {ColorToHex(img.color)}");
            sb.AppendLine($"{indent}Type: {img.type}");
            sb.AppendLine($"{indent}Fill: {img.fillMethod} ({img.fillAmount:F2})");
            sb.AppendLine($"{indent}Raycast: {img.raycastTarget}");
            sb.AppendLine($"{indent}Maskable: {img.maskable}");

            plainSb.AppendLine($"{indent}Sprite: {(img.sprite != null ? img.sprite.name : "null")}");
            plainSb.AppendLine($"{indent}Color: {ColorToHex(img.color)}");
            plainSb.AppendLine($"{indent}Type: {img.type}");
        }
        else if (comp is TMP_Text txt)
        {
            string preview = txt.text?.Length > 50 ? txt.text.Substring(0, 50) + "..." : txt.text;
            sb.AppendLine($"{indent}Text: <color=#AAFFAA>\"{preview}\"</color>");
            sb.AppendLine($"{indent}Font: {txt.font?.name ?? "null"}");
            sb.AppendLine($"{indent}Size: {txt.fontSize:F1}");
            sb.AppendLine($"{indent}Style: {txt.fontStyle}");
            sb.AppendLine($"{indent}Color: {ColorToHex(txt.color)}");
            sb.AppendLine($"{indent}Align: {txt.alignment}");
            sb.AppendLine($"{indent}Overflow: {txt.overflowMode}");
            sb.AppendLine($"{indent}Word Wrap: {txt.enableWordWrapping}");

            plainSb.AppendLine($"{indent}Text: \"{preview}\"");
            plainSb.AppendLine($"{indent}Font: {txt.font?.name ?? "null"}");
            plainSb.AppendLine($"{indent}Size: {txt.fontSize:F1}");
        }
        else if (comp is Button btn)
        {
            sb.AppendLine($"{indent}Interactable: {BoolColor(btn.interactable)}");
            sb.AppendLine($"{indent}Transition: {btn.transition}");
            sb.AppendLine($"{indent}Navigation: {btn.navigation.mode}");

            plainSb.AppendLine($"{indent}Interactable: {btn.interactable}");
        }
        else if (comp is LayoutGroup lg)
        {
            sb.AppendLine($"{indent}Padding: L{lg.padding.left} R{lg.padding.right} T{lg.padding.top} B{lg.padding.bottom}");
            sb.AppendLine($"{indent}Child Align: {lg.childAlignment}");

            if (lg is HorizontalOrVerticalLayoutGroup hvlg)
            {
                sb.AppendLine($"{indent}Spacing: {hvlg.spacing:F1}");
                sb.AppendLine($"{indent}Control Size: W={hvlg.childControlWidth} H={hvlg.childControlHeight}");
                sb.AppendLine($"{indent}Force Expand: W={hvlg.childForceExpandWidth} H={hvlg.childForceExpandHeight}");
            }

            if (lg is GridLayoutGroup glg)
            {
                sb.AppendLine($"{indent}Cell Size: ({glg.cellSize.x:F1}, {glg.cellSize.y:F1})");
                sb.AppendLine($"{indent}Spacing: ({glg.spacing.x:F1}, {glg.spacing.y:F1})");
                sb.AppendLine($"{indent}Constraint: {glg.constraint}");
            }

            plainSb.AppendLine($"{indent}Padding: L{lg.padding.left} R{lg.padding.right} T{lg.padding.top} B{lg.padding.bottom}");
        }
        else if (comp is LayoutElement le)
        {
            sb.AppendLine($"{indent}Min: W={le.minWidth:F1} H={le.minHeight:F1}");
            sb.AppendLine($"{indent}Preferred: W={le.preferredWidth:F1} H={le.preferredHeight:F1}");
            sb.AppendLine($"{indent}Flexible: W={le.flexibleWidth:F1} H={le.flexibleHeight:F1}");
            sb.AppendLine($"{indent}Ignore Layout: {le.ignoreLayout}");

            plainSb.AppendLine($"{indent}IgnoreLayout: {le.ignoreLayout}");
        }
        else if (comp is ContentSizeFitter csf)
        {
            sb.AppendLine($"{indent}Horizontal: {csf.horizontalFit}");
            sb.AppendLine($"{indent}Vertical: {csf.verticalFit}");

            plainSb.AppendLine($"{indent}H: {csf.horizontalFit}, V: {csf.verticalFit}");
        }
        else if (comp is ScrollRect sr)
        {
            sb.AppendLine($"{indent}Horizontal: {sr.horizontal}");
            sb.AppendLine($"{indent}Vertical: {sr.vertical}");
            sb.AppendLine($"{indent}Movement: {sr.movementType}");
            sb.AppendLine($"{indent}Inertia: {sr.inertia}");

            plainSb.AppendLine($"{indent}H: {sr.horizontal}, V: {sr.vertical}");
        }
        else if (comp is CanvasGroup cg)
        {
            sb.AppendLine($"{indent}Alpha: {cg.alpha:F2}");
            sb.AppendLine($"{indent}Interactable: {BoolColor(cg.interactable)}");
            sb.AppendLine($"{indent}Blocks Raycasts: {cg.blocksRaycasts}");
            sb.AppendLine($"{indent}Ignore Parent Groups: {cg.ignoreParentGroups}");

            plainSb.AppendLine($"{indent}Alpha: {cg.alpha:F2}");
        }
        else if (comp is Mask m)
        {
            sb.AppendLine($"{indent}Show Mask Graphic: {m.showMaskGraphic}");

            plainSb.AppendLine($"{indent}ShowMaskGraphic: {m.showMaskGraphic}");
        }
        else if (comp is Selectable sel)
        {
            sb.AppendLine($"{indent}Interactable: {BoolColor(sel.interactable)}");
            sb.AppendLine($"{indent}Transition: {sel.transition}");

            plainSb.AppendLine($"{indent}Interactable: {sel.interactable}");
        }
    }

    static string BoolColor(bool value)
    {
        return value ? "<color=#88FF88>Yes</color>" : "<color=#FF8888>No</color>";
    }

    /// <summary>
    /// Export the last inspection to a file.
    /// </summary>
    public static void ExportInspection()
    {
        if (string.IsNullOrEmpty(_lastInspectionPlain))
        {
            VDebugLog.Log.LogWarning("[VDebug] No inspection to export. Click an element first.");
            return;
        }

        try
        {
            string sanitizedPath = _lastInspectionPath.Replace("/", "_").Replace("\\", "_");
            if (sanitizedPath.Length > 50)
                sanitizedPath = sanitizedPath.Substring(0, 50);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"Inspection_{sanitizedPath}_{timestamp}.txt";
            string filePath = Path.Combine(BepInEx.Paths.BepInExRootPath, "VDebug", "Inspections", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, _lastInspectionPlain);

            VDebugLog.Log.LogInfo($"[VDebug] Inspection exported to: {filePath}");
        }
        catch (Exception ex)
        {
            VDebugLog.Log.LogWarning($"[VDebug] Failed to export inspection: {ex.Message}");
        }
    }

    static GameObject CreateScrollView(Transform parent)
    {
        // Scroll View Container
        GameObject scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(parent, false);

        RectTransform scrollRect = scrollGo.AddComponent<RectTransform>();
        scrollRect.sizeDelta = new Vector2(0, 250f);

        // Add mask
        Image scrollBg = scrollGo.AddComponent<Image>();
        scrollBg.color = new Color(0.05f, 0.05f, 0.07f, 0.8f);
        scrollGo.AddComponent<Mask>().showMaskGraphic = true;

        // ScrollRect component
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 20f;

        // Content
        GameObject contentGo = new GameObject("Content");
        contentGo.transform.SetParent(scrollGo.transform, false);

        RectTransform contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        // Content size fitter
        ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;
        scroll.viewport = scrollRect;

        // Text
        TextMeshProUGUI text = contentGo.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = FontService.GetFont();
        if (font != null) text.font = font;
        text.fontSize = 11;

        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.richText = true;
        text.text = "<color=#888888>Click a UI element to inspect...</color>";

        return scrollGo;
    }

    static void CreateLabel(Transform parent, string text, float fontSize, Color color, FontStyles style)
    {
        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(parent, false);

        RectTransform rect = labelGo.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, fontSize + 6);

        TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        TMP_FontAsset font = FontService.GetFont();
        if (font != null) tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = false;
    }

    static void CreateSeparator(Transform parent)
    {
        GameObject sepGo = new GameObject("Separator");
        sepGo.transform.SetParent(parent, false);

        RectTransform rect = sepGo.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 2);

        Image sepImg = sepGo.AddComponent<Image>();
        sepImg.color = new Color(0.3f, 0.3f, 0.35f, 0.8f);
    }

    static void CreateButton(Transform parent, string label, Action onClick)
    {
        GameObject buttonGo = new GameObject($"Button_{label.Replace(" ", "")}");
        buttonGo.transform.SetParent(parent, false);

        RectTransform rect = buttonGo.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 28f); // Fixed height

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

    static void AddDragHandler(GameObject panel)
    {
        if (!ClassInjector.IsTypeRegisteredInIl2Cpp(typeof(InspectorDragHandler)))
            ClassInjector.RegisterTypeInIl2Cpp<InspectorDragHandler>();

        panel.AddComponent<InspectorDragHandler>();
    }

    static void CreateInspectorBehaviour(GameObject canvasGo)
    {
        if (!ClassInjector.IsTypeRegisteredInIl2Cpp(typeof(InspectorBehaviour)))
            ClassInjector.RegisterTypeInIl2Cpp<InspectorBehaviour>();

        canvasGo.AddComponent<InspectorBehaviour>();
    }

    static string GetPath(Transform transform)
    {
        if (transform == null) return "null";

        StringBuilder sb = new StringBuilder(256);
        sb.Append(transform.name);

        Transform parent = transform.parent;
        int depth = 0;
        while (parent != null && depth < 20)
        {
            sb.Insert(0, '/');
            sb.Insert(0, parent.name);
            parent = parent.parent;
            depth++;
        }

        return sb.ToString();
    }

    static string ColorToHex(Color color)
    {
        return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
    }

    /// <summary>
    /// Behaviour that handles click detection for the inspector.
    /// </summary>
    class InspectorBehaviour : MonoBehaviour
    {
        void Update()
        {
            if (!_inspectorActive)
                return;

            // Right-click to exit inspector mode
            if (Input.GetMouseButtonDown(1))
            {
                Disable();
                return;
            }

            // Left-click to inspect
            if (Input.GetMouseButtonDown(0))
            {
                // Don't inspect if clicking on the inspector panel itself
                if (_inspectorPanel != null)
                {
                    RectTransform panelRect = _inspectorPanel.GetComponent<RectTransform>();
                    if (RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition))
                        return;
                }

                RectTransform hit = RaycastUI();
                if (hit != null)
                {
                    InspectElement(hit);
                }
            }

            // Update highlight position if target moved
            if (_currentTarget != null && _highlightOverlay != null && _highlightOverlay.activeSelf)
            {
                UpdateHighlight(_currentTarget);
            }
        }

        RectTransform RaycastUI()
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            Il2CppSystem.Collections.Generic.List<RaycastResult> results = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            // Skip our own overlay and panel
            for (int i = 0; i < results.Count; i++)
            {
                RaycastResult result = results[i];
                if (result.gameObject == null)
                    continue;

                // Skip VDebug elements
                if (result.gameObject.name.StartsWith("VDebug"))
                    continue;

                RectTransform rect = result.gameObject.GetComponent<RectTransform>();
                if (rect != null)
                    return rect;
            }

            return null;
        }
    }

    /// <summary>
    /// Drag handler for the inspector panel.
    /// </summary>
    class InspectorDragHandler : MonoBehaviour
    {
        RectTransform _rect;
        bool _dragging;
        Vector2 _offset;

        void Start()
        {
            _rect = GetComponent<RectTransform>();
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_rect, Input.mousePosition))
                {
                    Vector3[] corners = new Vector3[4];
                    _rect.GetWorldCorners(corners);

                    // Only drag from header area (top 40px)
                    if (Input.mousePosition.y > corners[1].y - 40f)
                    {
                        _dragging = true;
                        _offset = (Vector2)_rect.position - (Vector2)Input.mousePosition;
                    }
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
