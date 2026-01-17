using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

using BepInEx.Configuration;
using VDebug.Services;

namespace VDebug;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
internal class Plugin : BasePlugin
{
    internal static Plugin Instance { get; private set; }
    internal static ManualLogSource LogInstance => Instance.Log;
    public static ConfigEntry<string> CustomFontName;
    internal static ConfigEntry<bool> EnableAnsiColors;

    public override void Load()
    {
        Instance = this;
        VDebugLog.SetLog(Log);

        CustomFontName = Config.Bind("General", "CustomFontName", "NotoSansMono-Regular", "Name of the font to use. Can be a font in vdebug.bundle or an in-game font.");
        EnableAnsiColors = Config.Bind("Logging", "EnableAnsiColors", true, "If true, VDebug will prefix its logs with ANSI color codes for easier scanning (client/server + warning/error). Disable if your console/log viewer shows escape codes.");

        // Bind structured logging config (repeat suppression, etc.)
        StructuredLogging.BindConfig(Config);
        StructuredLogging.Initialize();

        if (EnableAnsiColors.Value)
        {
            ConsoleAnsiSupport.TryEnable();
            if (!ConsoleAnsiSupport.IsEnabled)
            {
                // Avoid printing raw escape codes in consoles that don't support ANSI.
                EnableAnsiColors.Value = false;
                Log.LogInfo("[VDebug] ANSI color output disabled (console does not support VT/ANSI).");
            }
        }

        if (Application.productName == "VRisingServer")
        {
            // Server mode: keep the API + logging available, but skip any UI/panel initialization.
            Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] loaded (server mode). UI/panel features are disabled on {Application.productName}.");
            return;
        }

        // Initialize the debug panel (will be created when API is called or automatically)
        // Panel initialization is deferred until a canvas is available
        Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] loaded. Call VDebugApi.ShowDebugPanel() to activate.");
    }
}
