using HarmonyLib;
using ProjectM.UI;
using VRiseUI.Data;

namespace VRiseUI.Patches;

/// <summary>
/// Harmony patches to intercept chat messages for command responses.
/// </summary>
[HarmonyPatch]
public static class ChatPatches
{
    /// <summary>
    /// Patch to intercept incoming system chat messages.
    /// </summary>
    [HarmonyPatch(typeof(ClientChatSystem), nameof(ClientChatSystem.OnUpdate))]
    [HarmonyPostfix]
    public static void OnChatUpdate_Postfix(ClientChatSystem __instance)
    {
        // This is a placeholder - actual implementation depends on game version
        // The idea is to capture system messages that are responses to our commands
    }
}

/// <summary>
/// Helper to send chat messages through the game's input system.
/// </summary>
public static class ChatSender
{
    private static bool _initialized;

    /// <summary>
    /// Initialize the chat sender.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        Plugin.Log.LogInfo("Chat sender initialized.");
    }

    /// <summary>
    /// Send a chat message as if the player typed it.
    /// </summary>
    public static void SendMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        try
        {
            // Use the game's chat input system
            // This depends on finding the active ChatEntry or InputField
            Plugin.Log.LogInfo($"[CHAT] Sending: {message}");

            // TODO: Hook into actual chat system
            // For now, route through CommandBridge for response handling
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Failed to send chat message: {ex.Message}");
        }
    }
}
