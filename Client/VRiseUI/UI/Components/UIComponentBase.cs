using VRiseUI.Adapters.Models;
using VRiseUI.Data;
using VRiseUI.UI.Interfaces;
using UnityEngine;

namespace VRiseUI.UI.Components;

/// <summary>
/// Base class for all UI components.
/// Provides common functionality inherited by all components.
/// </summary>
public abstract class UIComponentBase : IUIComponent
{
    protected readonly Adapter _adapter;
    protected readonly HudElementDef _definition;
    protected GameObject _gameObject;
    protected RectTransform _rectTransform;
    protected DataBinding _dataBinding;
    protected bool _isReady;
    protected bool _isVisible = true;

    public string ComponentId { get; }
    public string AdapterId => _adapter.Id;
    public bool IsEnabled { get; set; } = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            if (_gameObject != null)
                _gameObject.SetActive(value);
        }
    }
    public bool IsReady => _isReady;

    protected UIComponentBase(Adapter adapter, HudElementDef definition)
    {
        _adapter = adapter;
        _definition = definition;
        ComponentId = $"{adapter.Id}.{definition.Type}.{definition.Id}";
    }

    /// <summary>
    /// Initialize the component. Override in subclasses.
    /// </summary>
    public virtual void Initialize()
    {
        // Create base GameObject
        _gameObject = new GameObject(ComponentId);
        _rectTransform = _gameObject.AddComponent<RectTransform>();

        // Parent to our independent canvas HUD root
        var hudRoot = Core.CanvasManager.HudRoot;
        if (hudRoot != null)
        {
            _gameObject.transform.SetParent(hudRoot, false);
        }

        // Apply position and size from definition
        ApplyLayout();

        // Load saved position if exists
        if (Persistence.LayoutService.TryGetPosition(_adapter.Id, _definition.Id, out var savedPos))
        {
            _rectTransform.anchoredPosition = savedPos;
        }

        // Let subclasses create their specific UI
        CreateUI();

        // Setup data binding if defined
        if (_definition.DataSource != null)
        {
            _dataBinding = new DataBinding(_definition.DataSource, OnDataUpdated);
            _dataBinding.Start();
        }

        _isReady = true;
        Plugin.Log.LogInfo($"Initialized component: {ComponentId}");
    }

    /// <summary>
    /// Called when bound data is updated. Override in subclasses.
    /// </summary>
    protected virtual void OnDataUpdated(Dictionary<string, object> data) { }

    /// <summary>
    /// Apply position and size from definition.
    /// </summary>
    protected virtual void ApplyLayout()
    {
        if (_definition.Position != null)
        {
            _rectTransform.anchoredPosition = new Vector2(
                _definition.Position.X,
                -_definition.Position.Y  // Unity Y is inverted
            );

            // Set anchor based on definition
            SetAnchor(_definition.Position.Anchor);
        }

        if (_definition.Size != null)
        {
            _rectTransform.sizeDelta = new Vector2(
                _definition.Size.Width,
                _definition.Size.Height
            );
        }
    }

    /// <summary>
    /// Set the anchor preset.
    /// </summary>
    protected void SetAnchor(string anchor)
    {
        switch (anchor?.ToLowerInvariant())
        {
            case "topleft":
                _rectTransform.anchorMin = new Vector2(0, 1);
                _rectTransform.anchorMax = new Vector2(0, 1);
                _rectTransform.pivot = new Vector2(0, 1);
                break;
            case "topright":
                _rectTransform.anchorMin = new Vector2(1, 1);
                _rectTransform.anchorMax = new Vector2(1, 1);
                _rectTransform.pivot = new Vector2(1, 1);
                break;
            case "bottomleft":
                _rectTransform.anchorMin = new Vector2(0, 0);
                _rectTransform.anchorMax = new Vector2(0, 0);
                _rectTransform.pivot = new Vector2(0, 0);
                break;
            case "bottomright":
                _rectTransform.anchorMin = new Vector2(1, 0);
                _rectTransform.anchorMax = new Vector2(1, 0);
                _rectTransform.pivot = new Vector2(1, 0);
                break;
            case "center":
                _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _rectTransform.pivot = new Vector2(0.5f, 0.5f);
                break;
            default:
                // Default to top-left
                _rectTransform.anchorMin = new Vector2(0, 1);
                _rectTransform.anchorMax = new Vector2(0, 1);
                _rectTransform.pivot = new Vector2(0, 1);
                break;
        }
    }

    /// <summary>
    /// Create the specific UI elements. Override in subclasses.
    /// </summary>
    protected abstract void CreateUI();

    /// <summary>
    /// Update the component. Override in subclasses.
    /// </summary>
    public virtual void Update() { }

    /// <summary>
    /// Destroy the component and cleanup.
    /// </summary>
    public virtual void Destroy()
    {
        if (_gameObject != null)
        {
            UnityEngine.Object.Destroy(_gameObject);
            _gameObject = null;
        }
        _isReady = false;
    }

    /// <summary>
    /// Toggle visibility.
    /// </summary>
    public void Toggle()
    {
        IsVisible = !IsVisible;
    }

    /// <summary>
    /// Handle input device changes.
    /// </summary>
    public virtual void OnInputDeviceChanged(bool isGamepad) { }

    /// <summary>
    /// Get the underlying GameObject for this component.
    /// </summary>
    public GameObject GetGameObject() => _gameObject;

    /// <summary>
    /// Parse a color string to Unity Color.
    /// </summary>
    protected Color ParseColor(string colorString, Color defaultColor)
    {
        if (string.IsNullOrEmpty(colorString))
            return defaultColor;

        if (ColorUtility.TryParseHtmlString(colorString, out var color))
            return color;

        return defaultColor;
    }
}
