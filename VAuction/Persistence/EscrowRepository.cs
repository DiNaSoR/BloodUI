using System.Collections.Generic;
using System.IO;
using VAuction.Domain;

namespace VAuction.Persistence;

internal static class EscrowRepository
{
    static readonly object _lock = new();

    static string RootPath => Path.Combine(BepInEx.Paths.ConfigPath, "VAuction");
    static string EscrowPath => Path.Combine(RootPath, "escrow.json");

    public static Dictionary<ulong, List<EscrowEntry>> EscrowBySteamId { get; private set; } = [];

    public static void Initialize()
    {
        lock (_lock)
        {
            EscrowBySteamId = JsonStore.LoadOrDefault(EscrowPath, new Dictionary<ulong, List<EscrowEntry>>());
        }
    }

    public static void Save()
    {
        lock (_lock)
        {
            JsonStore.SaveAtomic(EscrowPath, EscrowBySteamId);
        }
    }

    public static void Add(ulong steamId, EscrowEntry entry)
    {
        lock (_lock)
        {
            if (!EscrowBySteamId.TryGetValue(steamId, out var list))
            {
                list = [];
                EscrowBySteamId[steamId] = list;
            }
            list.Add(entry);
        }
    }

    public static List<EscrowEntry> TakeAll(ulong steamId)
    {
        lock (_lock)
        {
            if (!EscrowBySteamId.TryGetValue(steamId, out var list) || list.Count == 0)
            {
                return [];
            }

            EscrowBySteamId.Remove(steamId);
            return list;
        }
    }
}

