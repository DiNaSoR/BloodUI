using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.UI;
using VRiseUI.Services;

namespace VRiseUI.Core;

/// <summary>
/// Manages VRiseUI's independent Canvas.
/// Creates a ScreenSpaceOverlay canvas that is completely separate from the game's UI.
/// </summary>
public class CanvasManager : MonoBehaviour
{
    private static CanvasManager _instance;
    private static Canvas _canvas;
    private static CanvasScaler _scaler;
    private static GraphicRaycaster _raycaster;
    private static RectTransform _hudRoot;
    private static bool _initialized;

    /// <summary>
    /// The VRiseUI canvas.
    /// </summary>
    public static Canvas Canvas => _canvas;

    /// <summary>
    /// Root transform for all HUD elements.
    /// </summary>
    public static Transform HudRoot => _hudRoot;

    /// <summary>
    /// Whether the canvas is ready.
    /// </summary>
    public static bool IsReady => _initialized && _canvas != null;

    static CanvasManager()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CanvasManager>();
    }

    public CanvasManager(IntPtr ptr) : base(ptr) { }

    /// <summary>
    /// Initialize the canvas system.
    /// </summary>
    public static void Initialize()
    {
        Log.Trace("CanvasManager.Initialize");

        if (_initialized)
        {
            Log.Debug("CanvasManager", "Already initialized.");
            return;
        }

        try
        {
            // Create the root GameObject
            Log.Debug("CanvasManager", "Creating VRiseUI Canvas...");
            var canvasGo = new GameObject("VRiseUI_Canvas");
            UnityEngine.Object.DontDestroyOnLoad(canvasGo);

            // Add CanvasManager component
            _instance = canvasGo.AddComponent<CanvasManager>();

            // Create Canvas component
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1000; // High value to render on top

            // Add CanvasScaler for resolution independence
            _scaler = canvasGo.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920, 1080);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.matchWidthOrHeight = 0.5f;

            // Add GraphicRaycaster for UI interaction
            _raycaster = canvasGo.AddComponent<GraphicRaycaster>();

            Log.Info("CanvasManager", $"Canvas created: {canvasGo.name}, RenderMode: ScreenSpaceOverlay, SortingOrder: 1000");

            // Create HUD root container
            var hudRootGo = new GameObject("HUD_Root");
            hudRootGo.transform.SetParent(canvasGo.transform, false);
            _hudRoot = hudRootGo.AddComponent<RectTransform>();
            _hudRoot.anchorMin = Vector2.zero;
            _hudRoot.anchorMax = Vector2.one;
            _hudRoot.sizeDelta = Vector2.zero;
            _hudRoot.anchoredPosition = Vector2.zero;

            Log.Info("CanvasManager", "HUD_Root created.");

            _initialized = true;

            // Load adapters and create components
            LoadAdaptersAndCreateUI();

            Log.Info("CanvasManager", "Canvas system initialized successfully!");
            Log.TraceExit("CanvasManager.Initialize");
        }
        catch (Exception ex)
        {
            Log.Error("CanvasManager", $"Failed to initialize: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Load adapters and create UI components.
    /// </summary>
    private static void LoadAdaptersAndCreateUI()
    {
        Log.Debug("CanvasManager", "Loading adapters and creating UI...");

        foreach (var adapter in AdapterManager.Adapters.Values)
        {
            try
            {
                Log.Info("CanvasManager", $"Loading adapter: {adapter.Manifest?.DisplayName ?? adapter.Id}");
                adapter.Load();
            }
            catch (Exception ex)
            {
                Log.Error("CanvasManager", $"Failed to load adapter {adapter.Id}: {ex.Message}");
            }
        }

        // Initialize all registered components
        UIOrchestrator.InitializeComponents();

        // Start hidden - press F1 to show
        SetVisible(false);

        Log.Info("CanvasManager", $"UI creation complete. Components: {UIOrchestrator.ComponentCount}. Press F1 to show.");
    }

    /// <summary>
    /// Show or hide the entire UI.
    /// </summary>
    public static void SetVisible(bool visible)
    {
        if (_hudRoot != null)
        {
            _hudRoot.gameObject.SetActive(visible);
            Log.Debug("CanvasManager", $"UI visibility set to: {visible}");
        }
    }

    /// <summary>
    /// Shutdown the canvas system.
    /// </summary>
    public static void Shutdown()
    {
        Log.Trace("CanvasManager.Shutdown");

        if (_instance != null)
        {
            UnityEngine.Object.Destroy(_instance.gameObject);
            _instance = null;
        }

        _canvas = null;
        _scaler = null;
        _raycaster = null;
        _hudRoot = null;
        _initialized = false;

        Log.Debug("CanvasManager", "Canvas system shutdown.");
        Log.TraceExit("CanvasManager.Shutdown");
    }
}
