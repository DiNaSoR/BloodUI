using Eclipse.Services;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Eclipse.Services;

/// <summary>
/// Intercepts Ctrl+Click on inventory slots to select items for VAuction.
/// Uses UI raycasting to detect clicked elements.
/// </summary>
internal static class InventoryClickInterceptor
{
    static bool _ctrlHeld;
    static EventSystem _eventSystem;
    static readonly Il2CppSystem.Collections.Generic.List<RaycastResult> _raycastResults = new();

    // Track last logged object to avoid spam
    static string _lastLoggedPath = "";

    /// <summary>
    /// Called from InputActionSystemPatch.OnUpdate to check for Ctrl+Click.
    /// </summary>
    public static void CheckForCtrlClick()
    {
        // Track Ctrl key state
        _ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (!_ctrlHeld || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        // Ctrl+Click detected - try to find what was clicked
        TryProcessClick();
    }

    static void TryProcessClick()
    {
        // Find the EventSystem
        if (_eventSystem == null)
        {
            _eventSystem = EventSystem.current;
        }

        if (_eventSystem == null)
        {
            Core.Log.LogWarning("[InventoryClickInterceptor] No EventSystem found.");
            return;
        }

        // Raycast from mouse position
        PointerEventData pointerData = new(_eventSystem)
        {
            position = Input.mousePosition
        };

        _raycastResults.Clear();
        _eventSystem.RaycastAll(pointerData, _raycastResults);

        if (_raycastResults.Count == 0)
        {
            Core.Log.LogInfo("[InventoryClickInterceptor] No UI element under cursor.");
            return;
        }

        // Log what we're hitting for debugging
        if (_raycastResults.Count > 0)
        {
            var first = _raycastResults[0];
            string path = GetPath(first.gameObject?.transform);
            if (path != _lastLoggedPath)
            {
                _lastLoggedPath = path;
                Core.Log.LogInfo($"[InventoryClickInterceptor] Hit: {path}");
            }
        }

        // Look through results for inventory-related elements
        foreach (var result in _raycastResults)
        {
            if (result.gameObject == null) continue;

            // Get the full hierarchy path
            string path = GetPath(result.gameObject.transform);

            // Check if this is in an inventory context
            bool isInventoryContext = path.Contains("Inventory") ||
                                      path.Contains("EquipmentSlot") ||
                                      path.Contains("ItemSlot") ||
                                      path.Contains("Grid") ||
                                      path.Contains("Container");

            if (!isInventoryContext) continue;

            // Found potential inventory slot - extract item data
            if (ExtractItemData(result.gameObject))
            {
                return; // Process only the first valid hit
            }
        }
    }

    static string GetPath(Transform t)
    {
        if (t == null) return "";
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    static bool ExtractItemData(GameObject slotObject)
    {
        // Search for item info in the slot and its parents
        Transform current = slotObject.transform;

        // First, try to find an Image with a sprite (item icon)
        // The sprite name often contains the item name
        for (int depth = 0; depth < 5 && current != null; depth++)
        {
            // Look for images that might be item icons
            var images = current.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var img in images)
            {
                if (img == null || img.sprite == null) continue;
                string spriteName = img.sprite.name;
                
                // Skip UI sprites
                if (string.IsNullOrWhiteSpace(spriteName)) continue;
                if (spriteName.Contains("Background") || spriteName.Contains("Border")) continue;
                if (spriteName.Contains("Icon_") || spriteName.Contains("Item_"))
                {
                    // This looks like an item sprite - try to extract name
                    string cleanName = CleanSpriteName(spriteName);
                    if (!string.IsNullOrEmpty(cleanName))
                    {
                        Core.Log.LogInfo($"[InventoryClickInterceptor] Sprite: {spriteName} -> {cleanName}");
                        int guid = LookupPrefabGuid(cleanName);
                        if (guid != 0)
                        {
                            VAuctionPopupService.SetSelectedItem(guid, 1, cleanName);
                            return true;
                        }
                    }
                }
            }

            // Look for text that might contain item name
            TextMeshProUGUI[] texts = current.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                if (text == null || string.IsNullOrWhiteSpace(text.text)) continue;
                string txt = text.text.Trim();

                // Skip common non-item texts and placeholders
                if (!IsValidItemName(txt)) continue;

                string itemName = txt;
                int quantity = ExtractQuantity(texts, text);

                // Attempt to find PrefabGUID from LocalizationService
                int prefabGuid = LookupPrefabGuid(itemName);

                if (prefabGuid != 0)
                {
                    Core.Log.LogInfo($"[InventoryClickInterceptor] Selected: {itemName} x{quantity} (GUID: {prefabGuid})");
                    VAuctionPopupService.SetSelectedItem(prefabGuid, quantity, itemName);
                    return true;
                }
                else
                {
                    // Log but only set if it looks like a real item name
                    Core.Log.LogInfo($"[InventoryClickInterceptor] Potential item (no GUID): {itemName}");
                }
            }

            current = current.parent;
        }

        return false;
    }

    static bool IsValidItemName(string txt)
    {
        if (string.IsNullOrWhiteSpace(txt)) return false;
        if (txt.Length < 3) return false;
        if (txt.All(char.IsDigit)) return false;

        // Skip placeholder/template text
        if (txt.Contains("{") || txt.Contains("}")) return false;
        if (txt.StartsWith("<") && txt.EndsWith(">")) return false;

        // Skip common UI text
        string lower = txt.ToLowerInvariant();
        if (lower.Contains("inventory")) return false;
        if (lower.Contains("equipment")) return false;
        if (lower.Contains("empty")) return false;
        if (lower.Contains("slot")) return false;
        if (lower == "x" || lower == "click") return false;

        return true;
    }

    static int ExtractQuantity(TextMeshProUGUI[] texts, TextMeshProUGUI excludeText)
    {
        foreach (var numText in texts)
        {
            if (numText == excludeText || numText == null) continue;
            string numStr = numText.text.Trim();
            if (numStr.StartsWith("x") && int.TryParse(numStr.Substring(1), out int qty))
            {
                return qty;
            }
            else if (int.TryParse(numStr, out int directQty) && directQty > 0 && directQty < 10000)
            {
                return directQty;
            }
        }
        return 1;
    }

    static string CleanSpriteName(string spriteName)
    {
        // Remove common prefixes
        string name = spriteName;
        name = name.Replace("Icon_", "");
        name = name.Replace("Item_", "");
        name = name.Replace("Ingredient_", "");
        name = name.Replace("_Icon", "");

        // Replace underscores with spaces
        name = name.Replace("_", " ");

        return name.Trim();
    }

    static int LookupPrefabGuid(string itemName)
    {
        // Try to find in LocalizationService
        try
        {
            var guid = LocalizationService.GetPrefabGuidFromName(itemName);
            if (guid.HasValue())
            {
                return guid.GuidHash;
            }
        }
        catch (Exception)
        {
            // Ignore lookup errors
        }

        return 0;
    }
}
