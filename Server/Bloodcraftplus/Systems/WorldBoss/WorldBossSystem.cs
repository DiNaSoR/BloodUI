using Bloodcraft.Patches;
// ============================================================================
// World Boss System for BloodCraftPlus
// 
// CREDITS & ACKNOWLEDGEMENT:
// This system is inspired by and adapted from BloodyBoss by @oscarpedrero
// Original project: https://github.com/oscarpedrero/BloodyBoss
// 
// BloodyBoss is an incredible standalone mod for V Rising that provides
// advanced VBlood world boss encounters. Full credit to oscarpedrero for
// the original concept, design patterns, and implementation ideas.
// 
// If you want a dedicated world boss solution with more features,
// we highly recommend checking out BloodyBoss!
// ============================================================================

using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Systems.WorldBoss;

/// <summary>
/// Main orchestrator for the World Boss system.
/// Handles scheduled spawning, update loop, and death handling.
/// </summary>
public static class WorldBossSystem
{
    static EntityManager EntityManager => Core.EntityManager;

    static Timer _spawnTimer;
    static bool _initialized;

    #region Initialization

    /// <summary>
    /// Initialize the World Boss system.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        if (!ConfigService.WorldBossSystem) return;

        try
        {
            // Load boss configurations
            WorldBossDatabase.Load();

            // Start spawn timer (checks every 30 seconds)
            _spawnTimer = new Timer(
                _ => OnTimerTick(),
                null,
                TimeSpan.FromSeconds(10),  // Initial delay
                TimeSpan.FromSeconds(30)   // Check interval
            );

            _initialized = true;
            Core.Log.LogInfo($"[WorldBoss] System initialized with {WorldBossDatabase.Bosses.Count} boss configurations.");
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"[WorldBoss] Failed to initialize: {ex.Message}");
        }
    }

    /// <summary>
    /// Shutdown the World Boss system.
    /// </summary>
    public static void Shutdown()
    {
        _spawnTimer?.Dispose();
        _spawnTimer = null;
        WorldBossTracker.Clear();
        _initialized = false;
    }

    #endregion

    #region Timer & Scheduling

    /// <summary>
    /// Timer callback - check for scheduled spawns.
    /// </summary>
    static void OnTimerTick()
    {
        if (!ConfigService.WorldBossSystem) return;

        try
        {
            var now = DateTime.Now;
            string currentTime = now.ToString("HH:mm");

            foreach (var boss in WorldBossDatabase.Bosses)
            {
                // Skip paused bosses
                if (boss.IsPaused) continue;

                // Skip already active bosses
                if (boss.IsActive) continue;

                // Skip if no spawn time configured
                if (string.IsNullOrEmpty(boss.SpawnTime)) continue;

                // Check if it's time to spawn
                if (boss.SpawnTime == currentTime)
                {
                    // Prevent double spawn in same minute
                    if (boss.LastSpawn.ToString("HH:mm") == currentTime)
                        continue;

                    SpawnBoss(boss);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"[WorldBoss] Timer error: {ex.Message}");
        }
    }

    /// <summary>
    /// Spawn a boss (triggered by timer or command).
    /// </summary>
    public static void SpawnBoss(WorldBossModel boss)
    {
        if (boss.IsActive)
        {
            Core.Log.LogWarning($"[WorldBoss] Boss '{boss.Name}' is already active.");
            return;
        }

        // Get any online user as trigger
        Entity triggerUser = GetAnyOnlineUser();
        if (triggerUser == Entity.Null)
        {
            Core.Log.LogWarning($"[WorldBoss] Cannot spawn '{boss.Name}' - no players online.");
            return;
        }

        WorldBossFactory.SpawnBoss(boss, triggerUser);
    }

    /// <summary>
    /// Force despawn a boss.
    /// </summary>
    public static void DespawnBoss(WorldBossModel boss)
    {
        Entity triggerUser = GetAnyOnlineUser();
        if (triggerUser == Entity.Null)
            triggerUser = Entity.Null; // Will still work for cleanup

        WorldBossFactory.DespawnBoss(boss, triggerUser);
    }

    #endregion

    #region Update Loop

    /// <summary>
    /// Called from game update loop to process active bosses.
    /// </summary>
    public static void OnUpdate()
    {
        if (!ConfigService.WorldBossSystem) return;
        if (!_initialized) return;

        WorldBossTracker.UpdateActiveBosses();
    }

    #endregion

    #region Death Handling

    /// <summary>
    /// Handle when a tracked world boss dies.
    /// </summary>
    public static void HandleBossDeath(Entity bossEntity, Entity killer)
    {
        if (!WorldBossTracker.IsBoss(bossEntity))
            return;

        // Record killer
        if (EntityManager.HasComponent<PlayerCharacter>(killer))
        {
            var pc = EntityManager.GetComponentData<PlayerCharacter>(killer);
            WorldBossTracker.GetBoss(bossEntity)?.AddContributor(pc.Name.ToString());
        }

        // Process death
        WorldBossTracker.OnBossDeath(bossEntity);
    }

    /// <summary>
    /// Handle damage dealt to a world boss.
    /// </summary>
    public static void HandleBossDamage(Entity bossEntity, Entity attacker, float damage)
    {
        if (!WorldBossTracker.IsBoss(bossEntity))
            return;

        // Get player name from attacker
        string playerName = null;

        if (EntityManager.HasComponent<PlayerCharacter>(attacker))
        {
            var pc = EntityManager.GetComponentData<PlayerCharacter>(attacker);
            playerName = pc.Name.ToString();
        }
        else if (EntityManager.HasComponent<Follower>(attacker))
        {
            // Familiar - get owner
            var follower = EntityManager.GetComponentData<Follower>(attacker);
            var followedEntity = follower.Followed._Value;
            if (followedEntity != Entity.Null &&
                EntityManager.HasComponent<PlayerCharacter>(followedEntity))
            {
                var pc = EntityManager.GetComponentData<PlayerCharacter>(followedEntity);
                playerName = pc.Name.ToString();
            }
        }

        if (!string.IsNullOrEmpty(playerName))
        {
            WorldBossTracker.RecordDamage(bossEntity, playerName, damage);
        }
    }

    #endregion

    #region API

    /// <summary>
    /// Create a new boss configuration.
    /// </summary>
    public static bool CreateBoss(string name, int prefabGuid, int level, float healthMultiplier = 2f, float lifetime = 1800f)
    {
        var boss = new WorldBossModel
        {
            Name = name,
            PrefabGUID = prefabGuid,
            Level = level,
            HealthMultiplier = healthMultiplier,
            Lifetime = lifetime
        };

        return WorldBossDatabase.AddBoss(boss);
    }

    /// <summary>
    /// Set boss spawn location.
    /// </summary>
    public static bool SetLocation(string name, float x, float y, float z)
    {
        var boss = WorldBossDatabase.GetBoss(name);
        if (boss == null) return false;

        boss.X = x;
        boss.Y = y;
        boss.Z = z;
        WorldBossDatabase.Save();
        return true;
    }

    /// <summary>
    /// Set boss spawn time.
    /// </summary>
    public static bool SetSpawnTime(string name, string time)
    {
        var boss = WorldBossDatabase.GetBoss(name);
        if (boss == null) return false;

        boss.SpawnTime = time;
        WorldBossDatabase.Save();
        return true;
    }

    /// <summary>
    /// Add loot to boss drop table.
    /// </summary>
    public static bool AddLoot(string bossName, string itemName, int itemGuid, int amount, float chance)
    {
        var boss = WorldBossDatabase.GetBoss(bossName);
        if (boss == null) return false;

        boss.LootTable.Add(new WorldBossLoot
        {
            Name = itemName,
            ItemGUID = itemGuid,
            Amount = amount,
            Chance = chance
        });

        WorldBossDatabase.Save();
        return true;
    }

    /// <summary>
    /// Add mechanic to boss.
    /// </summary>
    public static bool AddMechanic(string bossName, string type, float hpThreshold, Dictionary<string, object> parameters = null)
    {
        var boss = WorldBossDatabase.GetBoss(bossName);
        if (boss == null) return false;

        boss.Mechanics.Add(new WorldBossMechanic
        {
            Type = type,
            Trigger = new MechanicTrigger
            {
                Type = TriggerType.HpThreshold,
                Value = hpThreshold,
                Comparison = TriggerComparison.LessThan
            },
            Parameters = parameters ?? new Dictionary<string, object>()
        });

        WorldBossDatabase.Save();
        return true;
    }

    /// <summary>
    /// Get list of all configured bosses.
    /// </summary>
    public static IReadOnlyList<WorldBossModel> GetAllBosses() => WorldBossDatabase.Bosses;

    #endregion

    #region Utility

    static Entity GetAnyOnlineUser()
    {
        var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());
        var users = query.ToEntityArray(Allocator.Temp);

        foreach (var entity in users)
        {
            var user = EntityManager.GetComponentData<User>(entity);
            if (user.IsConnected)
            {
                users.Dispose();
                return entity;
            }
        }

        users.Dispose();
        return Entity.Null;
    }

    #endregion
}
