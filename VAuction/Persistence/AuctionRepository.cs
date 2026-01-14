using System;
using System.Collections.Generic;
using System.IO;
using VAuction.Domain;

namespace VAuction.Persistence;

internal static class AuctionRepository
{
    static readonly object _lock = new();

    static string RootPath => Path.Combine(BepInEx.Paths.ConfigPath, "VAuction");
    static string ActivePath => Path.Combine(RootPath, "auctions.json");
    static string HistoryPath => Path.Combine(RootPath, "history.json");

    public static Dictionary<ulong, AuctionListing> Active { get; private set; } = [];
    public static List<AuctionListing> History { get; private set; } = [];

    public static void Initialize()
    {
        lock (_lock)
        {
            Active = JsonStore.LoadOrDefault(ActivePath, new Dictionary<ulong, AuctionListing>());
            History = JsonStore.LoadOrDefault(HistoryPath, new List<AuctionListing>());
        }
    }

    public static void Save()
    {
        lock (_lock)
        {
            JsonStore.SaveAtomic(ActivePath, Active);
            JsonStore.SaveAtomic(HistoryPath, History);
        }
    }

    public static bool TryGet(ulong id, out AuctionListing listing)
    {
        lock (_lock)
        {
            return Active.TryGetValue(id, out listing);
        }
    }

    public static void Upsert(AuctionListing listing)
    {
        lock (_lock)
        {
            Active[listing.Id] = listing;
        }
    }

    public static void Remove(ulong id)
    {
        lock (_lock)
        {
            Active.Remove(id);
        }
    }

    public static void AppendHistory(AuctionListing listing)
    {
        lock (_lock)
        {
            History.Add(listing);
        }
    }
}

