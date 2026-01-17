using Bloodcraft.Systems.Familiars;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Behaviours;
using Stunlock.Core;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Bloodcraft.Utilities.EntityQueries;
using static Bloodcraft.Utilities.Familiars;

namespace Bloodcraft.Services;
internal class FamiliarService
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly WaitForSeconds _delay = new(10f);
    static readonly WaitForSeconds _smartUpdateDelay = new(0.5f);

    // Distance thresholds for speed boost
    const float CATCH_UP_DISTANCE = 15f;      // Start boosting speed at this distance
    const float NORMAL_DISTANCE = 8f;         // Return to normal speed at this distance
    const float SPEED_BOOST_MULTIPLIER = 2f;  // How much faster when catching up
    const float MIN_RUN_SPEED = 7f;           // Minimum run speed

    static readonly ComponentType[] _familiarAllComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<Follower>()),
        ComponentType.ReadOnly(Il2CppType.Of<TeamReference>()),
        ComponentType.ReadOnly(Il2CppType.Of<BlockFeedBuff>())
    ];

    static QueryDesc _familiarQueryDesc;

    static bool _shouldDestroy = true;
    public FamiliarService()
    {
        _familiarQueryDesc = EntityManager.CreateQueryDesc(_familiarAllComponents, options: EntityQueryOptions.IncludeDisabled);
        DisabledFamiliarPositionUpdateRoutine().Start();
        SmartFamiliarUpdateRoutine().Start();
    }
    
    /// <summary>
    /// Smart AI routine that runs frequently to:
    /// 1. Boost speed when far from player, normal speed when close
    /// 2. Sync familiar aggro with player's combat target
    /// </summary>
    static IEnumerator SmartFamiliarUpdateRoutine()
    {
        while (true)
        {
            yield return _smartUpdateDelay;

            foreach (var (steamId, familiarData) in ActiveFamiliarManager.ActiveFamiliars)
            {
                if (!steamId.TryGetPlayerInfo(out var playerInfo)) continue;
                if (familiarData.Dismissed || !familiarData.Familiar.Exists()) continue;

                Entity playerCharacter = playerInfo.CharEntity;
                Entity familiar = familiarData.Familiar;

                // 1. Distance-based speed adjustment
                UpdateFamiliarSpeed(playerCharacter, familiar);

                // 2. Smart aggro sync - attack what player is attacking
                SyncFamiliarAggro(playerCharacter, familiar);

                yield return null;
            }
        }
    }

    /// <summary>
    /// Boost familiar speed when far from player, return to normal when close
    /// </summary>
    static void UpdateFamiliarSpeed(Entity playerCharacter, Entity familiar)
    {
        if (!familiar.TryGetComponent(out AiMoveSpeeds aiMoveSpeeds)) return;

        float3 playerPos = playerCharacter.GetPosition();
        float3 familiarPos = familiar.GetPosition();
        float distance = math.distance(playerPos, familiarPos);

        // Get the original/base run speed from prefab
        PrefabGUID familiarId = familiar.GetPrefabGuid();
        if (!Core.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(familiarId, out Entity originalPrefab)) return;
        if (!originalPrefab.TryGetComponent(out AiMoveSpeeds originalSpeeds)) return;

        float baseRunSpeed = Math.Max(originalSpeeds.Run._Value, MIN_RUN_SPEED);

        if (distance >= CATCH_UP_DISTANCE)
        {
            // Far from player - boost speed to catch up
            float boostedSpeed = baseRunSpeed * SPEED_BOOST_MULTIPLIER;
            
            if (Math.Abs(aiMoveSpeeds.Run._Value - boostedSpeed) > 0.1f)
            {
                familiar.With((ref AiMoveSpeeds speeds) => speeds.Run._Value = boostedSpeed);
            }
        }
        else if (distance <= NORMAL_DISTANCE)
        {
            // Close to player - normal speed
            if (Math.Abs(aiMoveSpeeds.Run._Value - baseRunSpeed) > 0.1f)
            {
                familiar.With((ref AiMoveSpeeds speeds) => speeds.Run._Value = baseRunSpeed);
            }
        }
        // Between NORMAL_DISTANCE and CATCH_UP_DISTANCE: keep current speed (hysteresis)
    }

    /// <summary>
    /// Sync familiar aggro with player's combat targets
    /// Makes familiar attack what the player is attacking
    /// </summary>
    static void SyncFamiliarAggro(Entity playerCharacter, Entity familiar)
    {
        // Only sync if familiar is in combat-ready state
        if (!familiar.TryGetComponent(out BehaviourTreeState behaviourTreeState)) return;
        if (!familiar.EligibleForCombat()) return;

        // Get player's current aggro targets
        if (!playerCharacter.TryGetBuffer<InverseAggroBufferElement>(out var playerAggroBuffer)) return;
        if (playerAggroBuffer.IsEmpty) return;

        // Get familiar's current aggro buffer
        if (!familiar.TryGetBuffer<AggroBuffer>(out var familiarAggroBuffer)) return;

        // Find targets the player is fighting that familiar isn't targeting
        HashSet<Entity> familiarTargets = new();
        foreach (AggroBuffer entry in familiarAggroBuffer)
        {
            if (entry.Entity.Exists())
                familiarTargets.Add(entry.Entity);
        }

        List<Entity> newTargets = new();
        foreach (InverseAggroBufferElement playerTarget in playerAggroBuffer)
        {
            Entity target = playerTarget.Entity;
            if (target.Exists() && !familiarTargets.Contains(target))
            {
                newTargets.Add(target);
            }
        }

        // Add new targets to familiar's aggro
        if (newTargets.Count > 0)
        {
            AddToFamiliarAggroBuffer(playerCharacter, familiar, newTargets);
            
            // If familiar is not in combat, switch to combat mode
            if (!behaviourTreeState.Value.Equals(GenericEnemyState.Combat))
            {
                familiar.With((ref Follower follower) => follower.ModeModifiable._Value = 1);
                familiar.With((ref BehaviourTreeState state) => state.Value = GenericEnemyState.Combat);
            }
        }
    }

    static IEnumerator DisabledFamiliarPositionUpdateRoutine()
    {
        if (_shouldDestroy) DestroyFamiliars();

        while (true)
        {
            yield return _delay;

            foreach (var (steamId, familiarData) in ActiveFamiliarManager.ActiveFamiliars)
            {
                if (!steamId.TryGetPlayerInfo(out var playerInfo)) continue;
                else if (familiarData.Dismissed)
                {
                    TryReturnFamiliar(playerInfo.CharEntity, familiarData.Familiar);
                }

                yield return null;
            }
        }
    }
    static void DestroyFamiliars()
    {
        var entities = _familiarQueryDesc.EntityQuery.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                Entity servant = FindFamiliarServant(entity);
                Entity coffin = GetServantCoffin(servant);

                if (servant.Exists())
                {
                    FamiliarBindingSystem.RemoveDropTable(servant);
                    servant.Remove<Disabled>();
                    servant.Destroy();
                }

                if (coffin.Exists())
                {
                    coffin.Remove<Disabled>();
                    coffin.Destroy();
                }

                if (entity.Exists())
                {
                    entity.Remove<Disabled>();
                    FamiliarBindingSystem.RemoveDropTable(entity);
                    StatChangeUtility.KillOrDestroyEntity(EntityManager, entity, Entity.Null, Entity.Null, Core.ServerTime, StatChangeReason.Default, true);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"[PlayerService] DestroyFamiliars() - {ex}");
        }

        _shouldDestroy = false;
    }
}
