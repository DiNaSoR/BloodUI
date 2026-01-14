using HarmonyLib;
using ProjectM;
using VampireCommandFramework;

namespace VAuction.Patches;

/// <summary>
/// Server-side safety net: retries VAuction init and VCF command registration on server update.
/// This avoids relying on coroutines/timing during chainloader startup.
/// </summary>
[HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUpdate))]
internal static class ServerBootstrapSystemPatch
{
    static bool _loggedWaitingForVcf;
    static double _lastExpireTickServerTime;

    [HarmonyPostfix]
    static void OnUpdatePostfix()
    {
        // Ensure VAuction core is initialized (world is available by this point on most servers).
        if (!Core.TryInitialize())
        {
            return;
        }

        // Register commands once VCF is available. If VCF isn't ready yet, keep retrying.
        if (!Plugin.CommandsRegistered)
        {
            try
            {
                Plugin.CommandRegisterAttempts++;
                CommandRegistry.RegisterAll();
                Plugin.CommandsRegistered = true;
                Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] commands registered (attempt {Plugin.CommandRegisterAttempts}).");
            }
            catch (Exception ex)
            {
                if (!_loggedWaitingForVcf)
                {
                    _loggedWaitingForVcf = true;
                    Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] waiting for VCF to be ready...");
                }

                // Keep retrying; log only at debug-ish level to avoid spam.
                Core.Log.LogDebug($"[VAuction] CommandRegistry.RegisterAll failed: {ex.Message}");
            }
        }

        // Run expirations without coroutines (some server environments fail MonoBehaviour.StartCoroutine).
        TryProcessExpirationsTick();
    }

    static void TryProcessExpirationsTick()
    {
        try
        {
            if (Core.AuctionService == null) return;

            double now = Core.ServerGameManager.ServerTime;
            if (_lastExpireTickServerTime <= 0)
            {
                _lastExpireTickServerTime = now;
                return;
            }

            if (now - _lastExpireTickServerTime < 1.0)
            {
                return;
            }

            _lastExpireTickServerTime = now;
            Core.AuctionService.ProcessExpirations();
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"[VAuction] Expiration tick failed: {ex.Message}");
        }
    }
}

