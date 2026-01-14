using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stunlock.Core;
using UnityEngine;

namespace Eclipse.Services;

public static class PrefabCache
{
    private static Dictionary<string, int> _nameToGuid;
    private static bool _initialized;
    private static readonly List<string> _debugLog = new();

    public static void Initialize()
    {
        if (_initialized) return;

        _nameToGuid = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Reflect over the Prefabs class in the mod assembly
            Type prefabsType = typeof(Eclipse.Prefabs);
            FieldInfo[] fields = prefabsType.GetFields(BindingFlags.Public | BindingFlags.Static);

            int count = 0;
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(PrefabGUID))
                {
                    PrefabGUID guid = (PrefabGUID)field.GetValue(null);
                    // Store the field name (e.g., "Item_Ingredient_Gem_Emerald_T01")
                    _nameToGuid[field.Name] = guid.GuidHash;
                    count++;
                }
            }

            _initialized = true;
            Core.Log.LogInfo($"[PrefabCache] Initialized with {count} prefabs.");
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"[PrefabCache] Failed to initialize: {ex.Message}");
        }
    }

    public static int TryFuzzyLookup(string spriteName)
    {
        if (!_initialized) Initialize();
        if (string.IsNullOrEmpty(spriteName)) return 0;

        // 1. Clean the sprite name to get key tokens
        // "Stunlock_Icon_Item_Gem_Emerald1" -> "Gem Emerald1"
        string cleanName = CleanSpriteName(spriteName);
        
        // 2. Exact match check (rare, but good to have)
        if (_nameToGuid.TryGetValue(cleanName, out int exactGuid))
        {
            return exactGuid;
        }

        // 3. Fuzzy search
        // We want to find the key in _nameToGuid that best matches the sprite tokens
        // Sprite: "Gem Emerald1" -> Tokens: ["Gem", "Emerald", "1"]
        // Candidate: "Item_Ingredient_Gem_Emerald_T01" -> Tokens: ["Item", "Ingredient", "Gem", "Emerald", "T01"]
        
        var spriteTokens = Tokenize(cleanName);
        
        int bestMatchGuid = 0;
        int bestScore = 0;
        string bestMatchKey = "";

        // Iterate all keys (expensive on first run, but we only do this on click)
        // Optimization: We could cache these results
        foreach (var kvp in _nameToGuid)
        {
            int score = CalculateMatchScore(spriteTokens, kvp.Key);
            if (score > bestScore)
            {
                bestScore = score;
                bestMatchGuid = kvp.Value;
                bestMatchKey = kvp.Key;
            }
        }

        // Threshold: Must match at least 2 significant tokens or be a very strong single match
        if (bestScore >= 20) // Arbitrary score threshold
        {
            Core.Log.LogInfo($"[PrefabCache] Fuzzy Match: '{spriteName}' -> '{bestMatchKey}' (Score: {bestScore})");
            return bestMatchGuid;
        }

        return 0;
    }

    private static int CalculateMatchScore(List<string> spriteTokens, string prefabKey)
    {
        // prefabKey: "Item_Ingredient_Gem_Emerald_T01"
        string lowerKey = prefabKey.ToLowerInvariant();
        int score = 0;

        foreach (string token in spriteTokens)
        {
            string lowerToken = token.ToLowerInvariant();

            // Exact token match
            if (lowerKey.Contains(lowerToken))
            {
                score += 10;
                continue;
            }

            // Number handling: "1" matches "t01", "01"
            if (int.TryParse(lowerToken, out int num))
            {
                if (lowerKey.Contains($"t0{num}") || lowerKey.Contains($"t{num}"))
                {
                    score += 15; // Strong match for tier/level
                }
            }
        }
        
        // Penalize widely different lengths? No, Prefab names are verbose
        
        return score;
    }

    private static List<string> Tokenize(string input)
    {
        // Split by spaces and underscores
        return input.Split(new[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static string CleanSpriteName(string spriteName)
    {
        string name = spriteName;
        name = name.Replace("Stunlock_Icon_", "");
        name = name.Replace("Stunlock_", "");
        name = name.Replace("Poneti_Icon_", "");
        name = name.Replace("Icon_", "");
        name = name.Replace("Item_", "");
        name = name.Replace("Ingredient_", "");
        name = name.Replace("_Icon", "");
        name = name.Replace("_", " ");
        return name.Trim();
    }
}
