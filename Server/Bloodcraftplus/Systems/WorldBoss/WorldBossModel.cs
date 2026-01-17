using Stunlock.Core;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace Bloodcraft.Systems.WorldBoss;

/// <summary>
/// Represents a World Boss encounter configuration and runtime state.
/// </summary>
public class WorldBossModel
{
    #region Identity

    /// <summary>Display name of the boss.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique hash for entity identification.</summary>
    public string NameHash { get; set; } = string.Empty;

    /// <summary>VBlood or NPC PrefabGUID.</summary>
    public int PrefabGUID { get; set; }

    /// <summary>Resolved asset name from prefab collection.</summary>
    public string AssetName { get; set; } = string.Empty;

    #endregion

    #region Stats

    /// <summary>Boss level override.</summary>
    public int Level { get; set; } = 90;

    /// <summary>Base health multiplier (before dynamic scaling).</summary>
    public float HealthMultiplier { get; set; } = 2.0f;

    /// <summary>Seconds before boss despawns.</summary>
    public float Lifetime { get; set; } = 1800f; // 30 minutes default

    #endregion

    #region Position

    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public float3 Position
    {
        get => new(X, Y, Z);
        set { X = value.x; Y = value.y; Z = value.z; }
    }

    #endregion

    #region Scheduling

    /// <summary>Daily spawn time in "HH:mm" format (e.g., "20:00").</summary>
    public string SpawnTime { get; set; } = string.Empty;

    /// <summary>Whether the boss timer is paused.</summary>
    public bool IsPaused { get; set; }

    /// <summary>When the boss was last spawned.</summary>
    public DateTime LastSpawn { get; set; } = DateTime.MinValue;

    #endregion

    #region Runtime State

    /// <summary>The spawned boss entity (Entity.Null when not active).</summary>
    public Entity BossEntity { get; set; } = Entity.Null;

    /// <summary>The map icon entity.</summary>
    public Entity IconEntity { get; set; } = Entity.Null;

    /// <summary>Whether the boss is currently spawned and active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Consecutive spawns without being killed (for progressive difficulty).</summary>
    public int ConsecutiveSpawns { get; set; }

    /// <summary>Last announced phase number.</summary>
    public int LastAnnouncedPhase { get; set; }

    #endregion

    #region Loot

    /// <summary>Loot table for this boss.</summary>
    public List<WorldBossLoot> LootTable { get; set; } = new();

    #endregion

    #region Mechanics

    /// <summary>Combat mechanics that trigger at HP thresholds.</summary>
    public List<WorldBossMechanic> Mechanics { get; set; } = new();

    #endregion

    #region Damage Tracking

    /// <summary>Players who contributed damage (character names).</summary>
    public List<string> Contributors { get; set; } = new();

    public void AddContributor(string characterName)
    {
        if (!Contributors.Contains(characterName))
            Contributors.Add(characterName);
    }

    public void ClearContributors() => Contributors.Clear();

    #endregion
}

/// <summary>
/// Loot drop configuration for world bosses.
/// </summary>
public class WorldBossLoot
{
    /// <summary>Display name of the item.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Item PrefabGUID.</summary>
    public int ItemGUID { get; set; }

    /// <summary>Amount to drop per recipient.</summary>
    public int Amount { get; set; } = 1;

    /// <summary>Drop chance (0.0 to 1.0).</summary>
    public float Chance { get; set; } = 1.0f;

    /// <summary>Color for chat message (hex format).</summary>
    public string Color { get; set; } = "#FFD700"; // Gold default
}

/// <summary>
/// Combat mechanic that triggers during boss fight.
/// </summary>
public class WorldBossMechanic
{
    /// <summary>Unique identifier for this mechanic instance.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Mechanic type: "stun", "aoe", "slow", "enrage", etc.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Whether this mechanic is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Trigger configuration.</summary>
    public MechanicTrigger Trigger { get; set; } = new();

    /// <summary>Type-specific parameters.</summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    #region Runtime State

    /// <summary>Whether this mechanic has been triggered.</summary>
    public bool HasTriggered { get; set; }

    /// <summary>When this mechanic was last triggered.</summary>
    public DateTime? LastTriggered { get; set; }

    /// <summary>Total trigger count.</summary>
    public int TriggerCount { get; set; }

    /// <summary>Is this a one-time mechanic that has been used?</summary>
    public bool IsExpired => Trigger.OneTime && HasTriggered;

    #endregion

    #region Methods

    public bool CanTriggerAgain()
    {
        if (!HasTriggered || !Trigger.OneTime)
            return true;

        if (Trigger.RepeatInterval > 0 && LastTriggered.HasValue)
        {
            var elapsed = (DateTime.Now - LastTriggered.Value).TotalSeconds;
            return elapsed >= Trigger.RepeatInterval;
        }

        return false;
    }

    public void MarkTriggered()
    {
        HasTriggered = true;
        LastTriggered = DateTime.Now;
        TriggerCount++;
    }

    public void Reset()
    {
        HasTriggered = false;
        LastTriggered = null;
        TriggerCount = 0;
    }

    public string GetDescription()
    {
        var desc = $"{Type}";

        if (Trigger != null)
        {
            switch (Trigger.Type)
            {
                case TriggerType.HpThreshold:
                    desc += $" @ {Trigger.Value}% HP";
                    break;
                case TriggerType.Time:
                    desc += $" @ {Trigger.Value}s";
                    if (Trigger.RepeatInterval > 0)
                        desc += $" (every {Trigger.RepeatInterval}s)";
                    break;
                case TriggerType.PlayerCount:
                    desc += $" when {Trigger.Comparison} {Trigger.Value} players";
                    break;
            }
        }

        if (!Enabled)
            desc += " [DISABLED]";

        return desc;
    }

    #endregion
}

/// <summary>
/// Trigger configuration for mechanics.
/// </summary>
public class MechanicTrigger
{
    /// <summary>Trigger type.</summary>
    public TriggerType Type { get; set; } = TriggerType.HpThreshold;

    /// <summary>Value for comparison (HP%, seconds, player count).</summary>
    public float Value { get; set; }

    /// <summary>Comparison operator.</summary>
    public TriggerComparison Comparison { get; set; } = TriggerComparison.LessThan;

    /// <summary>Only trigger once per encounter.</summary>
    public bool OneTime { get; set; } = true;

    /// <summary>Seconds between repeat triggers (0 = no repeat).</summary>
    public float RepeatInterval { get; set; }

    public bool Evaluate(float currentValue)
    {
        return Comparison switch
        {
            TriggerComparison.LessThan => currentValue < Value,
            TriggerComparison.LessThanOrEqual => currentValue <= Value,
            TriggerComparison.GreaterThan => currentValue > Value,
            TriggerComparison.GreaterThanOrEqual => currentValue >= Value,
            TriggerComparison.Equal => Math.Abs(currentValue - Value) < 0.01f,
            _ => false
        };
    }
}

public enum TriggerType
{
    HpThreshold,    // Triggers at HP percentage
    Time,           // Triggers after X seconds
    PlayerCount     // Triggers based on nearby player count
}

public enum TriggerComparison
{
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    Equal
}
