using ProjectM.Network;
using Stunlock.Core;
using System.Collections.Generic;
using VAuction.Domain;
using VAuction.Persistence;

namespace VAuction.Services;

internal static class EscrowService
{
    public static void AddToEscrow(ulong steamId, string auctionKey, PrefabGUID prefab, int amount)
    {
        EscrowRepository.Add(steamId, new EscrowEntry
        {
            AuctionKey = auctionKey ?? string.Empty,
            PrefabGuid = prefab.GuidHash,
            Quantity = amount
        });
        EscrowRepository.Save();
    }

    public static int GetEscrowCount(ulong steamId)
    {
        if (EscrowRepository.EscrowBySteamId.TryGetValue(steamId, out List<EscrowEntry> list))
        {
            return list?.Count ?? 0;
        }
        return 0;
    }

    public static int ClaimAll(User user, Unity.Entities.Entity character)
    {
        ulong steamId = user.PlatformId;
        List<EscrowEntry> entries = EscrowRepository.TakeAll(steamId);
        if (entries.Count == 0) return 0;

        int claimed = 0;
        List<EscrowEntry> failed = [];

        foreach (EscrowEntry entry in entries)
        {
            PrefabGUID prefab = new(entry.PrefabGuid);
            int qty = entry.Quantity;

            if (qty <= 0)
            {
                continue;
            }

            bool ok = InventoryService.TryAdd(character, prefab, qty);
            if (ok)
            {
                claimed++;
            }
            else
            {
                failed.Add(entry);
            }
        }

        // Put failures back into escrow to avoid item loss.
        foreach (EscrowEntry entry in failed)
        {
            EscrowRepository.Add(steamId, entry);
        }

        EscrowRepository.Save();
        return claimed;
    }
}

