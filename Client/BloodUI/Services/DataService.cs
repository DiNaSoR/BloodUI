using Stunlock.Core;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using static BloodUI.Services.CanvasService.DataHUD;
using BloodUI.Services;

namespace BloodUI.Services;
internal static class DataService
{
    private static readonly Regex _regexPrestigeLB = new(@"<color=yellow>(\d+)</color>\| <color=green>(.*?)</color>, <color=#90EE90>(.*?)</color>: <color=white>(\d+)</color>", RegexOptions.Compiled);
    private static readonly string[] LegacyPrestigeTypes = {
        "Experience", "Exo", "SwordExpertise", "AxeExpertise", "MaceExpertise",
        "SpearExpertise", "CrossbowExpertise", "GreatSwordExpertise", "SlashersExpertise",
        "PistolsExpertise", "ReaperExpertise", "LongbowExpertise", "WhipExpertise",
        "UnarmedExpertise", "FishingPoleExpertise", "TwinBladesExpertise", "DaggersExpertise",
        "ClawsExpertise", "WorkerLegacy", "WarriorLegacy", "ScholarLegacy",
        "RogueLegacy", "MutantLegacy", "DraculinLegacy", "ImmortalLegacy",
        "CreatureLegacy", "BruteLegacy", "CorruptionLegacy"
    };
    public enum TargetType
    {
        Kill,
        Craft,
        Gather,
        Fish
    }
    public enum Profession
    {
        Enchanting,
        Alchemy,
        Harvesting,
        Blacksmithing,
        Tailoring,
        Woodcutting,
        Mining,
        Fishing
    }
    public enum PlayerClass
    {
        None,
        BloodKnight,
        DemonHunter,
        VampireLord,
        ShadowBlade,
        ArcaneSorcerer,
        DeathMage
    }
    public enum BloodType
    {
        Worker,
        Warrior,
        Scholar,
        Rogue,
        Mutant,
        VBlood,
        Frailed,
        GateBoss,
        Draculin,
        Immortal,
        Creature,
        Brute,
        Corruption
    }
    public enum WeaponType
    {
        Sword,
        Axe,
        Mace,
        Spear,
        Crossbow,
        GreatSword,
        Slashers,
        Pistols,
        Reaper,
        Longbow,
        Whip,
        Unarmed,
        FishingPole,
        TwinBlades,
        Daggers,
        Claws
    }

    /// <summary>
    /// Tabs available in the Bloodcraft character menu.
    /// </summary>
    public enum BloodcraftTab
    {
        Prestige,
        Exoform,
        Battles,
        StatBonuses,
        Progression,
        Familiars
    }

    public static Dictionary<WeaponStatType, float> _weaponStatValues = [];
    public enum WeaponStatType
    {
        None,
        MaxHealth,
        MovementSpeed,
        PrimaryAttackSpeed,
        PhysicalLifeLeech,
        SpellLifeLeech,
        PrimaryLifeLeech,
        PhysicalPower,
        SpellPower,
        PhysicalCriticalStrikeChance,
        PhysicalCriticalStrikeDamage,
        SpellCriticalStrikeChance,
        SpellCriticalStrikeDamage
    }

    /*
    public enum WeaponStatType
    {
        None,
        MaxHealth,
        MovementSpeed,
        PrimaryAttackSpeed,
        PhysicalLifeLeech,
        SpellLifeLeech,
        PrimaryLifeLeech,
        PhysicalPower,
        SpellPower,
        PhysicalCriticalStrikeChance,
        PhysicalCriticalStrikeDamage,
        SpellCriticalStrikeChance,
        SpellCriticalStrikeDamage
    }
    */

    public static readonly Dictionary<WeaponStatType, string> WeaponStatTypeAbbreviations = new()
    {
        { WeaponStatType.MaxHealth, "HP" },
        { WeaponStatType.MovementSpeed, "MS" },
        { WeaponStatType.PrimaryAttackSpeed, "PAS" },
        { WeaponStatType.PhysicalLifeLeech, "PLL" },
        { WeaponStatType.SpellLifeLeech, "SLL" },
        { WeaponStatType.PrimaryLifeLeech, "PAL" },
        { WeaponStatType.PhysicalPower, "PP" },
        { WeaponStatType.SpellPower, "SP" },
        { WeaponStatType.PhysicalCriticalStrikeChance, "PCC" },
        { WeaponStatType.PhysicalCriticalStrikeDamage, "PCD" },
        { WeaponStatType.SpellCriticalStrikeChance, "SCC" },
        { WeaponStatType.SpellCriticalStrikeDamage, "SCD" }
    };

    public static readonly Dictionary<string, string> WeaponStatStringAbbreviations = new()
    {
        { "MaxHealth", "HP" },
        { "MovementSpeed", "MS" },
        { "PrimaryAttackSpeed", "PAS" },
        { "PhysicalLifeLeech", "PLL" },
        { "SpellLifeLeech", "SLL" },
        { "PrimaryLifeLeech", "PAL" },
        { "PhysicalPower", "PP" },
        { "SpellPower", "SP" },
        { "PhysicalCriticalStrikeChance", "PCC" },
        { "PhysicalCriticalStrikeDamage", "PCD" },
        { "SpellCriticalStrikeChance", "SCC" },
        { "SpellCriticalStrikeDamage", "SCD" }
    };

    public static readonly Dictionary<WeaponStatType, string> WeaponStatFormats = new()
    {
        { WeaponStatType.MaxHealth, "integer" },
        { WeaponStatType.MovementSpeed, "decimal" },
        { WeaponStatType.PrimaryAttackSpeed, "percentage" },
        { WeaponStatType.PhysicalLifeLeech, "percentage" },
        { WeaponStatType.SpellLifeLeech, "percentage" },
        { WeaponStatType.PrimaryLifeLeech, "percentage" },
        { WeaponStatType.PhysicalPower, "decimal" },
        { WeaponStatType.SpellPower, "decimal" },
        { WeaponStatType.PhysicalCriticalStrikeChance, "percentage" },
        { WeaponStatType.PhysicalCriticalStrikeDamage, "percentage" },
        { WeaponStatType.SpellCriticalStrikeChance, "percentage" },
        { WeaponStatType.SpellCriticalStrikeDamage, "percentage" }
    };

    public static Dictionary<BloodStatType, float> _bloodStatValues = [];
    public enum BloodStatType
    {
        None,
        HealingReceived,
        DamageReduction,
        PhysicalResistance,
        SpellResistance,
        ResourceYield,
        ReducedBloodDrain,
        SpellCooldownRecoveryRate,
        WeaponCooldownRecoveryRate,
        UltimateCooldownRecoveryRate,
        MinionDamage,
        AbilityAttackSpeed,
        CorruptionDamageReduction
    }

    public static readonly Dictionary<BloodStatType, string> BloodStatTypeAbbreviations = new()
    {
        { BloodStatType.HealingReceived, "HR" },
        { BloodStatType.DamageReduction, "DR" },
        { BloodStatType.PhysicalResistance, "PR" },
        { BloodStatType.SpellResistance, "SR" },
        { BloodStatType.ResourceYield, "RY" },
        { BloodStatType.ReducedBloodDrain, "BDR" },
        { BloodStatType.SpellCooldownRecoveryRate, "SCR" },
        { BloodStatType.WeaponCooldownRecoveryRate, "WCR" },
        { BloodStatType.UltimateCooldownRecoveryRate, "UCR" },
        { BloodStatType.MinionDamage, "MD" },
        { BloodStatType.AbilityAttackSpeed, "AAS" },
        { BloodStatType.CorruptionDamageReduction, "CDR" }
    };

    public static readonly Dictionary<string, string> BloodStatStringAbbreviations = new()
    {
        { "HealingReceived", "HR" },
        { "DamageReduction", "DR" },
        { "PhysicalResistance", "PR" },
        { "SpellResistance", "SR" },
        { "ResourceYield", "RY" },
        { "ReducedBloodDrain", "BDR" },
        { "SpellCooldownRecoveryRate", "SCR" },
        { "WeaponCooldownRecoveryRate", "WCR" },
        { "UltimateCooldownRecoveryRate", "UCR" },
        { "MinionDamage", "MD" },
        { "AbilityAttackSpeed", "AAS" },
        { "CorruptionDamageReduction", "CDR" }
    };

    public static Dictionary<FamiliarStatType, float> _familiarStatValues = [];
    public enum FamiliarStatType
    {
        MaxHealth,
        PhysicalPower,
        SpellPower
    }

    public static readonly Dictionary<FamiliarStatType, string> FamiliarStatTypeAbbreviations = new()
    {
        { FamiliarStatType.MaxHealth, "HP" },
        { FamiliarStatType.PhysicalPower, "PP" },
        { FamiliarStatType.SpellPower, "SP" }
    };

    public static readonly List<string> FamiliarStatStringAbbreviations = new()
    {
        { "HP" },
        { "PP" },
        { "SP" }
    };

    public static readonly Dictionary<Profession, Color> ProfessionColors = new()
    {
        { Profession.Enchanting,    new Color(0.5f, 0.1f, 0.8f, 0.5f) },
        { Profession.Alchemy,       new Color(0.1f, 0.9f, 0.7f, 0.5f) },
        { Profession.Harvesting,    new Color(0f, 0.5f, 0f, 0.5f) },
        { Profession.Blacksmithing, new Color(0.2f, 0.2f, 0.3f, 0.5f) },
        { Profession.Tailoring,     new Color(0.9f, 0.6f, 0.5f, 0.5f) },
        { Profession.Woodcutting,   new Color(0.5f, 0.3f, 0.1f, 0.5f) },
        { Profession.Mining,        new Color(0.5f, 0.5f, 0.5f, 0.5f) },
        { Profession.Fishing,       new Color(0f, 0.5f, 0.7f, 0.5f) }
    };

    public static Dictionary<PlayerClass, (List<WeaponStatType> WeaponStats, List<BloodStatType> BloodStats)> _classStatSynergies = [];

    public static float _prestigeStatMultiplier;
    public static float _classStatMultiplier;
    public static bool _extraRecipes;
    public static PrefabGUID _primalCost;

    public static bool _classSystemEnabled;
    public static bool _classShiftSlotEnabled;
    public static int _defaultClassSpell;
    public static PrefabGUID _changeClassItem;
    public static int _changeClassQuantity;
    public static List<int> _classSpellUnlockLevels = [];
    public static Dictionary<PlayerClass, List<int>> _classSpells = [];
    public static bool _classDataReady;
    public static bool _prestigeDataReady;
    public static bool _prestigeSystemEnabled;
    public static bool _prestigeLeaderboardEnabled;
    public static readonly Dictionary<string, List<PrestigeLeaderboardEntry>> _prestigeLeaderboards = [];
    public static readonly List<string> _prestigeLeaderboardOrder = [];
    public static int _prestigeLeaderboardIndex;

    public static bool _exoFormDataReady;
    public static bool _exoFormEnabled;
    public static int _exoFormPrestiges;
    public static float _exoFormCharge;
    public static float _exoFormMaxDuration;
    public static bool _exoFormTauntEnabled;
    public static string _exoFormCurrentForm = string.Empty;
    public static readonly List<ExoFormEntry> _exoFormEntries = [];

    public static bool _familiarBattleDataReady;
    public static bool _familiarSystemEnabled;
    public static bool _familiarBattlesEnabled;
    public static string _familiarActiveBattleGroup = string.Empty;
    public static string _familiarActiveBox = string.Empty;
    public static bool? _familiarCombatModeEnabled;
    public static bool? _familiarEmoteActionsEnabled;
    public static bool? _familiarShinyEnabled;
    public static bool? _familiarVBloodEmotesEnabled;
    public static bool _statBonusDataReady;
    public static WeaponStatBonusData _weaponStatBonusData;
    public static readonly List<FamiliarBattleGroupData> _familiarBattleGroups = [];
    public static readonly List<string> _familiarBoxNames = [];
    public static readonly List<FamiliarBoxEntryData> _familiarBoxEntries = [];
    public static bool _familiarOverflowDataReady;
    public static readonly List<FamiliarOverflowEntryData> _familiarOverflowEntries = [];
    
    // Familiar Talent sync from server
    public static readonly HashSet<int> _familiarAllocatedTalents = [];
    public static bool _familiarTalentsDataReady = false;
    
    static bool _familiarBoxListCaptureActive;
    static bool _familiarBoxEntriesCaptureActive;
    static bool _familiarOverflowEntriesCaptureActive;
    static readonly Regex FamiliarColorTagRegex = new(@"<\/?color[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly Regex FamiliarBoxEntryRegex = new(
        @"^\s*(?<slot>\d+)\s*\|\s*(?<name>.+?)\s*\[(?<level>\d+)\]\s*(\[(?<prestige>\d+)\])?\s*$",
        RegexOptions.Compiled);
    public class ProfessionData(string enchantingProgress, string enchantingLevel, string alchemyProgress, string alchemyLevel,
        string harvestingProgress, string harvestingLevel, string blacksmithingProgress, string blacksmithingLevel,
        string tailoringProgress, string tailoringLevel, string woodcuttingProgress, string woodcuttingLevel, string miningProgress,
        string miningLevel, string fishingProgress, string fishingLevel)
    {
        public float EnchantingProgress { get; set; } = float.Parse(enchantingProgress, CultureInfo.InvariantCulture) / 100f;
        public int EnchantingLevel { get; set; } = int.Parse(enchantingLevel, CultureInfo.InvariantCulture);
        public float AlchemyProgress { get; set; } = float.Parse(alchemyProgress, CultureInfo.InvariantCulture) / 100f;
        public int AlchemyLevel { get; set; } = int.Parse(alchemyLevel, CultureInfo.InvariantCulture);
        public float HarvestingProgress { get; set; } = float.Parse(harvestingProgress, CultureInfo.InvariantCulture) / 100f;
        public int HarvestingLevel { get; set; } = int.Parse(harvestingLevel, CultureInfo.InvariantCulture);
        public float BlacksmithingProgress { get; set; } = float.Parse(blacksmithingProgress, CultureInfo.InvariantCulture) / 100f;
        public int BlacksmithingLevel { get; set; } = int.Parse(blacksmithingLevel, CultureInfo.InvariantCulture);
        public float TailoringProgress { get; set; } = float.Parse(tailoringProgress, CultureInfo.InvariantCulture) / 100f;
        public int TailoringLevel { get; set; } = int.Parse(tailoringLevel, CultureInfo.InvariantCulture);
        public float WoodcuttingProgress { get; set; } = float.Parse(woodcuttingProgress, CultureInfo.InvariantCulture) / 100f;
        public int WoodcuttingLevel { get; set; } = int.Parse(woodcuttingLevel, CultureInfo.InvariantCulture);
        public float MiningProgress { get; set; } = float.Parse(miningProgress, CultureInfo.InvariantCulture) / 100f;
        public int MiningLevel { get; set; } = int.Parse(miningLevel, CultureInfo.InvariantCulture);
        public float FishingProgress { get; set; } = float.Parse(fishingProgress, CultureInfo.InvariantCulture) / 100f;
        public int FishingLevel { get; set; } = int.Parse(fishingLevel, CultureInfo.InvariantCulture);
    }
    public class ExperienceData(string percent, string level, string prestige, string playerClass)
    {
        public float Progress { get; set; } = float.Parse(percent, CultureInfo.InvariantCulture) / 100f;
        public int Level { get; set; } = int.Parse(level, CultureInfo.InvariantCulture);
        public int Prestige { get; set; } = int.Parse(prestige, CultureInfo.InvariantCulture);
        public PlayerClass Class { get; set; } = (PlayerClass)int.Parse(playerClass, CultureInfo.InvariantCulture);
    }
    public class LegacyData(string percent, string level, string prestige, string legacyType, string bonusStats) : ExperienceData(percent, level, prestige, legacyType)
    {
        public string LegacyType { get; set; } = ((BloodType)int.Parse(legacyType, CultureInfo.InvariantCulture)).ToString();
        public List<string> BonusStats { get; set; } = [..Enumerable.Range(0, bonusStats.Length / 2).Select(i => ((BloodStatType)int.Parse(bonusStats.Substring(i * 2, 2), CultureInfo.InvariantCulture)).ToString())];
    }
    public class ExpertiseData(string percent, string level, string prestige, string expertiseType, string bonusStats) : ExperienceData(percent, level, prestige, expertiseType)
    {
        public string ExpertiseType { get; set; } = ((WeaponType)int.Parse(expertiseType)).ToString();
        public List<string> BonusStats { get; set; } = [.. Enumerable.Range(0, bonusStats.Length / 2).Select(i => ((WeaponStatType)int.Parse(bonusStats.Substring(i * 2, 2), CultureInfo.InvariantCulture)).ToString())];
    }
    public class QuestData(string type, string progress, string goal, string target, string isVBlood)
    {
        public TargetType TargetType { get; set; } = (TargetType)int.Parse(type, CultureInfo.InvariantCulture);
        public int Progress { get; set; } = int.Parse(progress, CultureInfo.InvariantCulture);
        public int Goal { get; set; } = int.Parse(goal, CultureInfo.InvariantCulture);
        public string Target { get; set; } = target;
        public bool IsVBlood { get; set; } = bool.Parse(isVBlood);
    }
    public class FamiliarData(string percent, string level, string prestige, string familiarName, string familiarStats)
    {
        public float Progress { get; set; } = float.Parse(percent, CultureInfo.InvariantCulture) / 100f;
        public int Level { get; set; } = int.TryParse(level, out int parsedLevel) && parsedLevel > 0 ? parsedLevel : 1;
        public int Prestige { get; set; } = int.Parse(prestige, CultureInfo.InvariantCulture);
        public string FamiliarName { get; set; } = !string.IsNullOrEmpty(familiarName) ? familiarName : "Familiar";
        public List<string> FamiliarStats { get; set; } = ParseFamiliarStats(familiarStats);

        static List<string> ParseFamiliarStats(string stats)
        {
            if (string.IsNullOrWhiteSpace(stats))
            {
                return ["", "", ""];
            }

            // New format (v1.4+): "hp|pp|sp" (variable width, no overflow misalignment).
            if (stats.Contains('|'))
            {
                string[] parts = stats.Split('|');
                string hp = parts.Length > 0 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedHp)
                    ? parsedHp.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;
                string pp = parts.Length > 1 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedPp)
                    ? parsedPp.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;
                string sp = parts.Length > 2 && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedSp)
                    ? parsedSp.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;

                return [hp, pp, sp];
            }

            // Legacy format (<= v1.3): D4 + D3 + D3 (10 chars total).
            if (stats.Length >= 10)
            {
                string hpRaw = stats[..4];
                string ppRaw = stats[4..7];
                string spRaw = stats[7..10];

                string hp = int.TryParse(hpRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedHp)
                    ? parsedHp.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;
                string pp = int.TryParse(ppRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedPp)
                    ? parsedPp.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;
                string sp = int.TryParse(spRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedSp)
                    ? parsedSp.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;

                return [hp, pp, sp];
            }

            return ["", "", ""];
        }
    }
    public class FamiliarBoxEntryData(int slotIndex, string name, int level, int prestige, bool isShiny)
    {
        public int SlotIndex { get; } = slotIndex;
        public string Name { get; } = name;
        public int Level { get; } = level;
        public int Prestige { get; } = prestige;
        public bool IsShiny { get; } = isShiny;
    }

    public class FamiliarOverflowEntryData(int index, string name, int level, int prestige, bool isShiny)
    {
        public int Index { get; } = index;
        public string Name { get; } = name;
        public int Level { get; } = level;
        public int Prestige { get; } = prestige;
        public bool IsShiny { get; } = isShiny;
    }
    public class ShiftSpellData(string index)
    {
        public int ShiftSpellIndex { get; set; } = int.Parse(index, CultureInfo.InvariantCulture);
    }
    public class PrestigeLeaderboardEntry(string name, int value)
    {
        public string Name { get; } = name;
        public int Value { get; } = value;
    }
    public class ExoFormAbilityData(int abilityId, float cooldown)
    {
        public int AbilityId { get; } = abilityId;
        public float Cooldown { get; } = cooldown;
    }
    public class ExoFormEntry(string formName, bool unlocked, List<ExoFormAbilityData> abilities)
    {
        public string FormName { get; } = formName;
        public bool Unlocked { get; } = unlocked;
        public List<ExoFormAbilityData> Abilities { get; } = abilities;
    }
    public class FamiliarBattleSlotData(int id, int level, int prestige, string name)
    {
        public int Id { get; } = id;
        public int Level { get; } = level;
        public int Prestige { get; } = prestige;
        public string Name { get; } = name;
    }
    public class FamiliarBattleGroupData(string name, List<FamiliarBattleSlotData> slots)
    {
        public string Name { get; } = name;
        public List<FamiliarBattleSlotData> Slots { get; } = slots;
    }
    public class WeaponStatBonusData
    {
        public string WeaponType { get; set; }
        public int ExpertiseLevel { get; set; }
        public float ExpertiseProgress { get; set; }
        public int MaxStatChoices { get; set; }
        public List<StatBonusDataEntry> SelectedStats { get; set; } = new();
        public List<StatBonusDataEntry> AvailableStats { get; set; } = new();
    }

    public class StatBonusDataEntry
    {
        public int StatIndex { get; set; }
        public string StatName { get; set; }
        public float Value { get; set; }
        public float MaxValue { get; set; }
        public bool IsSelected { get; set; }
    }
    public class ConfigDataV1_3
    {
        public float PrestigeStatMultiplier;

        public float ClassStatMultiplier;

        public int MaxPlayerLevel;

        public int MaxLegacyLevel;

        public int MaxExpertiseLevel;

        public int MaxFamiliarLevel;

        public int MaxProfessionLevel;

        public bool ExtraRecipes;

        public int PrimalCost;

        public Dictionary<WeaponStatType, float> WeaponStatValues;

        public Dictionary<BloodStatType, float> BloodStatValues;

        public Dictionary<PlayerClass, (List<WeaponStatType> WeaponStats, List<BloodStatType> bloodStats)> ClassStatSynergies;
        public ConfigDataV1_3(string prestigeMultiplier, string statSynergyMultiplier, string maxPlayerLevel, string maxLegacyLevel, string maxExpertiseLevel, string maxFamiliarLevel, string maxProfessionLevel, string extraRecipes, string primalCost, string weaponStatValues, string bloodStatValues, string classStatSynergies)
        {
            PrestigeStatMultiplier = float.Parse(prestigeMultiplier, CultureInfo.InvariantCulture);
            ClassStatMultiplier = float.Parse(statSynergyMultiplier, CultureInfo.InvariantCulture);

            MaxPlayerLevel = int.Parse(maxPlayerLevel, CultureInfo.InvariantCulture);
            MaxLegacyLevel = int.Parse(maxLegacyLevel, CultureInfo.InvariantCulture);
            MaxExpertiseLevel = int.Parse(maxExpertiseLevel, CultureInfo.InvariantCulture);
            MaxFamiliarLevel = int.Parse(maxFamiliarLevel, CultureInfo.InvariantCulture);
            MaxProfessionLevel = int.Parse(maxProfessionLevel, CultureInfo.InvariantCulture);

            ExtraRecipes = bool.Parse(extraRecipes);
            PrimalCost = int.Parse(primalCost, CultureInfo.InvariantCulture);

            WeaponStatValues = weaponStatValues.Split(',')
            .Select((value, index) => new { Index = index + 1, Value = float.Parse(value, CultureInfo.InvariantCulture) })
            .ToDictionary(x => (WeaponStatType)x.Index, x => x.Value);

            BloodStatValues = bloodStatValues.Split(',')
            .Select((value, index) => new { Index = index + 1, Value = float.Parse(value, CultureInfo.InvariantCulture) })
            .ToDictionary(x => (BloodStatType)x.Index, x => x.Value);

            ClassStatSynergies = classStatSynergies
            .Split(',')
            .Select((value, index) => new { Value = value, Index = index })
            .GroupBy(x => x.Index / 3)
            .ToDictionary(
                g => (PlayerClass)int.Parse(g.ElementAt(0).Value, CultureInfo.InvariantCulture),
                g => (
                    Enumerable.Range(0, g.ElementAt(1).Value.Length / 2)
                        .Select(j => (WeaponStatType)int.Parse(g.ElementAt(1).Value.Substring(j * 2, 2), CultureInfo.InvariantCulture))
                        .ToList(),
                    Enumerable.Range(0, g.ElementAt(2).Value.Length / 2)
                        .Select(j => (BloodStatType)int.Parse(g.ElementAt(2).Value.Substring(j * 2, 2), CultureInfo.InvariantCulture))
                        .ToList()
                )
            );
        }
    }
    public static List<string> ParseMessageString(string serverMessage)
    {
        if (string.IsNullOrEmpty(serverMessage))
        {
            return [];
        }

        return [..serverMessage.Split(',')];
    }
    public static void ParseConfigData(List<string> configData)
    {
        int index = 0;

        try
        {
            ConfigDataV1_3 parsedConfigData = new(
                configData[index++], // prestigeMultiplier
                configData[index++], // statSynergyMultiplier
                configData[index++], // maxPlayerLevel
                configData[index++], // maxLegacyLevel
                configData[index++], // maxExpertiseLevel
                configData[index++], // maxFamiliarLevel
                configData[index++], // maxProfessionLevel no longer used and merits getting cut but that necessitates enough other changes leaving alone for the moment
                configData[index++], // extraRecipes
                configData[index++], // primalCost
                string.Join(",", configData.Skip(index).Take(12)), // Combine the next 11 elements for weaponStatValues
                string.Join(",", configData.Skip(index += 12).Take(12)), // Combine the following 11 elements for bloodStatValues
                string.Join(",", configData.Skip(index += 12)) // Combine all remaining elements for classStatSynergies
            );

            _prestigeStatMultiplier = parsedConfigData.PrestigeStatMultiplier;
            _classStatMultiplier = parsedConfigData.ClassStatMultiplier;

            _experienceMaxLevel = parsedConfigData.MaxPlayerLevel;
            _legacyMaxLevel = parsedConfigData.MaxLegacyLevel;
            _expertiseMaxLevel = parsedConfigData.MaxExpertiseLevel;
            _familiarMaxLevel = parsedConfigData.MaxFamiliarLevel;
            _extraRecipes = parsedConfigData.ExtraRecipes;
            _primalCost = new PrefabGUID(parsedConfigData.PrimalCost);

            _weaponStatValues = parsedConfigData.WeaponStatValues;
            _bloodStatValues = parsedConfigData.BloodStatValues;

            _classStatSynergies = parsedConfigData.ClassStatSynergies;

            // Phase 2: Infer system enablement from config values for BloodCraft compatibility
            _classSystemEnabled = true; // Always enabled for BloodCraft
            _classDataReady = true;

            _prestigeSystemEnabled = _prestigeStatMultiplier > 0;
            _prestigeLeaderboardEnabled = _prestigeSystemEnabled;
            _prestigeDataReady = true;

            // Phase 3: Pre-populate prestige types if empty
            if (_prestigeLeaderboardOrder.Count == 0)
            {
                foreach (string type in LegacyPrestigeTypes)
                {
                    _prestigeLeaderboardOrder.Add(type);
                }
            }

            _exoFormEnabled = _prestigeSystemEnabled;
            _exoFormDataReady = true;

            _familiarSystemEnabled = _familiarMaxLevel > 0;
            _familiarBattlesEnabled = _familiarSystemEnabled;
            _familiarBattleDataReady = true;

            // Phase 4: Pre-populate weapon stat bonus data if empty
            if (_weaponStatBonusData == null)
            {
                _weaponStatBonusData = new WeaponStatBonusData
                {
                    WeaponType = "None",
                    ExpertiseLevel = 0,
                    ExpertiseProgress = 0f,
                    MaxStatChoices = 3
                };

                foreach (WeaponStatType statType in Enum.GetValues(typeof(WeaponStatType)))
                {
                    if (statType == WeaponStatType.None) continue;

                    _weaponStatBonusData.AvailableStats.Add(new StatBonusDataEntry
                    {
                        StatIndex = (int)statType,
                        StatName = System.Text.RegularExpressions.Regex.Replace(statType.ToString(), "([a-z])([A-Z])", "$1 $2"),
                        Value = 0f,
                        MaxValue = _weaponStatValues.TryGetValue(statType, out float val) ? val : 0f,
                        IsSelected = false
                    });
                }
            }
            _statBonusDataReady = true;

            try
            {
                if (_extraRecipes) Recipes.ModifyRecipes();
            }
            catch (Exception ex)
            {
                DebugToolsBridge.TryLogWarning($"Failed to modify recipes: {ex}");
            }
        }
        catch (Exception ex)
        {
            DebugToolsBridge.TryLogWarning($"Failed to parse config data: {ex}");
        }
    }
    public static void ParseClassData(string classData)
    {
        if (string.IsNullOrEmpty(classData))
        {
            return;
        }

        try
        {
            string[] segments = classData.Split('|');
            if (segments.Length == 0)
            {
                return;
            }

            string[] baseFields = segments[0].Split(',');
            if (baseFields.Length < 6)
            {
                DebugToolsBridge.TryLogWarning("Class data payload is missing required base fields.");
                return;
            }

            _classSystemEnabled = bool.TryParse(baseFields[0], out bool classSystem) && classSystem;
            _classShiftSlotEnabled = bool.TryParse(baseFields[1], out bool shiftSlot) && shiftSlot;
            _defaultClassSpell = int.Parse(baseFields[2], CultureInfo.InvariantCulture);
            _changeClassItem = new PrefabGUID(int.Parse(baseFields[3], CultureInfo.InvariantCulture));
            _changeClassQuantity = int.Parse(baseFields[4], CultureInfo.InvariantCulture);

            string prestigeLevels = string.Join(",", baseFields.Skip(5));
            _classSpellUnlockLevels = prestigeLevels
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(value => int.Parse(value, CultureInfo.InvariantCulture))
                .ToList();

            _classSpells = [];
            for (int i = 1; i < segments.Length; i++)
            {
                string segment = segments[i];
                if (string.IsNullOrWhiteSpace(segment))
                {
                    continue;
                }

                string[] fields = segment.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length == 0)
                {
                    continue;
                }

                if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int classId))
                {
                    continue;
                }

                PlayerClass playerClass = (PlayerClass)classId;
                List<int> spells = fields
                    .Skip(1)
                    .Select(value => int.Parse(value, CultureInfo.InvariantCulture))
                    .ToList();

                _classSpells[playerClass] = spells;
            }

            _classDataReady = true;
        }
        catch (Exception ex)
        {
            DebugToolsBridge.TryLogWarning($"Failed to parse class data: {ex}");
        }
    }
    /// <summary>
    /// Parses the prestige leaderboard payload and updates cached leaderboard data.
    /// </summary>
    /// <param name="leaderboardData">The leaderboard payload without the event prefix.</param>
    public static void ParsePrestigeLeaderboardData(string leaderboardData)
    {
        if (string.IsNullOrEmpty(leaderboardData))
        {
            return;
        }

        try
        {
            string[] segments = leaderboardData.Split('|');
            if (segments.Length == 0)
            {
                return;
            }

            string[] baseFields = segments[0].Split(new[] { ',' }, 2);
            if (baseFields.Length < 2)
            {
                DebugToolsBridge.TryLogWarning("Prestige leaderboard payload is missing required base fields.");
                return;
            }

            _prestigeSystemEnabled = bool.TryParse(baseFields[0], out bool prestigeEnabled) && prestigeEnabled;
            _prestigeLeaderboardEnabled = bool.TryParse(baseFields[1], out bool leaderboardEnabled) && leaderboardEnabled;

            _prestigeLeaderboards.Clear();
            _prestigeLeaderboardOrder.Clear();

            if (!_prestigeSystemEnabled || !_prestigeLeaderboardEnabled)
            {
                _prestigeDataReady = true;
                return;
            }

            for (int i = 1; i < segments.Length; i++)
            {
                string segment = segments[i];
                if (string.IsNullOrWhiteSpace(segment))
                {
                    continue;
                }

                string[] fields = segment.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length == 0)
                {
                    continue;
                }

                string prestigeType = fields[0];
                List<PrestigeLeaderboardEntry> entries = [];

                for (int entryIndex = 1; entryIndex + 1 < fields.Length; entryIndex += 2)
                {
                    string name = fields[entryIndex];
                    if (!int.TryParse(fields[entryIndex + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                    {
                        continue;
                    }

                    entries.Add(new PrestigeLeaderboardEntry(name, value));
                }

                _prestigeLeaderboards[prestigeType] = entries;
                _prestigeLeaderboardOrder.Add(prestigeType);
            }

            _prestigeDataReady = true;
        }
        catch (Exception ex)
        {
            DebugToolsBridge.TryLogWarning($"Failed to parse prestige leaderboard data: {ex}");
        }
    }
    /// <summary>
    /// Parses the exoform and shapeshift payload and updates cached exoform data.
    /// </summary>
    /// <param name="exoFormData">The exoform payload without the event prefix.</param>
    public static void ParseExoFormData(string exoFormData)
    {
        if (string.IsNullOrEmpty(exoFormData))
        {
            return;
        }

        try
        {
            string[] segments = exoFormData.Split('|');
            if (segments.Length == 0)
            {
                return;
            }

            string[] baseFields = segments[0].Split(new[] { ',' }, 6);
            if (baseFields.Length < 6)
            {
                DebugToolsBridge.TryLogWarning("Exoform payload is missing required base fields.");
                return;
            }

            _exoFormEnabled = bool.TryParse(baseFields[0], out bool exoEnabled) && exoEnabled;
            _exoFormPrestiges = int.TryParse(baseFields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int prestiges) ? prestiges : 0;
            _exoFormCharge = float.TryParse(baseFields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float charge) ? charge : 0f;
            _exoFormMaxDuration = float.TryParse(baseFields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float maxDuration) ? maxDuration : 0f;
            _exoFormTauntEnabled = bool.TryParse(baseFields[4], out bool tauntEnabled) && tauntEnabled;
            _exoFormCurrentForm = baseFields[5];

            _exoFormEntries.Clear();

            if (!_exoFormEnabled)
            {
                _exoFormDataReady = true;
                return;
            }

            for (int i = 1; i < segments.Length; i++)
            {
                string segment = segments[i];
                if (string.IsNullOrWhiteSpace(segment))
                {
                    continue;
                }

                string[] fields = segment.Split(',');
                if (fields.Length < 2)
                {
                    continue;
                }

                string formName = fields[0];
                bool unlocked = bool.TryParse(fields[1], out bool unlockedValue) && unlockedValue;
                List<ExoFormAbilityData> abilities = [];

                for (int abilityIndex = 2; abilityIndex < fields.Length; abilityIndex++)
                {
                    string abilityField = fields[abilityIndex];
                    if (string.IsNullOrWhiteSpace(abilityField))
                    {
                        continue;
                    }

                    string[] parts = abilityField.Split(':', 2);
                    if (parts.Length < 2)
                    {
                        continue;
                    }

                    if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int abilityId))
                    {
                        continue;
                    }

                    float cooldown = float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedCooldown)
                        ? parsedCooldown
                        : 0f;

                    abilities.Add(new ExoFormAbilityData(abilityId, cooldown));
                }

                _exoFormEntries.Add(new ExoFormEntry(formName, unlocked, abilities));
            }

            _exoFormDataReady = true;
        }
        catch (Exception ex)
        {
            DebugToolsBridge.TryLogWarning($"Failed to parse exoform data: {ex}");
        }
    }
    /// <summary>
    /// Parses the familiar battle payload and updates cached familiar battle data.
    /// </summary>
    /// <param name="battleData">The familiar battle payload without the event prefix.</param>
    public static void ParseFamiliarBattleData(string battleData)
    {
        if (string.IsNullOrEmpty(battleData))
        {
            return;
        }

        try
        {
            string[] segments = battleData.Split('|');
            if (segments.Length == 0)
            {
                return;
            }

            string[] baseFields = segments[0].Split(new[] { ',' }, 3);
            if (baseFields.Length < 3)
            {
                DebugToolsBridge.TryLogWarning("Familiar battle payload is missing required base fields.");
                return;
            }

            _familiarSystemEnabled = bool.TryParse(baseFields[0], out bool familiarEnabled) && familiarEnabled;
            _familiarBattlesEnabled = bool.TryParse(baseFields[1], out bool battlesEnabled) && battlesEnabled;
            _familiarActiveBattleGroup = baseFields[2];

            _familiarBattleGroups.Clear();

            if (!_familiarSystemEnabled || !_familiarBattlesEnabled)
            {
                _familiarBattleDataReady = true;
                return;
            }

            for (int i = 1; i < segments.Length; i++)
            {
                string segment = segments[i];
                if (string.IsNullOrWhiteSpace(segment))
                {
                    continue;
                }

                string[] fields = segment.Split(',');
                if (fields.Length == 0)
                {
                    continue;
                }

                string groupName = fields[0];
                List<FamiliarBattleSlotData> slots = [];

                for (int slotIndex = 1; slotIndex < fields.Length; slotIndex++)
                {
                    string slotField = fields[slotIndex];
                    if (string.IsNullOrWhiteSpace(slotField))
                    {
                        continue;
                    }

                    string[] parts = slotField.Split(':', 4);
                    if (parts.Length < 3)
                    {
                        continue;
                    }

                    int id = int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedId) ? parsedId : 0;
                    int level = int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedLevel) ? parsedLevel : 0;
                    int prestige = int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedPrestige) ? parsedPrestige : 0;
                    string name = parts.Length > 3 ? parts[3] : string.Empty;

                    slots.Add(new FamiliarBattleSlotData(id, level, prestige, name));
                }

                _familiarBattleGroups.Add(new FamiliarBattleGroupData(groupName, slots));
            }

            _familiarBattleDataReady = true;
        }
        catch (Exception ex)
        {
            DebugToolsBridge.TryLogWarning($"Failed to parse familiar battle data: {ex}");
        }
    }

    /// <summary>
    /// Parses familiar box data from system chat replies.
    /// </summary>
    /// <param name="message">The chat message to inspect.</param>
    /// <returns>True when the message was handled as familiar box data.</returns>
    public static bool TryParseFamiliarBoxChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        string cleanMessage = StripFamiliarColorTags(message).Trim();
        if (string.IsNullOrWhiteSpace(cleanMessage))
        {
            return false;
        }

        if (TryParseFamiliarToggleChatMessage(cleanMessage))
        {
            return true;
        }

        if (cleanMessage.Equals("Overflow Familiars:", StringComparison.OrdinalIgnoreCase))
        {
            _familiarOverflowEntries.Clear();
            _familiarOverflowEntriesCaptureActive = true;
            _familiarOverflowDataReady = true;
            _familiarBoxListCaptureActive = false;
            _familiarBoxEntriesCaptureActive = false;
            return true;
        }

        if (cleanMessage.Equals("Overflow storage is empty.", StringComparison.OrdinalIgnoreCase))
        {
            _familiarOverflowEntries.Clear();
            _familiarOverflowEntriesCaptureActive = false;
            _familiarOverflowDataReady = true;
            return true;
        }

        if (cleanMessage.Equals("Familiar Boxes:", StringComparison.OrdinalIgnoreCase))
        {
            _familiarBoxNames.Clear();
            _familiarBoxListCaptureActive = true;
            _familiarBoxEntriesCaptureActive = false;
            _familiarOverflowEntriesCaptureActive = false;
            return true;
        }

        if (cleanMessage.StartsWith("Box Selected -", StringComparison.OrdinalIgnoreCase))
        {
            string boxName = cleanMessage.Replace("Box Selected -", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            if (!string.IsNullOrWhiteSpace(boxName))
            {
                _familiarActiveBox = boxName;
            }
            _familiarBoxEntries.Clear();
            _familiarBoxEntriesCaptureActive = false;
            _familiarOverflowEntriesCaptureActive = false;
            return true;
        }

        if (cleanMessage.Equals("Couldn't find active box!", StringComparison.OrdinalIgnoreCase))
        {
            _familiarActiveBox = string.Empty;
            _familiarBoxEntries.Clear();
            _familiarBoxEntriesCaptureActive = false;
            _familiarOverflowEntriesCaptureActive = false;
            return true;
        }

        if (cleanMessage.Equals("You don't have any unlocks yet!", StringComparison.OrdinalIgnoreCase))
        {
            _familiarBoxNames.Clear();
            _familiarBoxListCaptureActive = false;
            return true;
        }

        if (IsFamiliarBoxHeader(message, cleanMessage))
        {
            _familiarActiveBox = cleanMessage.TrimEnd(':').Trim();
            _familiarBoxEntries.Clear();
            _familiarBoxEntriesCaptureActive = true;
            _familiarBoxListCaptureActive = false;
            return true;
        }

        if (_familiarBoxListCaptureActive)
        {
            if (!message.Contains("color=white", StringComparison.OrdinalIgnoreCase))
            {
                _familiarBoxListCaptureActive = false;
                return false;
            }

            bool parsed = TryParseFamiliarBoxListLine(cleanMessage);
            if (!parsed)
            {
                _familiarBoxListCaptureActive = false;
            }
            return parsed;
        }

        if (_familiarBoxEntriesCaptureActive)
        {
            bool parsed = TryParseFamiliarBoxEntry(cleanMessage);
            if (!parsed)
            {
                _familiarBoxEntriesCaptureActive = false;
            }
            return parsed;
        }

        if (_familiarOverflowEntriesCaptureActive)
        {
            bool parsed = TryParseFamiliarOverflowEntry(cleanMessage);
            if (!parsed)
            {
                _familiarOverflowEntriesCaptureActive = false;
            }
            return parsed;
        }

        return false;
    }

    static bool TryParseFamiliarToggleChatMessage(string cleanMessage)
    {
        if (string.IsNullOrWhiteSpace(cleanMessage))
        {
            return false;
        }

        if (cleanMessage.StartsWith("Emote actions", StringComparison.OrdinalIgnoreCase))
        {
            if (cleanMessage.Contains("enabled", StringComparison.OrdinalIgnoreCase))
            {
                _familiarEmoteActionsEnabled = true;
                return true;
            }

            if (cleanMessage.Contains("disabled", StringComparison.OrdinalIgnoreCase))
            {
                _familiarEmoteActionsEnabled = false;
                return true;
            }

            return false;
        }

        if (cleanMessage.StartsWith("Shiny familiars", StringComparison.OrdinalIgnoreCase))
        {
            if (cleanMessage.Contains("enabled", StringComparison.OrdinalIgnoreCase))
            {
                _familiarShinyEnabled = true;
                return true;
            }

            if (cleanMessage.Contains("disabled", StringComparison.OrdinalIgnoreCase))
            {
                _familiarShinyEnabled = false;
                return true;
            }

            return false;
        }

        if (cleanMessage.StartsWith("VBlood emotes", StringComparison.OrdinalIgnoreCase))
        {
            if (cleanMessage.Contains("enabled", StringComparison.OrdinalIgnoreCase))
            {
                _familiarVBloodEmotesEnabled = true;
                return true;
            }

            if (cleanMessage.Contains("disabled", StringComparison.OrdinalIgnoreCase))
            {
                _familiarVBloodEmotesEnabled = false;
                return true;
            }

            return false;
        }

        if (cleanMessage.StartsWith("Familiar combat", StringComparison.OrdinalIgnoreCase))
        {
            if (cleanMessage.Contains("not enabled", StringComparison.OrdinalIgnoreCase))
            {
                _familiarCombatModeEnabled = false;
                return true;
            }

            if (cleanMessage.Contains("enabled", StringComparison.OrdinalIgnoreCase))
            {
                _familiarCombatModeEnabled = true;
                return true;
            }

            if (cleanMessage.Contains("disabled", StringComparison.OrdinalIgnoreCase))
            {
                _familiarCombatModeEnabled = false;
                return true;
            }

            return false;
        }

        return false;
    }

    /// <summary>
    /// Parses TALENTS:x,y,z response from server to sync allocated talents.
    /// </summary>
    public static bool TryParseFamiliarTalentMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return false;

        // Remove color tags
        string cleanMessage = FamiliarColorTagRegex.Replace(message, "").Trim();

        if (!cleanMessage.StartsWith("TALENTS:", StringComparison.OrdinalIgnoreCase))
            return false;

        string payload = cleanMessage.Substring("TALENTS:".Length);
        _familiarAllocatedTalents.Clear();

        if (payload.Equals("NONE", StringComparison.OrdinalIgnoreCase))
        {
            // No active familiar - don't mark as ready, need to retry when familiar is bound
            DebugToolsBridge.TryLogInfo("[DataService] Familiar talents: no active familiar, will retry");
            return true; // Parsed successfully, but don't set ready flag
        }

        if (payload.Equals("EMPTY", StringComparison.OrdinalIgnoreCase))
        {
            // Active familiar exists but has no talents - this is valid data
            _familiarTalentsDataReady = true;
            DebugToolsBridge.TryLogInfo("[DataService] Familiar talents synced: none allocated");
            return true;
        }

        // Parse comma-separated talent IDs
        string[] talentParts = payload.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in talentParts)
        {
            if (int.TryParse(part.Trim(), out int talentId))
            {
                _familiarAllocatedTalents.Add(talentId);
            }
        }

        _familiarTalentsDataReady = true;
        DebugToolsBridge.TryLogInfo($"[DataService] Familiar talents synced: {string.Join(",", _familiarAllocatedTalents)}");
        return true;
    }

    static bool IsFamiliarBoxHeader(string rawMessage, string cleanMessage)
    {
        if (!rawMessage.Contains("color=white", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!cleanMessage.EndsWith(":", StringComparison.Ordinal))
        {
            return false;
        }

        return !cleanMessage.Equals("Familiar Boxes:", StringComparison.OrdinalIgnoreCase);
    }

    static bool TryParseFamiliarBoxListLine(string cleanMessage)
    {
        if (string.IsNullOrWhiteSpace(cleanMessage))
        {
            return false;
        }

        string[] parts = cleanMessage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        foreach (string part in parts)
        {
            if (!string.IsNullOrWhiteSpace(part))
            {
                _familiarBoxNames.Add(part);
            }
        }

        return true;
    }

    static bool TryParseFamiliarBoxEntry(string cleanMessage)
    {
        Match match = FamiliarBoxEntryRegex.Match(cleanMessage);
        if (!match.Success)
        {
            return false;
        }

        int slotIndex = 0;
        if (match.Groups["slot"].Success)
        {
            int.TryParse(match.Groups["slot"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out slotIndex);
        }

        string name = match.Groups["name"].Value.Trim();
        bool isShiny = false;
        if (name.EndsWith("*", StringComparison.Ordinal))
        {
            isShiny = true;
            name = name.TrimEnd('*').TrimEnd();
        }

        if (!int.TryParse(match.Groups["level"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
        {
            level = 0;
        }

        int prestige = 0;
        if (match.Groups["prestige"].Success)
        {
            int.TryParse(match.Groups["prestige"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out prestige);
        }

        _familiarBoxEntries.Add(new FamiliarBoxEntryData(slotIndex, name, level, prestige, isShiny));
        return true;
    }

    static bool TryParseFamiliarOverflowEntry(string cleanMessage)
    {
        Match match = FamiliarBoxEntryRegex.Match(cleanMessage);
        if (!match.Success)
        {
            return false;
        }

        int index = 0;
        if (match.Groups["slot"].Success)
        {
            int.TryParse(match.Groups["slot"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
        }

        string name = match.Groups["name"].Value.Trim();
        bool isShiny = false;
        if (name.EndsWith("*", StringComparison.Ordinal))
        {
            isShiny = true;
            name = name.TrimEnd('*').TrimEnd();
        }

        if (!int.TryParse(match.Groups["level"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
        {
            level = 0;
        }

        int prestige = 0;
        if (match.Groups["prestige"].Success)
        {
            int.TryParse(match.Groups["prestige"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out prestige);
        }

        _familiarOverflowEntries.Add(new FamiliarOverflowEntryData(index, name, level, prestige, isShiny));
        _familiarOverflowDataReady = true;
        return true;
    }

    static string StripFamiliarColorTags(string message)
        => FamiliarColorTagRegex.Replace(message, string.Empty);
    /// <summary>
    /// Parses the weapon stat bonus payload and updates cached data.
    /// </summary>
    /// <param name="data">The stat bonus payload without the event prefix.</param>
    public static void ParseWeaponStatBonusData(string data)
    {
        if (string.IsNullOrEmpty(data)) return;

        try
        {
            string[] segments = data.Split('|');
            if (segments.Length == 0) return;

            string[] baseInfo = segments[0].Split(',');
            if (baseInfo.Length < 4) return;

            var bonusData = new WeaponStatBonusData
            {
                WeaponType = baseInfo[0],
                ExpertiseLevel = int.Parse(baseInfo[1], CultureInfo.InvariantCulture),
                ExpertiseProgress = float.Parse(baseInfo[2], CultureInfo.InvariantCulture),
                MaxStatChoices = int.Parse(baseInfo[3], CultureInfo.InvariantCulture)
            };

            for (int i = 1; i < segments.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(segments[i])) continue;
                string[] statParts = segments[i].Split(',');
                if (statParts.Length < 4) continue;

                int serverIndex = int.Parse(statParts[0], CultureInfo.InvariantCulture);
                // Map server index (0-based) to client enum (1-based, 0 is None)
                var clientStatType = (WeaponStatType)(serverIndex + 1);
                
                var entry = new StatBonusDataEntry
                {
                    StatIndex = (int)clientStatType,
                    IsSelected = statParts[1] == "1",
                    Value = float.Parse(statParts[2], CultureInfo.InvariantCulture),
                    MaxValue = float.Parse(statParts[3], CultureInfo.InvariantCulture),
                    StatName = "Unknown Stat"
                };
                
                if (Enum.IsDefined(typeof(WeaponStatType), clientStatType))
                {
                    // Use full name with spaces
                    entry.StatName = System.Text.RegularExpressions.Regex.Replace(clientStatType.ToString(), "([a-z])([A-Z])", "$1 $2");
                }

                bonusData.AvailableStats.Add(entry);
                if (entry.IsSelected)
                {
                    bonusData.SelectedStats.Add(entry);
                }
            }

            _weaponStatBonusData = bonusData;
            _statBonusDataReady = true;
        }
        catch (Exception ex)
        {
            DebugToolsBridge.TryLogWarning($"Failed to parse stat bonus data: {ex}");
        }
    }

    private static bool WeaponStatStrAbbreviation(WeaponStatType type, out string abbr)
    {
        // Try get abbreviation from dictionary
        // Note: Generic string keys in dictionary might be easier to use if enum keys fail
        // But we have `WeaponStatStringAbbreviations` (string->string) and `WeaponStatTypeAbbreviations` (Enum->string)
        // Let's use `WeaponStatTypeAbbreviations`
        return WeaponStatTypeAbbreviations.TryGetValue(type, out abbr);
    }
    public static void ParsePlayerData(List<string> playerData)
    {
        int index = 0;

        ExperienceData experienceData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        LegacyData legacyData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        ExpertiseData expertiseData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        FamiliarData familiarData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        ProfessionData professionData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        QuestData dailyQuestData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        QuestData weeklyQuestData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);

        _experienceProgress = experienceData.Progress;
        _experienceLevel = experienceData.Level;
        _experiencePrestige = experienceData.Prestige;
        _classType = experienceData.Class;

        _legacyProgress = legacyData.Progress;
        _legacyLevel = legacyData.Level;
        _legacyPrestige = legacyData.Prestige;
        _legacyType = legacyData.LegacyType;
        _legacyBonusStats = legacyData.BonusStats;

        _expertiseProgress = expertiseData.Progress;
        _expertiseLevel = expertiseData.Level;
        _expertisePrestige = expertiseData.Prestige;
        _expertiseType = expertiseData.ExpertiseType;
        _expertiseBonusStats = expertiseData.BonusStats;

        _familiarProgress = familiarData.Progress;
        _familiarLevel = familiarData.Level;
        _familiarPrestige = familiarData.Prestige;
        _familiarName = familiarData.FamiliarName;
        _familiarStats = familiarData.FamiliarStats;

        _enchantingProgress = professionData.EnchantingProgress;
        _enchantingLevel = professionData.EnchantingLevel;
        _alchemyProgress = professionData.AlchemyProgress;
        _alchemyLevel = professionData.AlchemyLevel;
        _harvestingProgress = professionData.HarvestingProgress;
        _harvestingLevel = professionData.HarvestingLevel;
        _blacksmithingProgress = professionData.BlacksmithingProgress;
        _blacksmithingLevel = professionData.BlacksmithingLevel;
        _tailoringProgress = professionData.TailoringProgress;
        _tailoringLevel = professionData.TailoringLevel;
        _woodcuttingProgress = professionData.WoodcuttingProgress;
        _woodcuttingLevel = professionData.WoodcuttingLevel;
        _miningProgress = professionData.MiningProgress;
        _miningLevel = professionData.MiningLevel;
        _fishingProgress = professionData.FishingProgress;
        _fishingLevel = professionData.FishingLevel;

        _dailyTargetType = dailyQuestData.TargetType;
        _dailyProgress = dailyQuestData.Progress;
        _dailyGoal = dailyQuestData.Goal;
        _dailyTarget = dailyQuestData.Target;
        _dailyVBlood = dailyQuestData.IsVBlood;

        _weeklyTargetType = weeklyQuestData.TargetType;
        _weeklyProgress = weeklyQuestData.Progress;
        _weeklyGoal = weeklyQuestData.Goal;
        _weeklyTarget = weeklyQuestData.Target;
        _weeklyVBlood = weeklyQuestData.IsVBlood;

        ShiftSpellData shiftSpellData = new(playerData[index]);
        _shiftSpellIndex = shiftSpellData.ShiftSpellIndex;

        // Phase 4: Sync weapon stat bonus data from progress payload
        if (_weaponStatBonusData != null)
        {
            _weaponStatBonusData.ExpertiseLevel = _expertiseLevel;
            _weaponStatBonusData.ExpertiseProgress = _expertiseProgress;
            _weaponStatBonusData.WeaponType = _expertiseType;

            // Clear previous selections
            _weaponStatBonusData.SelectedStats.Clear();
            foreach (var stat in _weaponStatBonusData.AvailableStats)
            {
                stat.IsSelected = false;
            }

            // Apply selections from 6-digit integer string (e.g. "010310")
            // _expertiseBonusStats is already a List<string> of enum names (e.g. ["MaxHealth", "PrimaryAttackSpeed"])
            if (_expertiseBonusStats != null)
            {
                foreach (string statName in _expertiseBonusStats)
                {
                    var statEntry = _weaponStatBonusData.AvailableStats.Find(s => 
                        s.StatName.Replace(" ", "").Equals(statName, StringComparison.OrdinalIgnoreCase));
                    
                    if (statEntry != null)
                    {
                        statEntry.IsSelected = true;
                        _weaponStatBonusData.SelectedStats.Add(statEntry);
                    }
                }
            }

            // Update Values based on level/prestige/class synergy
            foreach (var stat in _weaponStatBonusData.AvailableStats)
            {
                if (Enum.TryParse(stat.StatName.Replace(" ", ""), out WeaponStatType statType))
                {
                    float baseCap = _weaponStatValues.TryGetValue(statType, out float cap) ? cap : 0f;
                    float classMultiplier = BloodUI.Services.HUD.Shared.HudUtilities.ClassSynergy(statType, _classType, _classStatSynergies);
                    float prestigeMultiplier = 1f + (_prestigeStatMultiplier * _expertisePrestige);
                    float levelMultiplier = _expertiseMaxLevel > 0 ? ((float)_expertiseLevel / _expertiseMaxLevel) : 0f;
                    
                    stat.Value = baseCap * prestigeMultiplier * classMultiplier * levelMultiplier;
                    stat.MaxValue = baseCap; // Or calculate actual max possible?
                }
            }
        }
    }

    /// <summary>
    /// Parses a lightweight server status payload to drive UI messaging when sync is disabled.
    /// Expected format: "disabled,<reason>" or "enabled".
    /// </summary>
    public static void ParseEclipseSyncStatus(string data)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                _eclipseSyncDisabled = false;
                _eclipseSyncStatus = string.Empty;
                return;
            }

            string[] parts = data.Split(',', 2);
            string state = parts[0].Trim();
            string reason = parts.Length > 1 ? parts[1].Trim() : string.Empty;

            if (state.Equals("disabled", StringComparison.OrdinalIgnoreCase))
            {
                _eclipseSyncDisabled = true;
                _eclipseSyncStatus = reason switch
                {
                    "server-config" => "Sync: Disabled (server config)",
                    _ => "Sync: Disabled"
                };
            }
            else
            {
                _eclipseSyncDisabled = false;
                _eclipseSyncStatus = string.Empty;
            }
        }
        catch (Exception ex)
        {
            DebugToolsBridge.TryLogWarning($"Failed to parse Eclipse sync status: {ex}");
        }
    }
    public static void RequestPrestigeLeaderboard(string typeKey)
    {
        // Don't use [ECLIPSE] sub-type because the server doesn't support it for LB updates.
        // Send as regular command that the server's VCF system handles.
        BloodUI.Utilities.Quips.SendCommand($".prestige lb {typeKey}");
    }

    public static bool TryParsePrestigeLeaderboardChatMessage(string message)
    {
        var matches = _regexPrestigeLB.Matches(message);
        if (matches.Count == 0) return false;

        foreach (Match match in matches)
        {
            try
            {
                string rankStr = match.Groups[1].Value;
                string name = match.Groups[2].Value;
                string typeKey = match.Groups[3].Value;
                int level = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);

                if (!_prestigeLeaderboards.ContainsKey(typeKey))
                {
                    _prestigeLeaderboards[typeKey] = new List<PrestigeLeaderboardEntry>();
                }

                // If this is the start of a batch (rank 1), clear the previous data for this type
                if (rankStr == "1")
                {
                    _prestigeLeaderboards[typeKey].Clear();
                }

                // Check for duplicates
                if (!_prestigeLeaderboards[typeKey].Any(e => e.Name == name))
                {
                    _prestigeLeaderboards[typeKey].Add(new PrestigeLeaderboardEntry(name, level));
                }

                // Ensure the type is in our order list if it wasn't pre-populated
                if (!_prestigeLeaderboardOrder.Contains(typeKey))
                {
                    _prestigeLeaderboardOrder.Add(typeKey);
                }
            }
            catch (Exception ex)
            {
                DebugToolsBridge.TryLogWarning($"Failed to parse prestige leaderboard entry from chat: {ex}");
            }
        }

        return true;
    }
}
