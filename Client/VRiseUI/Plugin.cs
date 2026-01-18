using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using VRiseUI.Core;

namespace VRiseUI;

/// <summary>
/// VRiseUI - Universal V Rising Mod UI Framework
/// Supports multiple mods via manifest-based adapters
/// 
/// Created by DiNaSoR
/// </summary>
[BepInPlugin(GUID, NAME, VERSION)]
public class Plugin : BasePlugin
{
    public const string GUID = "com.dinasor.vriseui";
    public const string NAME = "VRiseUI";
    public const string VERSION = "0.1.0";

    public new static ManualLogSource Log { get; private set; }
    public static Plugin Instance { get; private set; }

    private Harmony _harmony;

    public override void Load()
    {
        Instance = this;
        Log = base.Log;

        Log.LogInfo($"{NAME} v{VERSION} loading...");

        // Initialize logging system with VDebug integration
        Services.Log.Initialize(Log);
        Services.Log.Info("Plugin", $"{NAME} v{VERSION} loading...");

        // Initialize core systems
        Services.Log.Debug("Plugin", "Initializing UICore...");
        UICore.Initialize();

        // Apply Harmony patches
        Services.Log.Debug("Plugin", "Applying Harmony patches...");
        _harmony = Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, GUID);
        Services.Log.Debug("Plugin", $"Harmony patches applied. Patches: {_harmony.GetPatchedMethods().Count()}");

        Services.Log.Info("Plugin", $"{NAME} loaded successfully! Press F1 to toggle UI.");
        Log.LogInfo($"{NAME} loaded successfully!");
    }

    public override bool Unload()
    {
        _harmony?.UnpatchSelf();
        UICore.Shutdown();
        return true;
    }
}
