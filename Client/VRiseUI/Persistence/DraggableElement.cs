using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VRiseUI.Persistence;

/// <summary>
/// MonoBehaviour component for making UI elements draggable.
/// Note: Uses Update-based dragging instead of interfaces for IL2CPP compatibility.
/// </summary>
public class DraggableElement : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Vector2 _dragOffset;
    private bool _isDragging;
    private Camera _camera;

    // Component reference for saving
    private string _adapterId;
    private string _elementId;

    /// <summary>
    /// Whether dragging is enabled.
    /// </summary>
    public bool IsDraggable { get; set; } = true;

    /// <summary>
    /// Event raised when dragging ends.
    /// </summary>
    public event Action<Vector2> OnPositionChanged;

    static DraggableElement()
    {
        // Register with IL2CPP
        ClassInjector.RegisterTypeInIl2Cpp<DraggableElement>();
    }

    public DraggableElement(IntPtr ptr) : base(ptr) { }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// Setup the draggable element.
    /// </summary>
    public void Setup(string adapterId, string elementId)
    {
        _adapterId = adapterId;
        _elementId = elementId;
    }

    private void Update()
    {
        if (!IsDraggable) return;

        // Get camera for screen to canvas conversion
        if (_camera == null && _canvas != null)
        {
            _camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay 
                ? null 
                : _canvas.worldCamera;
        }

        // Handle mouse-based dragging
        if (UnityEngine.Input.GetMouseButtonDown(0))
        {
            TryBeginDrag();
        }
        else if (UnityEngine.Input.GetMouseButton(0) && _isDragging)
        {
            OnDrag();
        }
        else if (UnityEngine.Input.GetMouseButtonUp(0) && _isDragging)
        {
            OnEndDrag();
        }
    }

    private void TryBeginDrag()
    {
        if (_rectTransform == null) return;

        // Check if mouse is over this element
        var mousePos = UnityEngine.Input.mousePosition;
        if (RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, mousePos, _camera))
        {
            _isDragging = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform.parent as RectTransform,
                mousePos,
                _camera,
                out var localPoint
            );
            _dragOffset = _rectTransform.anchoredPosition - localPoint;
        }
    }

    private void OnDrag()
    {
        if (_rectTransform?.parent == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform.parent as RectTransform,
            UnityEngine.Input.mousePosition,
            _camera,
            out var localPoint
        );

        _rectTransform.anchoredPosition = localPoint + _dragOffset;
    }

    private void OnEndDrag()
    {
        _isDragging = false;

        // Save the new position
        var newPosition = _rectTransform.anchoredPosition;
        OnPositionChanged?.Invoke(newPosition);

        // Persist to layout service
        if (!string.IsNullOrEmpty(_adapterId) && !string.IsNullOrEmpty(_elementId))
        {
            LayoutService.SavePosition(_adapterId, _elementId, newPosition);
        }
    }
}
