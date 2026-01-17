using HarmonyLib;
using ProjectM;
using UnityEngine;
using Eclipse.Services;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class InputActionSystemPatch
{
    public static bool IsGamepad => _isGamepad;
    static bool _isGamepad;

    /*
    [HarmonyPatch(typeof(InputActionSystem), nameof(InputActionSystem.OnInputDeviceChange))]
    [HarmonyPostfix]
    static void OnInputDeviceChangePostfix(InputActionSystem __instance, InputDevice device, InputDeviceChange change)
    {
        DebugToolsBridge.TryLogWarning($"Input device changed: {device.name}, Change type: {change}");
        string deviceName = device.name.ToLower();

        if (deviceName.Contains("gamepad") || deviceName.Contains("controller") || deviceName.Contains("xinput") || deviceName.Contains("dualshock") || deviceName.Contains("ps4") || deviceName.Contains("xbox"))
        {
            if (change.Equals(InputDeviceChange.Removed, InputDeviceChange.Disconnected))
            {
                DebugToolsBridge.TryLogWarning($"Detected keyboard + mouse (gamepad disconnected or removed): {device.name} | {change}");
                CanvasService.HandleAdaptiveElement(false);
            }
            else
            {
                DebugToolsBridge.TryLogWarning($"Detected gamepad: {device.name} | {change}");
                CanvasService.HandleAdaptiveElement(true);
            }
        }
        else
        {
            DebugToolsBridge.TryLogWarning($"Detected keyboard + mouse: {device.name} | {change}");
            CanvasService.HandleAdaptiveElement(false);
        }
    }
    */

    [HarmonyPatch(typeof(InputActionSystem), nameof(InputActionSystem.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(InputActionSystem __instance)
    {
        _isGamepad = __instance.UsingGamepad;

        if (Input.GetKeyDown(KeyCode.F6))
        {
            DebugToolsBridge.TryToggleDebugPanel();
        }
    }
}
