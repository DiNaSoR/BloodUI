using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Systems.WorldBoss;

/// <summary>
/// Dynamic scaling and phase management for World Bosses.
/// </summary>
public static class WorldBossScaling
{
    static EntityManager EntityManager => Core.EntityManager;

    #region Phase Definitions

    public static readonly Dictionary<int, PhaseInfo> Phases = new()
    {
        { 1, new PhaseInfo("Normal", "#FFFFFF", 1.0f) },
        { 2, new PhaseInfo("Hard", "#FFFF00", 1.5f) },
        { 3, new PhaseInfo("Epic", "#FF6600", 2.0f) },
        { 4, new PhaseInfo("Legendary", "#FF0000", 3.0f) }
    };

    public class PhaseInfo
    {
        public string Name { get; }
        public string Color { get; }
        public float DamageMultiplier { get; }

        public PhaseInfo(string name, string color, float damageMultiplier)
        {
            Name = name;
            Color = color;
            DamageMultiplier = damageMultiplier;
        }
    }

    #endregion

    #region Scaling Calculations

    /// <summary>
    /// Calculate total health multiplier based on configuration and player count.
    /// </summary>
    public static float CalculateHealthMultiplier(WorldBossModel boss)
    {
        float baseMultiplier = boss.HealthMultiplier;

        if (!ConfigService.WorldBossEnableDynamicScaling)
            return baseMultiplier;

        int onlinePlayers = GetOnlinePlayerCount();
        int maxPlayers = ConfigService.WorldBossMaxPlayersForScaling;
        float perPlayer = ConfigService.WorldBossHealthPerPlayer;

        // Clamp player count
        int effectivePlayers = Math.Min(onlinePlayers, maxPlayers);

        // Calculate bonus: base * (1 + perPlayer * players)
        float playerBonus = 1f + (perPlayer * effectivePlayers);

        // Progressive difficulty bonus from consecutive spawns
        float progressiveBonus = 1f;
        if (ConfigService.WorldBossEnableProgressiveDifficulty)
        {
            progressiveBonus = 1f + (boss.ConsecutiveSpawns * 0.1f); // +10% per spawn
            progressiveBonus = Math.Min(progressiveBonus, 2f); // Cap at 2x
        }

        return baseMultiplier * playerBonus * progressiveBonus;
    }

    /// <summary>
    /// Calculate damage multiplier based on configuration and player count.
    /// </summary>
    public static float CalculateDamageMultiplier(WorldBossModel boss)
    {
        if (!ConfigService.WorldBossEnableDynamicScaling)
            return 1f;

        int onlinePlayers = GetOnlinePlayerCount();
        int maxPlayers = ConfigService.WorldBossMaxPlayersForScaling;
        float perPlayer = ConfigService.WorldBossDamagePerPlayer;

        int effectivePlayers = Math.Min(onlinePlayers, maxPlayers);

        return 1f + (perPlayer * effectivePlayers);
    }

    /// <summary>
    /// Get current scaling information for display.
    /// </summary>
    public static ScalingInfo GetScalingInfo(WorldBossModel boss)
    {
        int players = GetOnlinePlayerCount();
        float healthMult = CalculateHealthMultiplier(boss);
        float damageMult = CalculateDamageMultiplier(boss);
        int phase = GetPhaseFromConsecutiveSpawns(boss.ConsecutiveSpawns);

        return new ScalingInfo
        {
            OnlinePlayers = players,
            HealthMultiplier = healthMult,
            DamageMultiplier = damageMult,
            Phase = phase,
            PhaseInfo = Phases[phase],
            ConsecutiveSpawns = boss.ConsecutiveSpawns
        };
    }

    #endregion

    #region Phase Management

    /// <summary>
    /// Determine phase based on consecutive spawn count.
    /// </summary>
    static int GetPhaseFromConsecutiveSpawns(int spawns)
    {
        return spawns switch
        {
            >= 5 => 4, // Legendary
            >= 3 => 3, // Epic
            >= 1 => 2, // Hard
            _ => 1     // Normal
        };
    }

    /// <summary>
    /// Check if phase announcement should be made and send it.
    /// </summary>
    public static void CheckAndAnnouncePhase(WorldBossModel boss)
    {
        if (!ConfigService.WorldBossEnableDynamicScaling)
            return;

        var info = GetScalingInfo(boss);

        if (info.Phase > boss.LastAnnouncedPhase)
        {
            boss.LastAnnouncedPhase = info.Phase;
            AnnouncePhase(boss, info);
        }
    }

    /// <summary>
    /// Send phase announcement to all players.
    /// </summary>
    static void AnnouncePhase(WorldBossModel boss, ScalingInfo info)
    {
        string prefix = info.Phase switch
        {
            4 => "💀 LEGENDARY THREAT! ",
            3 => "⚡ EPIC ENCOUNTER! ",
            2 => "⚔️ ",
            _ => ""
        };

        string message = $"<color={info.PhaseInfo.Color}>{prefix}{boss.Name} [{info.PhaseInfo.Name}]</color> - " +
                        $"Phase {info.Phase} | {info.OnlinePlayers} players | " +
                        $"Damage x{info.DamageMultiplier:F1}";

        if (info.ConsecutiveSpawns > 0)
            message += $" | Consecutive: {info.ConsecutiveSpawns}";

        WorldBossTracker.BroadcastToAllPlayers(message);

        Core.Log.LogInfo($"[WorldBoss] Phase announcement: {boss.Name} -> Phase {info.Phase}");
    }

    #endregion

    #region Utility

    /// <summary>
    /// Get count of online players.
    /// </summary>
    public static int GetOnlinePlayerCount()
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

    #endregion
}

/// <summary>
/// Current scaling state information.
/// </summary>
public class ScalingInfo
{
    public int OnlinePlayers { get; set; }
    public float HealthMultiplier { get; set; }
    public float DamageMultiplier { get; set; }
    public int Phase { get; set; }
    public WorldBossScaling.PhaseInfo PhaseInfo { get; set; }
    public int ConsecutiveSpawns { get; set; }
}
