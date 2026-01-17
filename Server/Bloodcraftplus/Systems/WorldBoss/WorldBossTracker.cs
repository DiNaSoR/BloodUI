using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bloodcraft.Systems.WorldBoss;

/// <summary>
/// Tracks active world bosses and manages damage attribution for rewards.
/// </summary>
public static class WorldBossTracker
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    /// <summary>
    /// Active boss tracking data.
    /// </summary>
    class TrackedBoss
    {
        public Entity Entity;
        public WorldBossModel Model;
        public float LastHpPercent = 100f;
        public DateTime SpawnTime;
        public Dictionary<string, float> DamageDealers = new();
    }

    static readonly ConcurrentDictionary<Entity, TrackedBoss> ActiveBosses = new();

    #region Registration

    /// <summary>
    /// Register a spawned boss for tracking.
    /// </summary>
    public static void RegisterBoss(Entity bossEntity, WorldBossModel model)
    {
        var tracked = new TrackedBoss
        {
            Entity = bossEntity,
            Model = model,
            SpawnTime = DateTime.Now
        };

        ActiveBosses[bossEntity] = tracked;
        Core.Log.LogDebug($"[WorldBoss] Registered boss '{model.Name}' for tracking.");
    }

    /// <summary>
    /// Unregister a boss (death or despawn).
    /// </summary>
    public static void UnregisterBoss(Entity bossEntity)
    {
        if (ActiveBosses.TryRemove(bossEntity, out var tracked))
        {
            // Reset mechanic states
            foreach (var mechanic in tracked.Model.Mechanics)
                mechanic.Reset();

            Core.Log.LogDebug($"[WorldBoss] Unregistered boss '{tracked.Model.Name}'.");
        }
    }

    /// <summary>
    /// Check if an entity is a tracked world boss.
    /// </summary>
    public static bool IsBoss(Entity entity)
    {
        return ActiveBosses.ContainsKey(entity);
    }

    /// <summary>
    /// Get tracked boss by entity.
    /// </summary>
    public static WorldBossModel GetBoss(Entity entity)
    {
        return ActiveBosses.TryGetValue(entity, out var tracked) ? tracked.Model : null;
    }

    #endregion

    #region Damage Tracking

    /// <summary>
    /// Record damage dealt by a player.
    /// </summary>
    public static void RecordDamage(Entity bossEntity, string playerName, float damage)
    {
        if (!ActiveBosses.TryGetValue(bossEntity, out var tracked))
            return;

        if (tracked.DamageDealers.ContainsKey(playerName))
            tracked.DamageDealers[playerName] += damage;
        else
            tracked.DamageDealers[playerName] = damage;

        tracked.Model.AddContributor(playerName);
    }

    /// <summary>
    /// Get all damage contributors sorted by damage dealt.
    /// </summary>
    public static List<(string Name, float Damage)> GetDamageRanking(Entity bossEntity)
    {
        if (!ActiveBosses.TryGetValue(bossEntity, out var tracked))
            return new List<(string, float)>();

        return tracked.DamageDealers
            .OrderByDescending(kv => kv.Value)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }

    #endregion

    #region Update Loop

    /// <summary>
    /// Update all active bosses (check HP, trigger mechanics).
    /// Called from WorldBossSystem.OnUpdate().
    /// </summary>
    public static void UpdateActiveBosses()
    {
        foreach (var kvp in ActiveBosses)
        {
            var tracked = kvp.Value;

            if (!EntityManager.Exists(tracked.Entity))
            {
                // Boss was killed or despawned externally
                OnBossDeath(tracked);
                ActiveBosses.TryRemove(kvp.Key, out _);
                continue;
            }

            // Check HP for mechanics
            if (EntityManager.HasComponent<Health>(tracked.Entity))
            {
                var health = EntityManager.GetComponentData<Health>(tracked.Entity);
                float currentHpPercent = (health.Value / health.MaxHealth.Value) * 100f;

                // Check HP threshold mechanics
                CheckMechanics(tracked, currentHpPercent);

                // Check for phase announcements
                CheckPhaseAnnouncement(tracked, currentHpPercent);

                tracked.LastHpPercent = currentHpPercent;
            }
        }
    }

    /// <summary>
    /// Check and trigger HP-based mechanics.
    /// </summary>
    static void CheckMechanics(TrackedBoss tracked, float currentHpPercent)
    {
        foreach (var mechanic in tracked.Model.Mechanics)
        {
            if (!mechanic.Enabled || mechanic.IsExpired)
                continue;

            if (mechanic.Trigger.Type != TriggerType.HpThreshold)
                continue;

            // Check if we crossed the threshold
            bool crossedThreshold = tracked.LastHpPercent >= mechanic.Trigger.Value &&
                                   currentHpPercent < mechanic.Trigger.Value;

            if (crossedThreshold && mechanic.CanTriggerAgain())
            {
                // Execute mechanic
                WorldBossMechanics.ExecuteMechanic(tracked.Entity, tracked.Model, mechanic);
            }
        }
    }

    /// <summary>
    /// Check for phase announcements based on HP thresholds.
    /// </summary>
    static void CheckPhaseAnnouncement(TrackedBoss tracked, float currentHpPercent)
    {
        // Announce at 75%, 50%, 25%
        int phase = currentHpPercent switch
        {
            < 25 => 4,
            < 50 => 3,
            < 75 => 2,
            _ => 1
        };

        if (phase > tracked.Model.LastAnnouncedPhase)
        {
            tracked.Model.LastAnnouncedPhase = phase;

            string phaseName = phase switch
            {
                2 => "Enraged",
                3 => "Desperate",
                4 => "Critical",
                _ => ""
            };

            if (!string.IsNullOrEmpty(phaseName))
            {
                string message = $"<color=#FF6600>⚔️ {tracked.Model.Name}</color> enters <color=#FF0000>{phaseName}</color> phase! ({currentHpPercent:F0}% HP)";
                BroadcastToAllPlayers(message);
            }
        }
    }

    #endregion

    #region Boss Death

    /// <summary>
    /// Handle boss death - distribute rewards.
    /// </summary>
    public static void OnBossDeath(Entity bossEntity)
    {
        if (!ActiveBosses.TryGetValue(bossEntity, out var tracked))
            return;

        OnBossDeath(tracked);
        ActiveBosses.TryRemove(bossEntity, out _);
    }

    static void OnBossDeath(TrackedBoss tracked)
    {
        var model = tracked.Model;

        // Filter contributors by distance
        var validContributors = FilterContributorsByDistance(tracked);

        if (validContributors.Count > 0)
        {
            // Distribute loot
            DistributeLoot(model, validContributors);

            // Announce kill
            AnnounceKill(model, validContributors);

            // Reset consecutive spawns on successful kill
            model.ConsecutiveSpawns = 0;
        }

        // Reset state
        model.IsActive = false;
        model.BossEntity = Entity.Null;
        model.IconEntity = Entity.Null;
        model.LastAnnouncedPhase = 0;
        model.ClearContributors();

        WorldBossDatabase.Save();
    }

    /// <summary>
    /// Filter contributors to those within range of the boss.
    /// </summary>
    static List<string> FilterContributorsByDistance(TrackedBoss tracked)
    {
        var valid = new List<string>();
        float3 bossPos = float3.zero;

        if (EntityManager.Exists(tracked.Entity) && EntityManager.HasComponent<LocalToWorld>(tracked.Entity))
            bossPos = EntityManager.GetComponentData<LocalToWorld>(tracked.Entity).Position;
        else
            return tracked.Model.Contributors.ToList(); // Can't verify, include all

        float maxDistance = 100f;
        float maxDistanceSq = maxDistance * maxDistance;

        // Query all players
        var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerCharacter>());
        var entities = query.ToEntityArray(Allocator.Temp);

        foreach (var entity in entities)
        {
            if (!EntityManager.HasComponent<LocalToWorld>(entity))
                continue;

            var playerPos = EntityManager.GetComponentData<LocalToWorld>(entity).Position;
            float distSq = math.distancesq(bossPos, playerPos);

            if (distSq <= maxDistanceSq)
            {
                var pc = EntityManager.GetComponentData<PlayerCharacter>(entity);
                string name = pc.Name.ToString();

                if (tracked.Model.Contributors.Contains(name))
                    valid.Add(name);
            }
        }

        entities.Dispose();
        return valid;
    }

    /// <summary>
    /// Distribute loot to all valid contributors.
    /// </summary>
    static void DistributeLoot(WorldBossModel model, List<string> contributors)
    {
        var random = new System.Random();

        foreach (var loot in model.LootTable)
        {
            // Roll for drop chance
            if (random.NextDouble() > loot.Chance)
                continue;

            foreach (var name in contributors)
            {
                try
                {
                    // Find player entity
                    var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerCharacter>());
                    var entities = query.ToEntityArray(Allocator.Temp);

                    foreach (var entity in entities)
                    {
                        var pc = EntityManager.GetComponentData<PlayerCharacter>(entity);
                        if (pc.Name.ToString() != name)
                            continue;

                        var userEntity = pc.UserEntity;
                        if (!EntityManager.Exists(userEntity))
                            continue;

                        // Give item using ServerGameManager
                        var itemGuid = new PrefabGUID(loot.ItemGUID);
                        if (!ServerGameManager.TryAddInventoryItem(entity, itemGuid, loot.Amount))
                        {
                            // Drop nearby if inventory full
                            InventoryUtilitiesServer.CreateDropItem(
                                EntityManager,
                                entity,
                                itemGuid,
                                loot.Amount,
                                default
                            );
                        }

                        // Send private message
                        var user = EntityManager.GetComponentData<User>(userEntity);
                        string msg = $"<color={loot.Color}>You received: {loot.Name} x{loot.Amount}</color>";
                        LocalizationService.HandleServerReply(EntityManager, user, msg);

                        break;
                    }

                    entities.Dispose();
                }
                catch (Exception ex)
                {
                    Core.Log.LogError($"[WorldBoss] Error giving loot to {name}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Announce boss kill with contributor list.
    /// </summary>
    static void AnnounceKill(WorldBossModel model, List<string> contributors)
    {
        string msg = $"<color=#00FF00>🏆 {model.Name} has been slain!</color> Contributors: {string.Join(", ", contributors.Take(5))}";
        if (contributors.Count > 5)
            msg += $" and {contributors.Count - 5} more";

        BroadcastToAllPlayers(msg);

        Core.Log.LogInfo($"[WorldBoss] {model.Name} killed by {contributors.Count} contributors");
    }

    #endregion

    #region Utility

    /// <summary>
    /// Broadcast message to all connected players.
    /// </summary>
    public static void BroadcastToAllPlayers(string message)
    {
        var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());
        var users = query.ToEntityArray(Allocator.Temp);

        foreach (var entity in users)
        {
            var user = EntityManager.GetComponentData<User>(entity);
            if (user.IsConnected)
            {
                LocalizationService.HandleServerReply(EntityManager, user, message);
            }
        }

        users.Dispose();
    }

    /// <summary>
    /// Get count of active bosses.
    /// </summary>
    public static int GetActiveBossCount() => ActiveBosses.Count;

    /// <summary>
    /// Clear all tracking data (for cleanup).
    /// </summary>
    public static void Clear() => ActiveBosses.Clear();

    #endregion
}
