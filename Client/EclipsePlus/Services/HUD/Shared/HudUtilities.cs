using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Eclipse.Services.DataService;

namespace Eclipse.Services.HUD.Shared;

/// <summary>
/// Utility methods for HUD components.
/// Contains formatting, conversion, and helper functions.
/// </summary>
internal static class HudUtilities
{
    #region Static Dictionaries

    public static readonly Dictionary<BloodStatType, string> BloodStatTypeAbbreviations = new()
    {
        { BloodStatType.HealingReceived, "HR" },
        { BloodStatType.DamageReduction, "DR" },
        { BloodStatType.PhysicalResistance, "PR" },
        { BloodStatType.SpellResistance, "SR" },
        { BloodStatType.ResourceYield, "RY" },
        { BloodStatType.ReducedBloodDrain, "RBD" },
        { BloodStatType.SpellCooldownRecoveryRate, "SCR" },
        { BloodStatType.WeaponCooldownRecoveryRate, "WCR" },
        { BloodStatType.UltimateCooldownRecoveryRate, "UCR" },
        { BloodStatType.MinionDamage, "MD" },
        { BloodStatType.AbilityAttackSpeed, "AAS" },
        { BloodStatType.CorruptionDamageReduction, "CDR" }
    };

    public static readonly string[] FamiliarStatStringAbbreviations = ["HP", "PP", "SP"];

    public static readonly Dictionary<PlayerClass, Color> ClassColorHexMap = new()
    {
        { PlayerClass.ShadowBlade, new Color(0.6f, 0.1f, 0.9f) },
        { PlayerClass.DemonHunter, new Color(1f, 0.8f, 0f) },
        { PlayerClass.BloodKnight, new Color(1f, 0f, 0f) },
        { PlayerClass.ArcaneSorcerer, new Color(0f, 0.5f, 0.5f) },
        { PlayerClass.VampireLord, new Color(0f, 1f, 1f) },
        { PlayerClass.DeathMage, new Color(0f, 1f, 0f) }
    };

    #endregion

    #region Conversion Methods

    /// <summary>
    /// Converts an integer to a Roman numeral string.
    /// </summary>
    public static string ToRoman(int num)
    {
        string result = string.Empty;

        foreach (var item in HudData.RomanNumerals)
        {
            while (num >= item.Key)
            {
                result += item.Value;
                num -= item.Key;
            }
        }

        return result;
    }

    /// <summary>
    /// Converts an integer to a Roman numeral string (alias for ToRoman).
    /// </summary>
    public static string IntegerToRoman(int num) => ToRoman(num);

    /// <summary>
    /// Formats a class type into a human-readable string with spaces.
    /// </summary>
    public static string FormatClassName(PlayerClass classType)
    {
        return HudData.ClassNameRegex.Replace(classType.ToString(), " $1").Trim();
    }

    #endregion

    #region String Formatting

    /// <summary>
    /// Splits a PascalCase string into words with spaces.
    /// </summary>
    public static string SplitPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return System.Text.RegularExpressions.Regex.Replace(input, "([a-z])([A-Z])", "$1 $2");
    }

    /// <summary>
    /// Trims a string to the first word (or first two words for titles).
    /// </summary>
    public static string TrimToFirstWord(string input)
    {
        int firstSpaceIndex = input.IndexOf(' ');
        int secondSpaceIndex = input.IndexOf(' ', firstSpaceIndex + 1);
        bool isProperTitle = input.StartsWith("Sir") || input.StartsWith("Lord") || input.StartsWith("General") || input.StartsWith("Baron");

        if (firstSpaceIndex > 0 && secondSpaceIndex > 0)
        {
            if (isProperTitle)
                return input[..secondSpaceIndex];

            return input[..firstSpaceIndex];
        }

        return input;
    }

    /// <summary>
    /// Formats a weapon stat value for display in a progress bar.
    /// </summary>
    public static string FormatWeaponStatBar(WeaponStatType weaponStat, float statValue)
    {
        string statValueString = WeaponStatFormats[weaponStat] switch
        {
            "integer" => ((int)statValue).ToString(),
            "decimal" => statValue.ToString("0.#"),
            "percentage" => (statValue * 100f).ToString("0.#") + "%",
            _ => statValue.ToString(),
        };

        return $"<color=#00FFFF>{WeaponStatTypeAbbreviations[weaponStat]}</color>: <color=#90EE90>{statValueString}</color>";
    }

    /// <summary>
    /// Formats an attribute value for display.
    /// </summary>
    public static string FormatAttributeValue(UnitStatType unitStatType, float statValue)
    {
        string statString = $"<color=#90EE90>+{statValue * 100f:F0}%</color>";

        if (Enum.TryParse(unitStatType.ToString(), out WeaponStatType weaponStatType))
            statString = FormatWeaponAttribute(weaponStatType, statValue);

        return statString;
    }

    /// <summary>
    /// Formats a weapon attribute value for display.
    /// </summary>
    public static string FormatWeaponAttribute(WeaponStatType weaponStat, float statValue)
    {
        string statValueString = WeaponStatFormats[weaponStat] switch
        {
            "integer" => ((int)statValue).ToString(),
            "decimal" => statValue.ToString("0.#"),
            "percentage" => (statValue * 100f).ToString("0.#") + "%",
            _ => statValue.ToString(),
        };

        return $"<color=#90EE90>+{statValueString}</color>";
    }

    /// <summary>
    /// Calculates the class synergy multiplier for a given stat type.
    /// </summary>
    public static float ClassSynergy<T>(T statType, PlayerClass classType, Dictionary<PlayerClass, (List<WeaponStatType> WeaponStatTypes, List<BloodStatType> BloodStatTypes)> classStatSynergy)
    {
        if (classType.Equals(PlayerClass.None))
            return 1f;

        if (typeof(T) == typeof(WeaponStatType) && classStatSynergy[classType].WeaponStatTypes.Contains((WeaponStatType)(object)statType))
        {
            return _classStatMultiplier;
        }

        if (typeof(T) == typeof(BloodStatType) && classStatSynergy[classType].BloodStatTypes.Contains((BloodStatType)(object)statType))
        {
            return _classStatMultiplier;
        }

        return 1f;
    }

    /// <summary>
    /// Finds and caches sprites used by HUD components.
    /// </summary>
    public static void FindSprites()
    {
        Il2CppArrayBase<Sprite> sprites = UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>();

        foreach (Sprite sprite in sprites)
        {
            if (HudData.SpriteNames.Contains(sprite.name) && !HudData.Sprites.ContainsKey(sprite.name))
            {
                HudData.Sprites[sprite.name] = sprite;

                if (sprite.name.Equals("BloodIcon_Cursed") && HudData.QuestKillVBloodUnit == null)
                {
                    HudData.QuestKillVBloodUnit = sprite;
                }

                if (sprite.name.Equals("BloodIcon_Warrior") && HudData.QuestKillStandardUnit == null)
                {
                    HudData.QuestKillStandardUnit = sprite;
                }
            }
        }
    }

    /// <summary>
    /// Gets the color for a player class.
    /// </summary>
    public static Color GetClassColor(PlayerClass classType)
    {
        return HudData.ClassColorHexMap.TryGetValue(classType, out var color) ? color : Color.white;
    }

    /// <summary>
    /// Gets the icon name for a profession.
    /// </summary>
    public static string GetProfessionIcon(Profession profession)
    {
        return HudData.ProfessionIcons.TryGetValue(profession, out var icon) ? icon : string.Empty;
    }

    #endregion
}
