using Eclipse.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Stunlock.Core;

namespace Eclipse.Services;

/// <summary>
/// Intercepts Ctrl+Click on inventory slots to select items for VAuction.
/// Only examines the SPECIFIC clicked slot, not the entire inventory.
/// </summary>
internal static class InventoryClickInterceptor
{
    static EventSystem _eventSystem;
    static readonly Il2CppSystem.Collections.Generic.List<RaycastResult> _raycastResults = new();


    /// <summary>
    /// Called from InputActionSystemPatch.OnUpdate to check for Ctrl+Click.
    /// </summary>
    public static void CheckForCtrlClick()
    {
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (!ctrlHeld || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        // Ctrl+Click detected - try to find what was clicked
        TryProcessClick();
    }

    static void TryProcessClick()
    {
        if (_eventSystem == null)
        {
            _eventSystem = EventSystem.current;
        }

        if (_eventSystem == null)
        {
            return;
        }

        PointerEventData pointerData = new(_eventSystem)
        {
            position = Input.mousePosition
        };

        _raycastResults.Clear();
        _eventSystem.RaycastAll(pointerData, _raycastResults);

        if (_raycastResults.Count == 0)
        {
            return;
        }

        // Find the inventory slot that was clicked
        foreach (var result in _raycastResults)
        {
            if (result.gameObject == null) continue;

            // Walk up to find ContainerSlot(Clone) - that's the item slot
            Transform slotTransform = FindContainerSlot(result.gameObject.transform);
            if (slotTransform == null) continue;

            // Extract from ONLY this specific slot
            if (ExtractFromSlot(slotTransform))
            {
                return;
            }
        }
    }

    /// <summary>
    /// Walk up the hierarchy to find the ContainerSlot(Clone) parent
    /// </summary>
    static Transform FindContainerSlot(Transform t)
    {
        Transform current = t;
        int depth = 0;

        while (current != null && depth < 10)
        {
            if (current.name.Contains("ContainerSlot") ||
                current.name.Contains("EquipmentSlot") ||
                current.name.Contains("ItemSlot"))
            {
                return current;
            }
            current = current.parent;
            depth++;
        }

        return null;
    }

    /// <summary>
    /// Extract item name and quantity from ONLY this specific slot's direct children
    /// </summary>
    static bool ExtractFromSlot(Transform slotTransform)
    {
        string rawSpriteName = null;
        string displayName = null;
        int prefabGuid = 0;
        int quantity = 1;

        // Look for ItemIcon child - the main item image
        Transform itemIcon = slotTransform.Find("ItemIcon");
        if (itemIcon == null)
        {
            // Try to find by searching immediate children
            for (int i = 0; i < slotTransform.childCount; i++)
            {
                var child = slotTransform.GetChild(i);
                if (child.name.Contains("Icon") || child.name.Contains("Image"))
                {
                    itemIcon = child;
                    break;
                }
            }
        }

        if (itemIcon != null)
        {
            // Get the sprite name from the item icon
            var img = itemIcon.GetComponent<UnityEngine.UI.Image>();
            if (img != null && img.sprite != null)
            {
                rawSpriteName = img.sprite.name;
                
                // Use the new Cache to find the GUID
                prefabGuid = PrefabCache.TryFuzzyLookup(rawSpriteName);
                
                // Display name is just the cleaned sprite name for UI
                displayName = CleanSpriteName(rawSpriteName);
            }
        }

        // Look for quantity text
        Transform amountText = slotTransform.Find("ItemText") ?? slotTransform.Find("AmountText") ?? slotTransform.Find("Amount");
        if (amountText != null)
        {
            var tmp = amountText.GetComponent<TextMeshProUGUI>();
            if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
            {
                string txt = tmp.text.Trim();
                if (int.TryParse(txt, out int q) && q > 0)
                {
                    quantity = q;
                }
            }
        }
        else
        {
            // Search only DIRECT children of the slot for quantity text
            for (int i = 0; i < slotTransform.childCount; i++)
            {
                var child = slotTransform.GetChild(i);
                var tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
                {
                    string txt = tmp.text.Trim();
                    if (int.TryParse(txt, out int q) && q > 0 && q < 10000)
                    {
                        quantity = q;
                        break;
                    }
                }
            }
        }

        // If we didn't find an item name from sprite, the slot may be empty
        if (string.IsNullOrEmpty(displayName))
        {
            return false;
        }

        Core.Log.LogInfo($"[InventoryClickInterceptor] Selected: {displayName} x{quantity} (GUID: {prefabGuid}, Sprite: {rawSpriteName})");
        VAuctionPopupService.SetSelectedItem(prefabGuid, quantity, displayName);
        return true;
    }

    static string CleanSpriteName(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;

        // Skip UI/common sprites
        if (spriteName.Contains("Cross") || spriteName.Contains("Background"))
            return null;
        if (spriteName.Contains("Border") || spriteName.Contains("Damaged"))
            return null;
        if (spriteName.Contains("Blood") || spriteName.Contains("Slash"))
            return null;

        string name = spriteName;

        // Remove common prefixes
        name = name.Replace("Stunlock_Icon_", "");
        name = name.Replace("Stunlock_", "");
        name = name.Replace("Poneti_Icon_", "");
        name = name.Replace("Icon_", "");
        name = name.Replace("Item_", "");
        name = name.Replace("Ingredient_", "");
        name = name.Replace("_Icon", "");

        // Replace underscores with spaces
        name = name.Replace("_", " ");

        return name.Trim();
    }
}
