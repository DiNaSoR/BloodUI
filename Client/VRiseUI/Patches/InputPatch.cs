using HarmonyLib;
using ProjectM.UI;
using UnityEngine;
using VRiseUI.Core;
using VRiseUI.Services;

namespace VRiseUI.Patches;

/// <summary>
/// Patches UICanvasSystem.UpdateHideIfDisabled to handle VRiseUI input.
/// This runs every frame when the UI is active, making it reliable for input detection.
/// </summary>
[HarmonyPatch]
public static class InputPatch
{
    private static bool _uiVisible = false;
    private static int _updateCount = 0;
    private static float _lastLogTime = 0;
    private static bool _initialized = false;
    private static bool _loggedFirstUpdate = false;

    /// <summary>
    /// Whether the UI is currently visible.
    /// </summary>
    public static bool IsUIVisible => _uiVisible;

    /// <summary>
    /// Initialize the input patch.
    /// </summary>
    public static void Initialize()
    {
        _initialized = true;
        Log.Info("InputPatch", "Input patch initialized. Press F1 to toggle UI, F2 for status, F3 to force show.");
    }

    /// <summary>
    /// Hook into UICanvasSystem.UpdateHideIfDisabled for per-frame input detection.
    /// This runs every frame when in-game with UI active.
    /// </summary>
    [HarmonyPatch(typeof(UICanvasSystem), nameof(UICanvasSystem.UpdateHideIfDisabled))]
    [HarmonyPostfix]
    public static void OnCanvasUpdate_Postfix(UICanvasBase canvas)
    {
        if (!_initialized) return;

        _updateCount++;

        // Log first update to confirm hook is working
        if (!_loggedFirstUpdate)
        {
            _loggedFirstUpdate = true;
            Log.Info("InputPatch", $"First update! Input detection active. Frame: {_updateCount}");
        }

        // Log update periodically to confirm it's being called
        if (Time.time - _lastLogTime > 30f)
        {
            Log.Debug("InputPatch", $"Update running. Frame: {_updateCount}, Time: {Time.time:F1}");
            _lastLogTime = Time.time;
        }

        // F1 = Toggle VRiseUI
        if (UnityEngine.Input.GetKeyDown(KeyCode.F1))
        {
            Log.Info("InputPatch", ">>> F1 PRESSED! <<<");
            ToggleUI();
        }

        // F2 = Status dump
        if (UnityEngine.Input.GetKeyDown(KeyCode.F2))
        {
            Log.Info("InputPatch", ">>> F2 PRESSED! <<< Status dump:");
            DumpStatus();
        }

        // F3 = Force show UI
        if (UnityEngine.Input.GetKeyDown(KeyCode.F3))
        {
            Log.Info("InputPatch", ">>> F3 PRESSED! <<< Force showing UI!");
            ForceShowUI();
        }

        // F4 = Test key (just to verify input works)
        if (UnityEngine.Input.GetKeyDown(KeyCode.F4))
        {
            Log.Info("InputPatch", ">>> F4 TEST KEY WORKS! <<<");
        }
    }

    private static void ToggleUI()
    {
        _uiVisible = !_uiVisible;

        Log.Info("========================================");
        Log.Info($"VRiseUI: UI {(_uiVisible ? "SHOWN" : "HIDDEN")}");
        Log.Info("========================================");

        // Use CanvasManager (our independent canvas)
        CanvasManager.SetVisible(_uiVisible);

        DumpStatus();
        Log.Info("========================================");
    }

    private static void ForceShowUI()
    {
        _uiVisible = true;
        CanvasManager.SetVisible(true);
        Log.Info("InputPatch", "UI forced to VISIBLE");
        DumpStatus();
    }

    public static void DumpStatus()
    {
        Log.Info("Status", $"UI Visible: {_uiVisible}");
        Log.Info("Status", $"Canvas Ready: {CanvasManager.IsReady}");
        Log.Info("Status", $"HudRoot: {(CanvasManager.HudRoot != null ? "EXISTS" : "NULL")}");
        Log.Info("Status", $"Components: {UIOrchestrator.ComponentCount}");
        Log.Info("Status", $"Update Frames: {_updateCount}");

        // Log adapter info
        var adapters = AdapterManager.Adapters;
        if (adapters.Count == 0)
        {
            Log.Warning("Status", "No adapters loaded!");
        }
        else
        {
            Log.Info("Status", $"Loaded Adapters ({adapters.Count}):");
            foreach (var adapter in adapters.Values)
            {
                var manifest = adapter.Manifest;
                var name = manifest?.DisplayName ?? adapter.Id;
                var elements = manifest?.Hud?.Elements?.Count ?? 0;
                Log.Info("Status", $"  • {name} ({elements} elements)");
            }
        }
    }

    /// <summary>
    /// Set UI visibility directly.
    /// </summary>
    public static void SetVisible(bool visible)
    {
        Log.Debug("InputPatch", $"SetVisible({visible})");
        _uiVisible = visible;
        CanvasManager.SetVisible(visible);
    }
}
