using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using VampireCommandFramework;

namespace VAuction;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
internal class Plugin : BasePlugin
{
    Harmony _harmony;
    internal static Plugin Instance { get; private set; }
    internal static ManualLogSource LogInstance => Instance.Log;
    internal static bool CommandsRegistered;
    internal static int CommandRegisterAttempts;

    public override void Load()
    {
        Instance = this;

        if (Application.productName != "VRisingServer")
        {
            LogInstance.LogInfo($"{MyPluginInfo.PLUGIN_NAME} is a server mod and will not load on client.");
            return;
        }

        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

        LogInstance.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] loaded (awaiting init/command registration).");

        // VAuction may load before the Server world is available (depends on server startup order).
        // Delay initialization until the Server world exists, then register commands.
        Core.BeginInitializeWithRetry(() =>
        {
            try
            {
                Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] initialized (waiting for VCF).");
            }
            catch (Exception ex)
            {
                LogInstance.LogError($"[VAuction] Failed to register commands: {ex}");
            }
        });
    }

    public override bool Unload()
    {
        Config.Clear();
        _harmony.UnpatchSelf();
        return true;
    }
}

