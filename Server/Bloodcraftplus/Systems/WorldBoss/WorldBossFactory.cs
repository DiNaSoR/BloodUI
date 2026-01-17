// ============================================================================
// World Boss Factory - Spawning & Configuration
// Inspired by BloodyBoss by @oscarpedrero
// https://github.com/oscarpedrero/BloodyBoss
// ============================================================================

using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bloodcraft.Systems.WorldBoss;

/// <summary>
/// Factory for spawning and configuring World Boss entities.
/// </summary>
public static class WorldBossFactory
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    /// <summary>
    /// Spawn a world boss at configured location.
    /// </summary>
    public static void SpawnBoss(WorldBossModel boss, Entity triggerUser)
    {
        if (boss.IsActive)
        {
            Core.Log.LogWarning($"[WorldBoss] Boss '{boss.Name}' is already active.");
            return;
        }

        var prefabGuid = new PrefabGUID(boss.PrefabGUID);
        var position = boss.Position;

        try
        {
            // Spawn entity using ServerGameManager pattern (same as familiars)
            Entity bossEntity = ServerGameManager.InstantiateEntityImmediate(triggerUser, prefabGuid);

            if (bossEntity == Entity.Null)
            {
                Core.Log.LogError($"[WorldBoss] Failed to instantiate '{boss.Name}'");
                return;
            }

            // Move to configured position
            if (EntityManager.HasComponent<Translation>(bossEntity))
            {
                var translation = EntityManager.GetComponentData<Translation>(bossEntity);
                translation.Value = position;
                EntityManager.SetComponentData(bossEntity, translation);
            }

            // Configure the boss
            ConfigureBossEntity(bossEntity, boss, triggerUser);

            Core.Log.LogInfo($"[WorldBoss] Spawned '{boss.Name}' at {position}");
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"[WorldBoss] Failed to spawn '{boss.Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Configure the spawned boss entity with custom stats.
    /// </summary>
    static void ConfigureBossEntity(Entity bossEntity, WorldBossModel boss, Entity triggerUser)
    {
        if (bossEntity == Entity.Null)
        {
            Core.Log.LogError($"[WorldBoss] Failed to spawn '{boss.Name}' - entity is null.");
            return;
        }

        try
        {
            // Store entity reference
            boss.BossEntity = bossEntity;
            boss.IsActive = true;
            boss.LastSpawn = DateTime.Now;

            // Set level
            if (EntityManager.HasComponent<UnitLevel>(bossEntity))
            {
                var unitLevel = EntityManager.GetComponentData<UnitLevel>(bossEntity);
                unitLevel.Level = new ModifiableInt(boss.Level);
                EntityManager.SetComponentData(bossEntity, unitLevel);
            }

            // Apply health multiplier with dynamic scaling
            float finalMultiplier = CalculateFinalHealthMultiplier(boss);
            if (EntityManager.HasComponent<Health>(bossEntity))
            {
                var health = EntityManager.GetComponentData<Health>(bossEntity);
                health.MaxHealth._Value *= finalMultiplier;
                health.Value = health.MaxHealth.Value;
                EntityManager.SetComponentData(bossEntity, health);
            }

            // Clear default drop table if configured
            ClearDropTable(bossEntity);

            // Add unique identifier for tracking
            AddNameableComponent(bossEntity, boss);

            // Prevent VBlood unlock from world boss
            RemoveVBloodUnlockBuffer(bossEntity);

            // CRITICAL: Prevent VBlood from despawning after kills
            PreventDespawn(bossEntity);

            // Register for tracking
            WorldBossTracker.RegisterBoss(bossEntity, boss);

            // Add map icon
            AddMapIcon(boss, triggerUser);

            // Send spawn announcement
            SendSpawnAnnouncement(boss);

            // Increment consecutive spawns for progressive difficulty
            boss.ConsecutiveSpawns++;

            // Save state
            WorldBossDatabase.Save();

            Core.Log.LogInfo($"[WorldBoss] '{boss.Name}' configured: Level {boss.Level}, HP x{finalMultiplier:F2}");
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"[WorldBoss] Error configuring '{boss.Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Prevent the VBlood from despawning after kills.
    /// VBloods have LifeTime components and return buffs that cause despawn.
    /// </summary>
    static void PreventDespawn(Entity bossEntity)
    {
        // Remove LifeTime component to prevent timed despawn
        if (EntityManager.HasComponent<LifeTime>(bossEntity))
        {
            EntityManager.RemoveComponent<LifeTime>(bossEntity);
            Core.Log.LogDebug("[WorldBoss] Removed LifeTime component");
        }

        // Remove Age component if present
        if (EntityManager.HasComponent<Age>(bossEntity))
        {
            EntityManager.RemoveComponent<Age>(bossEntity);
            Core.Log.LogDebug("[WorldBoss] Removed Age component");
        }

        // Keep aggro decay disabled but allow aggro to work
        if (EntityManager.HasComponent<AggroConsumer>(bossEntity))
        {
            var aggro = EntityManager.GetComponentData<AggroConsumer>(bossEntity);
            aggro.AlertDecayPerSecond = 0f;  // Never lose aggro
            aggro.MaxDistanceFromPreCombatPosition = 0f;  // Don't return to spawn
            aggro.Active._Value = true;  // ENABLE aggro
            EntityManager.SetComponentData(bossEntity, aggro);
            Core.Log.LogDebug("[WorldBoss] Enabled aggro, disabled decay");
        }

        // Remove IsMinion to prevent minion despawn logic
        if (EntityManager.HasComponent<IsMinion>(bossEntity))
        {
            EntityManager.RemoveComponent<IsMinion>(bossEntity);
            Core.Log.LogDebug("[WorldBoss] Removed IsMinion component");
        }

        // Make hostile to all players
        MakeHostileToPlayers(bossEntity);
    }

    /// <summary>
    /// Make the boss hostile to all players by removing ownership and enabling aggro.
    /// </summary>
    static void MakeHostileToPlayers(Entity bossEntity)
    {
        // Remove Follower component - this makes spawned entities follow/ally with spawner
        if (EntityManager.HasComponent<Follower>(bossEntity))
        {
            EntityManager.RemoveComponent<Follower>(bossEntity);
            Core.Log.LogDebug("[WorldBoss] Removed Follower component");
        }

        // Enable Aggroable so boss can be targeted and will target others
        if (EntityManager.HasComponent<Aggroable>(bossEntity))
        {
            var aggroable = EntityManager.GetComponentData<Aggroable>(bossEntity);
            aggroable.Value._Value = true;
            EntityManager.SetComponentData(bossEntity, aggroable);
            Core.Log.LogDebug("[WorldBoss] Enabled Aggroable");
        }

        // Clear any team reference that might ally boss with spawner
        if (EntityManager.HasComponent<TeamReference>(bossEntity))
        {
            // Set to default/null team so boss is hostile to everyone
            var teamRef = EntityManager.GetComponentData<TeamReference>(bossEntity);
            teamRef.Value._Value = Entity.Null;
            EntityManager.SetComponentData(bossEntity, teamRef);
            Core.Log.LogDebug("[WorldBoss] Cleared TeamReference");
        }

        Core.Log.LogInfo("[WorldBoss] Boss set hostile to all players");
    }

    /// <summary>
    /// Calculate final health multiplier including dynamic scaling.
    /// </summary>
    static float CalculateFinalHealthMultiplier(WorldBossModel boss)
    {
        float baseMultiplier = boss.HealthMultiplier;

        if (ConfigService.WorldBossEnableDynamicScaling)
        {
            int onlinePlayers = GetOnlinePlayerCount();
            float perPlayerBonus = ConfigService.WorldBossHealthPerPlayer * onlinePlayers;
            int maxPlayers = ConfigService.WorldBossMaxPlayersForScaling;

            // Cap the bonus
            float cappedBonus = Math.Min(perPlayerBonus, ConfigService.WorldBossHealthPerPlayer * maxPlayers);

            return baseMultiplier * (1f + cappedBonus);
        }

        return baseMultiplier;
    }

    /// <summary>
    /// Clear the default drop table to prevent vanilla rewards.
    /// </summary>
    static void ClearDropTable(Entity bossEntity)
    {
        if (EntityManager.HasBuffer<DropTableDataBuffer>(bossEntity))
        {
            var buffer = EntityManager.GetBuffer<DropTableDataBuffer>(bossEntity);
            buffer.Clear();
        }
    }

    /// <summary>
    /// Add nameable component for entity identification.
    /// </summary>
    static void AddNameableComponent(Entity bossEntity, WorldBossModel boss)
    {
        if (!EntityManager.HasComponent<NameableInteractable>(bossEntity))
            EntityManager.AddComponent<NameableInteractable>(bossEntity);

        var nameable = new NameableInteractable
        {
            Name = new FixedString64Bytes(boss.NameHash + "_wb")
        };
        EntityManager.SetComponentData(bossEntity, nameable);
    }

    /// <summary>
    /// Remove VBlood unlock buffer to prevent tech unlocks.
    /// </summary>
    static void RemoveVBloodUnlockBuffer(Entity bossEntity)
    {
        if (EntityManager.HasBuffer<VBloodUnlockTechBuffer>(bossEntity))
        {
            var buffer = EntityManager.GetBuffer<VBloodUnlockTechBuffer>(bossEntity);
            buffer.Clear();
        }
    }

    /// <summary>
    /// Add map icon for the boss.
    /// </summary>
    static void AddMapIcon(WorldBossModel boss, Entity triggerUser)
    {
        try
        {
            var iconPrefab = new PrefabGUID(-604416910); // MapIcon_POI_VBloodSource

            Entity iconEntity = ServerGameManager.InstantiateEntityImmediate(triggerUser, iconPrefab);

            if (iconEntity == Entity.Null)
            {
                Core.Log.LogDebug($"[WorldBoss] Could not create map icon for '{boss.Name}'");
                return;
            }

            boss.IconEntity = iconEntity;

            // Set position
            if (EntityManager.HasComponent<Translation>(iconEntity))
            {
                var translation = EntityManager.GetComponentData<Translation>(iconEntity);
                translation.Value = new float3(boss.X, boss.Y, boss.Z);
                EntityManager.SetComponentData(iconEntity, translation);
            }

            // Link to boss
            if (!EntityManager.HasComponent<MapIconTargetEntity>(iconEntity))
                EntityManager.AddComponent<MapIconTargetEntity>(iconEntity);

            var target = new MapIconTargetEntity
            {
                TargetEntity = NetworkedEntity.ServerEntity(boss.BossEntity),
                TargetNetworkId = EntityManager.GetComponentData<NetworkId>(boss.BossEntity)
            };
            EntityManager.SetComponentData(iconEntity, target);

            // Name the icon
            if (!EntityManager.HasComponent<NameableInteractable>(iconEntity))
                EntityManager.AddComponent<NameableInteractable>(iconEntity);

            var nameable = new NameableInteractable
            {
                Name = new FixedString64Bytes(boss.NameHash + "_icon")
            };
            EntityManager.SetComponentData(iconEntity, nameable);
        }
        catch (Exception ex)
        {
            Core.Log.LogDebug($"[WorldBoss] Map icon error: {ex.Message}");
        }
    }

    /// <summary>
    /// Send spawn announcement to all players.
    /// </summary>
    static void SendSpawnAnnouncement(WorldBossModel boss)
    {
        string message = $"<color=#FF4444>⚔️ WORLD BOSS</color> <color=#FFD700>{boss.Name}</color> has spawned! " +
                        $"<color=#AAAAAA>(Level {boss.Level}, {boss.Lifetime / 60:F0} minutes)</color>";

        WorldBossTracker.BroadcastToAllPlayers(message);
    }

    /// <summary>
    /// Despawn a world boss.
    /// </summary>
    public static void DespawnBoss(WorldBossModel boss, Entity triggerUser)
    {
        if (!boss.IsActive)
            return;

        try
        {
            // Remove icon
            if (boss.IconEntity != Entity.Null && EntityManager.Exists(boss.IconEntity))
            {
                StatChangeUtility.KillOrDestroyEntity(EntityManager, boss.IconEntity, triggerUser, triggerUser, 0, StatChangeReason.Any, true);
            }

            // Kill boss entity
            if (boss.BossEntity != Entity.Null && EntityManager.Exists(boss.BossEntity))
            {
                ClearDropTable(boss.BossEntity);
                StatChangeUtility.KillOrDestroyEntity(EntityManager, boss.BossEntity, triggerUser, triggerUser, 0, StatChangeReason.Any, true);
            }

            // Unregister from tracking
            WorldBossTracker.UnregisterBoss(boss.BossEntity);

            // Reset state
            boss.BossEntity = Entity.Null;
            boss.IconEntity = Entity.Null;
            boss.IsActive = false;

            WorldBossDatabase.Save();

            Core.Log.LogInfo($"[WorldBoss] '{boss.Name}' despawned.");
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"[WorldBoss] Error despawning '{boss.Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Get count of online players.
    /// </summary>
    static int GetOnlinePlayerCount()
    {
        var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());
        var users = query.ToEntityArray(Allocator.Temp);

        int count = 0;
        foreach (var entity in users)
        {
            var user = EntityManager.GetComponentData<User>(entity);
            if (user.IsConnected)
                count++;
        }

        users.Dispose();
        return count;
    }
}
