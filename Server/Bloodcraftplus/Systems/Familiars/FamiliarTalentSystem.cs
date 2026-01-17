using Bloodcraft.Services;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Unity.Entities;

namespace Bloodcraft.Systems.Familiars;

/// <summary>
/// Path of Exile-style talent system for familiars.
/// Talents modify stats and can apply visual effects like enrage auras.
/// </summary>
public static class FamiliarTalentSystem
{
    #region Enums

    public enum TalentNodeType
    {
        Minor,      // Small stat boost (+5-10%)
        Notable,    // Significant bonus (+15-25%)
        Keystone    // Major effect with trade-off
    }

    public enum TalentStatType
    {
        PhysicalPower,
        SpellPower,
        AttackSpeed,
        MovementSpeed,
        MaxHealth,
        DamageReduction,
        CritChance,
        CritDamage
    }

    public enum TalentPath
    {
        Speed,      // Movement and attack speed
        Power,      // Damage focused
        Vitality    // Health and defense
    }

    #endregion

    #region Talent Node Definition

    public class TalentNode
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TalentNodeType Type { get; set; }
        public TalentPath Path { get; set; }
        public int Tier { get; set; } // 0 = start, higher = further in tree
        public List<int> Prerequisites { get; set; } = []; // Node IDs that must be allocated first
        public Dictionary<TalentStatType, float> StatModifiers { get; set; } = []; // Stat type -> multiplier (1.05 = +5%)
        public int? VisualBuffId { get; set; } // Optional buff for visual effect (e.g., enrage aura)
        public int PointCost { get; set; } = 1;
    }

    #endregion

    #region Talent Tree Definition

    /// <summary>
    /// Static talent tree definition - shared across all familiars
    /// </summary>
    public static readonly List<TalentNode> TalentTree =
    [
        // === SPEED PATH ===
        new TalentNode
        {
            Id = 1,
            Name = "Speed I",
            Description = "+5% Movement Speed",
            Type = TalentNodeType.Minor,
            Path = TalentPath.Speed,
            Tier = 1,
            Prerequisites = [],
            StatModifiers = { { TalentStatType.MovementSpeed, 0.05f } }
        },
        new TalentNode
        {
            Id = 2,
            Name = "Speed II",
            Description = "+10% Movement Speed",
            Type = TalentNodeType.Minor,
            Path = TalentPath.Speed,
            Tier = 2,
            Prerequisites = [1],
            StatModifiers = { { TalentStatType.MovementSpeed, 0.10f } }
        },
        new TalentNode
        {
            Id = 3,
            Name = "Swift Strike",
            Description = "+25% Attack Speed",
            Type = TalentNodeType.Notable,
            Path = TalentPath.Speed,
            Tier = 3,
            Prerequisites = [2],
            StatModifiers = { { TalentStatType.AttackSpeed, 0.25f } }
        },
        new TalentNode
        {
            Id = 4,
            Name = "Berserker",
            Description = "+50% Attack Speed, +30% Movement Speed, Dark Red Aura",
            Type = TalentNodeType.Keystone,
            Path = TalentPath.Speed,
            Tier = 4,
            Prerequisites = [3],
            StatModifiers = 
            { 
                { TalentStatType.AttackSpeed, 0.50f }, 
                { TalentStatType.MovementSpeed, 0.30f } 
            },
            VisualBuffId = -1133938228, // Enrage-style buff
            PointCost = 3
        },

        // === POWER PATH ===
        new TalentNode
        {
            Id = 10,
            Name = "Power I",
            Description = "+5% Physical & Spell Power",
            Type = TalentNodeType.Minor,
            Path = TalentPath.Power,
            Tier = 1,
            Prerequisites = [],
            StatModifiers = 
            { 
                { TalentStatType.PhysicalPower, 0.05f }, 
                { TalentStatType.SpellPower, 0.05f } 
            }
        },
        new TalentNode
        {
            Id = 11,
            Name = "Power II",
            Description = "+10% Physical & Spell Power",
            Type = TalentNodeType.Minor,
            Path = TalentPath.Power,
            Tier = 2,
            Prerequisites = [10],
            StatModifiers = 
            { 
                { TalentStatType.PhysicalPower, 0.10f }, 
                { TalentStatType.SpellPower, 0.10f } 
            }
        },
        new TalentNode
        {
            Id = 12,
            Name = "Brutal Force",
            Description = "+25% All Damage",
            Type = TalentNodeType.Notable,
            Path = TalentPath.Power,
            Tier = 3,
            Prerequisites = [11],
            StatModifiers = 
            { 
                { TalentStatType.PhysicalPower, 0.25f }, 
                { TalentStatType.SpellPower, 0.25f } 
            }
        },
        new TalentNode
        {
            Id = 13,
            Name = "Enrage",
            Description = "+100% Damage, +50% Attack Speed, -25% Health, Dark Red Aura",
            Type = TalentNodeType.Keystone,
            Path = TalentPath.Power,
            Tier = 4,
            Prerequisites = [12],
            StatModifiers = 
            { 
                { TalentStatType.PhysicalPower, 1.00f }, 
                { TalentStatType.SpellPower, 1.00f },
                { TalentStatType.AttackSpeed, 0.50f },
                { TalentStatType.MaxHealth, -0.25f }
            },
            VisualBuffId = -1133938228, // Enrage buff for dark red aura
            PointCost = 3
        },

        // === VITALITY PATH ===
        new TalentNode
        {
            Id = 20,
            Name = "Vitality I",
            Description = "+5% Max Health",
            Type = TalentNodeType.Minor,
            Path = TalentPath.Vitality,
            Tier = 1,
            Prerequisites = [],
            StatModifiers = { { TalentStatType.MaxHealth, 0.05f } }
        },
        new TalentNode
        {
            Id = 21,
            Name = "Vitality II",
            Description = "+10% Max Health",
            Type = TalentNodeType.Minor,
            Path = TalentPath.Vitality,
            Tier = 2,
            Prerequisites = [20],
            StatModifiers = { { TalentStatType.MaxHealth, 0.10f } }
        },
        new TalentNode
        {
            Id = 22,
            Name = "Fortitude",
            Description = "+25% Max Health",
            Type = TalentNodeType.Notable,
            Path = TalentPath.Vitality,
            Tier = 3,
            Prerequisites = [21],
            StatModifiers = { { TalentStatType.MaxHealth, 0.25f } }
        },
        new TalentNode
        {
            Id = 23,
            Name = "Juggernaut",
            Description = "+100% Health, +50% Damage Reduction, -30% Movement Speed",
            Type = TalentNodeType.Keystone,
            Path = TalentPath.Vitality,
            Tier = 4,
            Prerequisites = [22],
            StatModifiers = 
            { 
                { TalentStatType.MaxHealth, 1.00f }, 
                { TalentStatType.DamageReduction, 0.50f },
                { TalentStatType.MovementSpeed, -0.30f }
            },
            PointCost = 3
        },

        // === CROSS-PATH NODES ===
        new TalentNode
        {
            Id = 30,
            Name = "Critical Focus",
            Description = "+15% Crit Chance",
            Type = TalentNodeType.Notable,
            Path = TalentPath.Power,
            Tier = 2,
            Prerequisites = [10],
            StatModifiers = { { TalentStatType.CritChance, 0.15f } }
        },
        new TalentNode
        {
            Id = 31,
            Name = "Deadly Precision",
            Description = "+30% Crit Damage",
            Type = TalentNodeType.Notable,
            Path = TalentPath.Power,
            Tier = 3,
            Prerequisites = [30],
            StatModifiers = { { TalentStatType.CritDamage, 0.30f } }
        },
        new TalentNode
        {
            Id = 32,
            Name = "Frenzy",
            Description = "Double Damage on Crit, +30% Crit Chance",
            Type = TalentNodeType.Keystone,
            Path = TalentPath.Power,
            Tier = 4,
            Prerequisites = [31],
            StatModifiers = 
            { 
                { TalentStatType.CritChance, 0.30f }, 
                { TalentStatType.CritDamage, 1.00f }
            },
            PointCost = 3
        }
    ];

    /// <summary>
    /// Get a talent node by ID
    /// </summary>
    public static TalentNode GetTalentNode(int id) => TalentTree.FirstOrDefault(n => n.Id == id);

    /// <summary>
    /// Get all talent nodes for a specific path
    /// </summary>
    public static List<TalentNode> GetTalentsByPath(TalentPath path) => 
        TalentTree.Where(n => n.Path == path).OrderBy(n => n.Tier).ToList();

    #endregion

    #region Talent Application

    /// <summary>
    /// Calculate combined stat modifiers from allocated talents
    /// </summary>
    public static Dictionary<TalentStatType, float> CalculateTalentBonuses(List<int> allocatedTalentIds)
    {
        var bonuses = new Dictionary<TalentStatType, float>();

        foreach (int talentId in allocatedTalentIds)
        {
            TalentNode node = GetTalentNode(talentId);
            if (node == null) continue;

            foreach (var modifier in node.StatModifiers)
            {
                if (bonuses.ContainsKey(modifier.Key))
                    bonuses[modifier.Key] += modifier.Value;
                else
                    bonuses[modifier.Key] = modifier.Value;
            }
        }

        return bonuses;
    }

    /// <summary>
    /// Get visual buff IDs for allocated talents (e.g., enrage aura)
    /// </summary>
    public static List<int> GetVisualBuffs(List<int> allocatedTalentIds)
    {
        return allocatedTalentIds
            .Select(GetTalentNode)
            .Where(n => n?.VisualBuffId.HasValue == true)
            .Select(n => n.VisualBuffId.Value)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Check if a talent can be allocated (prerequisites met)
    /// </summary>
    public static bool CanAllocateTalent(int talentId, List<int> currentlyAllocated)
    {
        TalentNode node = GetTalentNode(talentId);
        if (node == null) return false;

        // Already allocated?
        if (currentlyAllocated.Contains(talentId)) return false;

        // Check prerequisites
        return node.Prerequisites.All(prereq => currentlyAllocated.Contains(prereq));
    }

    /// <summary>
    /// Calculate total talent points spent
    /// </summary>
    public static int CalculatePointsSpent(List<int> allocatedTalentIds)
    {
        return allocatedTalentIds
            .Select(GetTalentNode)
            .Where(n => n != null)
            .Sum(n => n.PointCost);
    }

    /// <summary>
    /// Calculate available talent points for a familiar based on level and prestige
    /// </summary>
    public static int CalculateAvailableTalentPoints(int familiarLevel, int familiarPrestige, int maxLevel)
    {
        // 1 point every 10 levels + 2 points per prestige
        int levelPoints = familiarLevel / 10;
        int prestigePoints = familiarPrestige * 2;
        return levelPoints + prestigePoints;
    }

    #endregion
}
