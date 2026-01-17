using Bloodcraft.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bloodcraft.Systems.WorldBoss;

/// <summary>
/// Handles persistence of World Boss configurations to JSON.
/// </summary>
public static class WorldBossDatabase
{
    static readonly string CONFIG_PATH = Path.Combine(BepInEx.Paths.ConfigPath, "Bloodcraft");
    static readonly string DATABASE_FILE = Path.Combine(CONFIG_PATH, "WorldBosses.json");

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>All configured world bosses.</summary>
    public static List<WorldBossModel> Bosses { get; private set; } = new();

    /// <summary>
    /// Load bosses from JSON file.
    /// </summary>
    public static void Load()
    {
        try
        {
            if (!Directory.Exists(CONFIG_PATH))
                Directory.CreateDirectory(CONFIG_PATH);

            if (File.Exists(DATABASE_FILE))
            {
                var json = File.ReadAllText(DATABASE_FILE);
                var loaded = JsonSerializer.Deserialize<List<WorldBossModel>>(json, JsonOptions);
                Bosses = loaded ?? new List<WorldBossModel>();
                Core.Log.LogInfo($"[WorldBoss] Loaded {Bosses.Count} boss configurations.");
            }
            else
            {
                Bosses = new List<WorldBossModel>();
                Save(); // Create empty file
                Core.Log.LogInfo("[WorldBoss] Created empty boss database.");
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"[WorldBoss] Failed to load database: {ex.Message}");
            Bosses = new List<WorldBossModel>();
        }
    }

    /// <summary>
    /// Save bosses to JSON file.
    /// </summary>
    public static void Save()
    {
        try
        {
            if (!Directory.Exists(CONFIG_PATH))
                Directory.CreateDirectory(CONFIG_PATH);

            // Create a clean copy without runtime state for serialization
            var toSave = new List<WorldBossSerializable>();
            foreach (var boss in Bosses)
            {
                toSave.Add(new WorldBossSerializable
                {
                    Name = boss.Name,
                    NameHash = boss.NameHash,
                    PrefabGUID = boss.PrefabGUID,
                    Level = boss.Level,
                    HealthMultiplier = boss.HealthMultiplier,
                    Lifetime = boss.Lifetime,
                    X = boss.X,
                    Y = boss.Y,
                    Z = boss.Z,
                    SpawnTime = boss.SpawnTime,
                    LootTable = boss.LootTable,
                    Mechanics = boss.Mechanics.ConvertAll(m => new WorldBossMechanicSerializable
                    {
                        Id = m.Id,
                        Type = m.Type,
                        Enabled = m.Enabled,
                        Trigger = m.Trigger,
                        Parameters = m.Parameters
                    })
                });
            }

            var json = JsonSerializer.Serialize(toSave, JsonOptions);
            File.WriteAllText(DATABASE_FILE, json);
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"[WorldBoss] Failed to save database: {ex.Message}");
        }
    }

    /// <summary>
    /// Get a boss by name (case-insensitive).
    /// </summary>
    public static WorldBossModel GetBoss(string name)
    {
        return Bosses.Find(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get a boss by its unique hash.
    /// </summary>
    public static WorldBossModel GetBossByHash(string nameHash)
    {
        return Bosses.Find(b => b.NameHash == nameHash);
    }

    /// <summary>
    /// Add a new boss configuration.
    /// </summary>
    public static bool AddBoss(WorldBossModel boss)
    {
        if (GetBoss(boss.Name) != null)
            return false; // Already exists

        // Generate unique hash
        boss.NameHash = Guid.NewGuid().ToString("N")[..8];
        Bosses.Add(boss);
        Save();
        return true;
    }

    /// <summary>
    /// Remove a boss configuration.
    /// </summary>
    public static bool RemoveBoss(string name)
    {
        var boss = GetBoss(name);
        if (boss == null)
            return false;

        Bosses.Remove(boss);
        Save();
        return true;
    }

    /// <summary>
    /// Get all active (spawned) bosses.
    /// </summary>
    public static IEnumerable<WorldBossModel> GetActiveBosses()
    {
        foreach (var boss in Bosses)
        {
            if (boss.IsActive)
                yield return boss;
        }
    }
}

#region Serializable Models (exclude runtime state)

internal class WorldBossSerializable
{
    public string Name { get; set; }
    public string NameHash { get; set; }
    public int PrefabGUID { get; set; }
    public int Level { get; set; }
    public float HealthMultiplier { get; set; }
    public float Lifetime { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public string SpawnTime { get; set; }
    public List<WorldBossLoot> LootTable { get; set; }
    public List<WorldBossMechanicSerializable> Mechanics { get; set; }
}

internal class WorldBossMechanicSerializable
{
    public string Id { get; set; }
    public string Type { get; set; }
    public bool Enabled { get; set; }
    public MechanicTrigger Trigger { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}

#endregion
