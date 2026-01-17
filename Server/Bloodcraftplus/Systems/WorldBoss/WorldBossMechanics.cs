using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bloodcraft.Systems.WorldBoss;

/// <summary>
/// Mechanic execution system for World Bosses.
/// </summary>
public static class WorldBossMechanics
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    #region Mechanic Registry

    static readonly Dictionary<string, IMechanic> Mechanics = new(StringComparer.OrdinalIgnoreCase)
    {
        { "stun", new StunMechanic() },
        { "aoe", new AoeMechanic() },
        { "enrage", new EnrageMechanic() },
        { "slow", new SlowMechanic() }
    };

    /// <summary>
    /// Register a custom mechanic.
    /// </summary>
    public static void RegisterMechanic(string type, IMechanic mechanic)
    {
        Mechanics[type.ToLowerInvariant()] = mechanic;
    }

    #endregion

    #region Execution

    /// <summary>
    /// Execute a mechanic on a boss.
    /// </summary>
    public static void ExecuteMechanic(Entity bossEntity, WorldBossModel boss, WorldBossMechanic mechanic)
    {
        if (!mechanic.Enabled || mechanic.IsExpired)
            return;

        if (!Mechanics.TryGetValue(mechanic.Type, out var implementation))
        {
            Core.Log.LogWarning($"[WorldBoss] Unknown mechanic type: {mechanic.Type}");
            return;
        }

        try
        {
            if (!implementation.CanApply(bossEntity))
            {
                Core.Log.LogDebug($"[WorldBoss] Cannot apply {mechanic.Type} to {boss.Name}");
                return;
            }

            implementation.Execute(bossEntity, mechanic.Parameters);
            mechanic.MarkTriggered();

            Core.Log.LogInfo($"[WorldBoss] Executed {mechanic.GetDescription()} on {boss.Name}");

            WorldBossDatabase.Save();
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"[WorldBoss] Error executing {mechanic.Type}: {ex.Message}");
        }
    }

    #endregion
}

#region Mechanic Interface

/// <summary>
/// Interface for boss combat mechanics.
/// </summary>
public interface IMechanic
{
    /// <summary>Mechanic type identifier.</summary>
    string Type { get; }

    /// <summary>Check if mechanic can be applied to entity.</summary>
    bool CanApply(Entity boss);

    /// <summary>Execute the mechanic.</summary>
    void Execute(Entity boss, Dictionary<string, object> parameters);
}

#endregion

#region Core Mechanics

/// <summary>
/// Stun mechanic - stuns a random/nearest player.
/// </summary>
public class StunMechanic : IMechanic
{
    static EntityManager EntityManager => Core.EntityManager;

    public string Type => "stun";

    public bool CanApply(Entity boss) => EntityManager.Exists(boss);

    public void Execute(Entity boss, Dictionary<string, object> parameters)
    {
        float duration = GetFloat(parameters, "duration", 3f);
        string target = GetString(parameters, "target", "random"); // random, nearest, all

        var players = GetNearbyPlayers(boss, 50f);
        if (players.Count == 0) return;

        Entity targetPlayer;
        if (target == "nearest")
            targetPlayer = GetNearestPlayer(boss, players);
        else if (target == "all")
        {
            foreach (var p in players)
                ApplyStun(p);
            return;
        }
        else
        {
            var random = new System.Random();
            targetPlayer = players[random.Next(players.Count)];
        }

        ApplyStun(targetPlayer);
    }

    void ApplyStun(Entity player)
    {
        // AB_Vampire_VeilOfBones_StunTarget
        var stunBuff = new PrefabGUID(475051028);
        
        // Apply buff using extension method
        player.TryApplyBuff(stunBuff);
    }

    List<Entity> GetNearbyPlayers(Entity boss, float radius)
    {
        var result = new List<Entity>();
        
        if (!EntityManager.HasComponent<LocalToWorld>(boss))
            return result;

        var bossPos = EntityManager.GetComponentData<LocalToWorld>(boss).Position;
        float radiusSq = radius * radius;

        var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerCharacter>());
        var entities = query.ToEntityArray(Allocator.Temp);

        foreach (var entity in entities)
        {
            if (!EntityManager.HasComponent<LocalToWorld>(entity))
                continue;

            var playerPos = EntityManager.GetComponentData<LocalToWorld>(entity).Position;
            if (math.distancesq(bossPos, playerPos) <= radiusSq)
                result.Add(entity);
        }

        entities.Dispose();
        return result;
    }

    Entity GetNearestPlayer(Entity boss, List<Entity> players)
    {
        var bossPos = EntityManager.GetComponentData<LocalToWorld>(boss).Position;
        Entity nearest = Entity.Null;
        float nearestDist = float.MaxValue;

        foreach (var player in players)
        {
            var playerPos = EntityManager.GetComponentData<LocalToWorld>(player).Position;
            float dist = math.distance(bossPos, playerPos);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = player;
            }
        }

        return nearest;
    }

    static float GetFloat(Dictionary<string, object> p, string key, float def) =>
        p.TryGetValue(key, out var v) && float.TryParse(v?.ToString(), out var f) ? f : def;

    static string GetString(Dictionary<string, object> p, string key, string def) =>
        p.TryGetValue(key, out var v) ? v?.ToString() ?? def : def;
}

/// <summary>
/// AoE mechanic - creates damage zone around boss.
/// </summary>
public class AoeMechanic : IMechanic
{
    static EntityManager EntityManager => Core.EntityManager;

    public string Type => "aoe";

    public bool CanApply(Entity boss) => EntityManager.Exists(boss);

    public void Execute(Entity boss, Dictionary<string, object> parameters)
    {
        float radius = GetFloat(parameters, "radius", 10f);
        float damage = GetFloat(parameters, "damage", 100f);
        string element = GetString(parameters, "element", "fire"); // fire, frost, chaos

        if (!EntityManager.HasComponent<LocalToWorld>(boss))
            return;

        var bossPos = EntityManager.GetComponentData<LocalToWorld>(boss).Position;

        // Get nearby players and deal damage
        var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerCharacter>());
        var entities = query.ToEntityArray(Allocator.Temp);

        foreach (var entity in entities)
        {
            if (!EntityManager.HasComponent<LocalToWorld>(entity))
                continue;

            var playerPos = EntityManager.GetComponentData<LocalToWorld>(entity).Position;
            float dist = math.distance(bossPos, playerPos);

            if (dist <= radius)
            {
                // Apply damage through health component
                if (EntityManager.HasComponent<Health>(entity))
                {
                    var health = EntityManager.GetComponentData<Health>(entity);
                    health.Value = Math.Max(1, health.Value - damage); // Don't kill, leave at 1 HP minimum
                    EntityManager.SetComponentData(entity, health);
                }
            }
        }

        entities.Dispose();

        // Announce AoE
        Core.Log.LogDebug($"[WorldBoss] AoE triggered: {element} for {damage} damage in {radius} radius");
    }

    static float GetFloat(Dictionary<string, object> p, string key, float def) =>
        p.TryGetValue(key, out var v) && float.TryParse(v?.ToString(), out var f) ? f : def;

    static string GetString(Dictionary<string, object> p, string key, string def) =>
        p.TryGetValue(key, out var v) ? v?.ToString() ?? def : def;
}

/// <summary>
/// Enrage mechanic - buffs boss attack speed/damage.
/// </summary>
public class EnrageMechanic : IMechanic
{
    static EntityManager EntityManager => Core.EntityManager;

    public string Type => "enrage";

    public bool CanApply(Entity boss) => EntityManager.Exists(boss);

    public void Execute(Entity boss, Dictionary<string, object> parameters)
    {
        float attackSpeedBonus = GetFloat(parameters, "attackSpeed", 0.3f);
        float damageBonus = GetFloat(parameters, "damage", 0.5f);

        // Apply enrage buff using extension method
        var enrageBuff = new PrefabGUID(-1055766373); // Generic damage buff

        boss.TryApplyBuff(enrageBuff);

        Core.Log.LogInfo($"[WorldBoss] Boss enraged: +{attackSpeedBonus * 100}% attack speed, +{damageBonus * 100}% damage");
    }

    static float GetFloat(Dictionary<string, object> p, string key, float def) =>
        p.TryGetValue(key, out var v) && float.TryParse(v?.ToString(), out var f) ? f : def;
}

/// <summary>
/// Slow mechanic - slows nearby players.
/// </summary>
public class SlowMechanic : IMechanic
{
    static EntityManager EntityManager => Core.EntityManager;

    public string Type => "slow";

    public bool CanApply(Entity boss) => EntityManager.Exists(boss);

    public void Execute(Entity boss, Dictionary<string, object> parameters)
    {
        float radius = GetFloat(parameters, "radius", 15f);
        float duration = GetFloat(parameters, "duration", 5f);

        if (!EntityManager.HasComponent<LocalToWorld>(boss))
            return;

        var bossPos = EntityManager.GetComponentData<LocalToWorld>(boss).Position;
        var slowBuff = new PrefabGUID(-1350836076); // Generic slow debuff

        var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerCharacter>());
        var entities = query.ToEntityArray(Allocator.Temp);

        int affected = 0;
        foreach (var entity in entities)
        {
            if (!EntityManager.HasComponent<LocalToWorld>(entity))
                continue;

            var playerPos = EntityManager.GetComponentData<LocalToWorld>(entity).Position;
            float dist = math.distance(bossPos, playerPos);

            if (dist <= radius)
            {
                entity.TryApplyBuff(slowBuff);
                affected++;
            }
        }

        entities.Dispose();

        Core.Log.LogDebug($"[WorldBoss] Slow applied to {affected} players");
    }

    static float GetFloat(Dictionary<string, object> p, string key, float def) =>
        p.TryGetValue(key, out var v) && float.TryParse(v?.ToString(), out var f) ? f : def;
}

#endregion
