using VRiseUI.Services;

namespace VRiseUI.Core;

/// <summary>
/// Core initialization and lifecycle management for VRiseUI.
/// </summary>
public static class UICore
{
    private static bool _initialized;

    /// <summary>
    /// Whether VRiseUI has been initialized and is ready.
    /// </summary>
    public static bool IsInitialized => _initialized;

    /// <summary>
    /// Initialize all VRiseUI systems.
    /// </summary>
    public static void Initialize()
    {
        Log.Trace("UICore.Initialize");

        if (_initialized)
        {
            Log.Warning("UICore", "Already initialized, skipping.");
            return;
        }

        Log.Info("UICore", "Initializing VRiseUI Core...");

        // Initialize layout service (load saved positions)
        Log.Debug("UICore", "Initializing LayoutService...");
        Persistence.LayoutService.Initialize();

        // Initialize asset loader
        Log.Debug("UICore", "Initializing AssetLoader...");
        AssetLoader.Initialize();

        // Initialize chat sender
        Log.Debug("UICore", "Initializing ChatSender...");
        Patches.ChatSender.Initialize();

        // Initialize adapter manager (discovers and loads adapters)
        Log.Debug("UICore", "Initializing AdapterManager...");
        AdapterManager.Initialize();

        // Initialize UI orchestrator
        Log.Debug("UICore", "Initializing UIOrchestrator...");
        UIOrchestrator.Initialize();

        // Initialize our own independent canvas (not hooked to game UI)
        Log.Debug("UICore", "Initializing CanvasManager (independent canvas)...");
        CanvasManager.Initialize();

        // Initialize input patch (F1 toggle - using Harmony for IL2CPP compatibility)
        Log.Debug("UICore", "Initializing InputPatch...");
        Patches.InputPatch.Initialize();

        _initialized = true;
        Log.Info("UICore", "VRiseUI Core initialized. Press F1 to toggle UI.");
        Log.TraceExit("UICore.Initialize");
    }

    /// <summary>
    /// Shutdown all VRiseUI systems.
    /// </summary>
    public static void Shutdown()
    {
        Log.Trace("UICore.Shutdown");

        if (!_initialized)
        {
            Log.Debug("UICore", "Not initialized, nothing to shutdown.");
            return;
        }

        Log.Info("UICore", "Shutting down VRiseUI...");

        CanvasManager.Shutdown();
        UIOrchestrator.Shutdown();
        AdapterManager.Shutdown();
        AssetLoader.Shutdown();

        _initialized = false;
        Log.Info("UICore", "VRiseUI shutdown complete.");
        Log.TraceExit("UICore.Shutdown");
    }
}
