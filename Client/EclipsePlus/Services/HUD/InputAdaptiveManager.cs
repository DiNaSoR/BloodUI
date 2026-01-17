using Eclipse.Patches;
using ProjectM;
using System.Collections.Generic;
using UnityEngine;
using Eclipse.Services;

namespace Eclipse.Services.HUD;

/// <summary>
/// Manages input device detection and UI element adaptation.
/// Extracted from CanvasService.InputHUD.
/// </summary>
internal static class InputAdaptiveManager
{
    public static bool IsGamepad => InputActionSystemPatch.IsGamepad;
    static ControllerType _inputDevice = ControllerType.KeyboardAndMouse;

    public readonly struct InputAdaptiveElement
    {
        public readonly GameObject AdaptiveObject;

        public readonly Vector2 KeyboardMouseAnchoredPosition;
        public readonly Vector2 KeyboardMouseAnchorMin;
        public readonly Vector2 KeyboardMouseAnchorMax;
        public readonly Vector2 KeyboardMousePivot;
        public readonly Vector3 KeyboardMouseScale;

        public readonly Vector2 ControllerAnchoredPosition;
        public readonly Vector2 ControllerAnchorMin;
        public readonly Vector2 ControllerAnchorMax;
        public readonly Vector2 ControllerPivot;
        public readonly Vector3 ControllerScale;

        public InputAdaptiveElement(
            GameObject adaptiveObject,
            Vector2 keyboardMousePos,
            Vector2 keyboardMouseAnchorMin,
            Vector2 keyboardMouseAnchorMax,
            Vector2 keyboardMousePivot,
            Vector3 keyboardMouseScale,
            Vector2 controllerPos,
            Vector2 controllerAnchorMin,
            Vector2 controllerAnchorMax,
            Vector2 controllerPivot,
            Vector3 controllerScale)
        {
            AdaptiveObject = adaptiveObject;
            KeyboardMouseAnchoredPosition = keyboardMousePos;
            KeyboardMouseAnchorMin = keyboardMouseAnchorMin;
            KeyboardMouseAnchorMax = keyboardMouseAnchorMax;
            KeyboardMousePivot = keyboardMousePivot;
            KeyboardMouseScale = keyboardMouseScale;
            ControllerAnchoredPosition = controllerPos;
            ControllerAnchorMin = controllerAnchorMin;
            ControllerAnchorMax = controllerAnchorMax;
            ControllerPivot = controllerPivot;
            ControllerScale = controllerScale;
        }
    }

    public static readonly List<InputAdaptiveElement> AdaptiveElements = [];

    public static void SyncInputHUD()
    {
        bool isSynced = IsGamepad
            ? _inputDevice.Equals(ControllerType.Gamepad)
            : _inputDevice.Equals(ControllerType.KeyboardAndMouse);

        if (!isSynced)
            SyncAdaptiveElements(IsGamepad);
    }

    public static void RegisterAdaptiveElement(
        GameObject adaptiveObject,
        Vector2 keyboardMousePos,
        Vector2 keyboardMouseAnchorMin,
        Vector2 keyboardMouseAnchorMax,
        Vector2 keyboardMousePivot,
        Vector3 keyboardMouseScale,
        Vector2 controllerPos,
        Vector2 controllerAnchorMin,
        Vector2 controllerAnchorMax,
        Vector2 controllerPivot,
        Vector3 controllerScale)
    {
        if (adaptiveObject == null) return;

        AdaptiveElements.Add(new InputAdaptiveElement(
            adaptiveObject,
            keyboardMousePos,
            keyboardMouseAnchorMin,
            keyboardMouseAnchorMax,
            keyboardMousePivot,
            keyboardMouseScale,
            controllerPos,
            controllerAnchorMin,
            controllerAnchorMax,
            controllerPivot,
            controllerScale));
    }

    public static void SyncAdaptiveElements(bool isGamepad)
    {
        _inputDevice = isGamepad ? ControllerType.Gamepad : ControllerType.KeyboardAndMouse;
        DebugToolsBridge.TryLogWarning($"[OnInputDeviceChanged] - ControllerType: {_inputDevice}");

        foreach (InputAdaptiveElement adaptiveElement in AdaptiveElements)
        {
            if (adaptiveElement.AdaptiveObject == null) continue;

            RectTransform rectTransform = adaptiveElement.AdaptiveObject.GetComponent<RectTransform>();
            if (rectTransform == null) continue;

            if (isGamepad)
            {
                rectTransform.anchorMin = adaptiveElement.ControllerAnchorMin;
                rectTransform.anchorMax = adaptiveElement.ControllerAnchorMax;
                rectTransform.pivot = adaptiveElement.ControllerPivot;
                rectTransform.anchoredPosition = adaptiveElement.ControllerAnchoredPosition;
                rectTransform.localScale = adaptiveElement.ControllerScale;
            }
            else
            {
                rectTransform.anchorMin = adaptiveElement.KeyboardMouseAnchorMin;
                rectTransform.anchorMax = adaptiveElement.KeyboardMouseAnchorMax;
                rectTransform.pivot = adaptiveElement.KeyboardMousePivot;
                rectTransform.anchoredPosition = adaptiveElement.KeyboardMouseAnchoredPosition;
                rectTransform.localScale = adaptiveElement.KeyboardMouseScale;
            }
        }

        LayoutService.ApplyLayoutsForInput(isGamepad);
    }

    public static void Reset()
    {
        AdaptiveElements.Clear();
        _inputDevice = ControllerType.KeyboardAndMouse;
    }
}
