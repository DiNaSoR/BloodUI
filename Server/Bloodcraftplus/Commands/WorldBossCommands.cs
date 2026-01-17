using Bloodcraft.Services;
// ============================================================================
// World Boss Commands - Admin Interface
// Inspired by BloodyBoss by @oscarpedrero
// https://github.com/oscarpedrero/BloodyBoss
// ============================================================================

using Bloodcraft.Systems.WorldBoss;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using VampireCommandFramework;

namespace Bloodcraft.Commands;

[CommandGroup(name: "worldboss", "wb")]
internal static class WorldBossCommands
{
    static EntityManager EntityManager => Core.EntityManager;

    [Command(name: "create", adminOnly: true, usage: ".wb create <name> <prefabGUID> [level]", description: "Create a new world boss configuration.")]
    public static void CreateBossCommand(ChatCommandContext ctx, string name, int prefabGUID, int level = 90)
    {
        if (!ConfigService.WorldBossSystem)
        {
            LocalizationService.HandleReply(ctx, "World Boss system is not enabled.");
            return;
        }

        if (WorldBossDatabase.GetBoss(name) != null)
        {
            LocalizationService.HandleReply(ctx, $"Boss '<color=#FFD700>{name}</color>' already exists.");
            return;
        }

        // Verify prefab exists
        var guid = new PrefabGUID(prefabGUID);
        if (!guid.HasValue())
        {
            LocalizationService.HandleReply(ctx, $"Invalid PrefabGUID: <color=red>{prefabGUID}</color>");
            return;
        }

        if (WorldBossSystem.CreateBoss(name, prefabGUID, level))
        {
            string assetName = guid.GetLocalizedName();
            LocalizationService.HandleReply(ctx, $"Created boss '<color=#FFD700>{name}</color>' using <color=white>{assetName}</color> at level <color=yellow>{level}</color>.");
            LocalizationService.HandleReply(ctx, "Use <color=#00FF00>.wb setlocation</color> to set spawn position.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"Failed to create boss '<color=red>{name}</color>'.");
        }
    }

    [Command(name: "setlocation", shortHand: "setloc", adminOnly: true, usage: ".wb setloc <name>", description: "Set boss spawn location to your current position.")]
    public static void SetLocationCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.WorldBossSystem)
        {
            LocalizationService.HandleReply(ctx, "World Boss system is not enabled.");
            return;
        }

        var boss = WorldBossDatabase.GetBoss(name);
        if (boss == null)
        {
            LocalizationService.HandleReply(ctx, $"Boss '<color=red>{name}</color>' not found.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        var localToWorld = EntityManager.GetComponentData<LocalToWorld>(character);

        if (WorldBossSystem.SetLocation(name, localToWorld.Position.x, localToWorld.Position.y, localToWorld.Position.z))
        {
            LocalizationService.HandleReply(ctx, $"Set '<color=#FFD700>{name}</color>' spawn location to <color=yellow>({boss.X:F0}, {boss.Y:F0}, {boss.Z:F0})</color>.");
        }
    }

    [Command(name: "settime", adminOnly: true, usage: ".wb settime <name> <HH:mm>", description: "Set daily spawn time for a boss.")]
    public static void SetTimeCommand(ChatCommandContext ctx, string name, string time)
    {
        if (!ConfigService.WorldBossSystem)
        {
            LocalizationService.HandleReply(ctx, "World Boss system is not enabled.");
            return;
        }

        // Validate time format
        if (!System.DateTime.TryParseExact(time, "HH:mm", null, System.Globalization.DateTimeStyles.None, out _))
        {
            LocalizationService.HandleReply(ctx, "Invalid time format. Use <color=yellow>HH:mm</color> (e.g., 20:00).");
            return;
        }

        if (WorldBossSystem.SetSpawnTime(name, time))
        {
            LocalizationService.HandleReply(ctx, $"Set '<color=#FFD700>{name}</color>' spawn time to <color=green>{time}</color> daily.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"Boss '<color=red>{name}</color>' not found.");
        }
    }

    [Command(name: "spawn", adminOnly: true, usage: ".wb spawn <name>", description: "Force spawn a world boss.")]
    public static void SpawnCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.WorldBossSystem)
        {
            LocalizationService.HandleReply(ctx, "World Boss system is not enabled.");
            return;
        }

        var boss = WorldBossDatabase.GetBoss(name);
        if (boss == null)
        {
            LocalizationService.HandleReply(ctx, $"Boss '<color=red>{name}</color>' not found.");
            return;
        }

        if (boss.IsActive)
        {
            LocalizationService.HandleReply(ctx, $"Boss '<color=#FFD700>{name}</color>' is already active!");
            return;
        }

        if (boss.X == 0 && boss.Y == 0 && boss.Z == 0)
        {
            LocalizationService.HandleReply(ctx, "Spawn location not set. Use <color=#00FF00>.wb setloc</color> first.");
            return;
        }

        WorldBossSystem.SpawnBoss(boss);
        LocalizationService.HandleReply(ctx, $"Spawning '<color=#FFD700>{name}</color>'...");
    }

    [Command(name: "despawn", adminOnly: true, usage: ".wb despawn <name>", description: "Force despawn a world boss.")]
    public static void DespawnCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.WorldBossSystem)
        {
            LocalizationService.HandleReply(ctx, "World Boss system is not enabled.");
            return;
        }

        var boss = WorldBossDatabase.GetBoss(name);
        if (boss == null)
        {
            LocalizationService.HandleReply(ctx, $"Boss '<color=red>{name}</color>' not found.");
            return;
        }

        if (!boss.IsActive)
        {
            LocalizationService.HandleReply(ctx, $"Boss '<color=#FFD700>{name}</color>' is not currently active.");
            return;
        }

        WorldBossSystem.DespawnBoss(boss);
        LocalizationService.HandleReply(ctx, $"Despawned '<color=#FFD700>{name}</color>'.");
    }

    [Command(name: "list", adminOnly: true, usage: ".wb list", description: "List all configured world bosses.")]
    public static void ListCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.WorldBossSystem)
        {
            LocalizationService.HandleReply(ctx, "World Boss system is not enabled.");
            return;
        }

        var bosses = WorldBossSystem.GetAllBosses();

        if (bosses.Count == 0)
        {
            LocalizationService.HandleReply(ctx, "No world bosses configured. Use <color=#00FF00>.wb create</color>.");
            return;
        }

        LocalizationService.HandleReply(ctx, $"<color=#FFD700>World Bosses ({bosses.Count})</color>:");
        foreach (var boss in bosses)
        {
            string status = boss.IsActive ? "<color=green>ACTIVE</color>" : 
                           boss.IsPaused ? "<color=yellow>PAUSED</color>" : 
                           "<color=gray>Idle</color>";
            string time = string.IsNullOrEmpty(boss.SpawnTime) ? "Not scheduled" : boss.SpawnTime;
            LocalizationService.HandleReply(ctx, $"  <color=white>{boss.Name}</color> [Lv{boss.Level}] - {status} | Time: {time}");
        }
    }

    [Command(name: "info", adminOnly: true, usage: ".wb info <name>", description: "Show detailed info about a world boss.")]
    public static void InfoCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.WorldBossSystem)
        {
            LocalizationService.HandleReply(ctx, "World Boss system is not enabled.");
            return;
        }

        var boss = WorldBossDatabase.GetBoss(name);
        if (boss == null)
        {
            LocalizationService.HandleReply(ctx, $"Boss '<color=red>{name}</color>' not found.");
            return;
        }

        var guid = new PrefabGUID(boss.PrefabGUID);
        var scaling = WorldBossScaling.GetScalingInfo(boss);

        LocalizationService.HandleReply(ctx, $"<color=#FFD700>═══ {boss.Name} ═══</color>");
        LocalizationService.HandleReply(ctx, $"Prefab: <color=white>{guid.GetLocalizedName()}</color> ({boss.PrefabGUID})");
        LocalizationService.HandleReply(ctx, $"Level: <color=yellow>{boss.Level}</color> | HP Mult: <color=yellow>x{boss.HealthMultiplier:F1}</color>");
        LocalizationService.HandleReply(ctx, $"Position: <color=gray>({boss.X:F0}, {boss.Y:F0}, {boss.Z:F0})</color>");
        LocalizationService.HandleReply(ctx, $"Lifetime: <color=white>{boss.Lifetime / 60:F0} minutes</color>");
        LocalizationService.HandleReply(ctx, $"Spawn Time: <color=green>{(string.IsNullOrEmpty(boss.SpawnTime) ? "Not set" : boss.SpawnTime)}</color>");
        LocalizationService.HandleReply(ctx, $"Scaling: <color=cyan>Phase {scaling.Phase} ({scaling.PhaseInfo.Name})</color> | HP x{scaling.HealthMultiplier:F2}");
        LocalizationService.HandleReply(ctx, $"Loot Items: <color=white>{boss.LootTable.Count}</color> | Mechanics: <color=white>{boss.Mechanics.Count}</color>");
    }

    [Command(name: "addloot", adminOnly: true, usage: ".wb addloot <name> <itemGUID> <amount> [chance]", description: "Add item to boss loot table.")]
    public static void AddLootCommand(ChatCommandContext ctx, string name, int itemGUID, int amount, float chance = 1f)
    {
        if (!ConfigService.WorldBossSystem)
        {
            LocalizationService.HandleReply(ctx, "World Boss system is not enabled.");
            return;
        }

        var itemPrefab = new PrefabGUID(itemGUID);
        if (!itemPrefab.HasValue())
        {
            LocalizationService.HandleReply(ctx, $"Invalid item PrefabGUID: <color=red>{itemGUID}</color>");
            return;
        }

        string itemName = itemPrefab.GetLocalizedName();

        if (WorldBossSystem.AddLoot(name, itemName, itemGUID, amount, chance))
        {
            LocalizationService.HandleReply(ctx, $"Added <color=#FFD700>{itemName}</color> x{amount} ({chance * 100:F0}% chance) to '<color=white>{name}</color>'.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"Boss '<color=red>{name}</color>' not found.");
        }
    }

    [Command(name: "addmechanic", shortHand: "addmech", adminOnly: true, usage: ".wb addmech <name> <type> <hpThreshold>", description: "Add a combat mechanic to a boss.")]
    public static void AddMechanicCommand(ChatCommandContext ctx, string name, string type, float hpThreshold)
    {
        if (!ConfigService.WorldBossSystem)
        {
            LocalizationService.HandleReply(ctx, "World Boss system is not enabled.");
            return;
        }

        // Validate mechanic type
        var validTypes = new HashSet<string> { "stun", "aoe", "enrage", "slow" };
        if (!validTypes.Contains(type.ToLower()))
        {
            LocalizationService.HandleReply(ctx, $"Invalid mechanic type. Valid types: <color=yellow>stun, aoe, enrage, slow</color>");
            return;
        }

        if (WorldBossSystem.AddMechanic(name, type.ToLower(), hpThreshold))
        {
            LocalizationService.HandleReply(ctx, $"Added '<color=#FF6600>{type}</color>' mechanic at {hpThreshold}% HP to '<color=white>{name}</color>'.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"Boss '<color=red>{name}</color>' not found.");
        }
    }

    [Command(name: "pause", adminOnly: true, usage: ".wb pause <name>", description: "Pause scheduled spawning for a boss.")]
    public static void PauseCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.WorldBossSystem)
        {
            LocalizationService.HandleReply(ctx, "World Boss system is not enabled.");
            return;
        }

        var boss = WorldBossDatabase.GetBoss(name);
        if (boss == null)
        {
            LocalizationService.HandleReply(ctx, $"Boss '<color=red>{name}</color>' not found.");
            return;
        }

        boss.IsPaused = !boss.IsPaused;
        WorldBossDatabase.Save();

        string status = boss.IsPaused ? "<color=yellow>paused</color>" : "<color=green>resumed</color>";
        LocalizationService.HandleReply(ctx, $"Boss '<color=#FFD700>{name}</color>' spawning {status}.");
    }

    [Command(name: "delete", adminOnly: true, usage: ".wb delete <name>", description: "Delete a world boss configuration.")]
    public static void DeleteCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.WorldBossSystem)
        {
            LocalizationService.HandleReply(ctx, "World Boss system is not enabled.");
            return;
        }

        if (WorldBossDatabase.RemoveBoss(name))
        {
            LocalizationService.HandleReply(ctx, $"Deleted boss '<color=#FFD700>{name}</color>'.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"Boss '<color=red>{name}</color>' not found.");
        }
    }

    [Command(name: "teleport", shortHand: "tp", adminOnly: false, usage: ".wb tp <name>", description: "Teleport to an active world boss.")]
    public static void TeleportCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.WorldBossSystem)
        {
            LocalizationService.HandleReply(ctx, "World Boss system is not enabled.");
            return;
        }

        var boss = WorldBossDatabase.GetBoss(name);
        if (boss == null)
        {
            LocalizationService.HandleReply(ctx, $"Boss '<color=red>{name}</color>' not found.");
            return;
        }

        if (!boss.IsActive)
        {
            LocalizationService.HandleReply(ctx, $"Boss '<color=#FFD700>{name}</color>' is not currently active.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        var translation = EntityManager.GetComponentData<Translation>(character);
        translation.Value = new Unity.Mathematics.float3(boss.X, boss.Y, boss.Z);
        EntityManager.SetComponentData(character, translation);

        LocalizationService.HandleReply(ctx, $"Teleported to '<color=#FFD700>{name}</color>'!");
    }
}
