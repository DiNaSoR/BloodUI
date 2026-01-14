using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VAuction.Domain;
using VAuction.EclipseBridge;
using VAuction.Persistence;
using VAuction.Services.Config;

namespace VAuction.Services;

internal sealed class AuctionService
{
    readonly object _lock = new();

    public AuctionService() { }

    public long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public bool TryCreateListing(User user, Unity.Entities.Entity character, PrefabGUID itemPrefab, int quantity,
        int startingBid, int buyNow, int durationHours, out string error, out AuctionListing created)
    {
        error = string.Empty;
        created = null;

        if (!VAuctionConfigService.Enabled)
        {
            error = "VAuction is disabled.";
            return false;
        }

        if (quantity <= 0)
        {
            error = "Quantity must be > 0.";
            return false;
        }

        int minHours = VAuctionConfigService.MinAuctionDurationHours;
        int maxHours = VAuctionConfigService.MaxAuctionDurationHours;
        if (durationHours < minHours || durationHours > maxHours)
        {
            error = $"Duration must be between {minHours} and {maxHours} hours.";
            return false;
        }

        int minPrice = VAuctionConfigService.MinimumPrice;
        int maxPrice = VAuctionConfigService.MaximumPrice;
        startingBid = PricingService.ClampPrice(startingBid, minPrice, maxPrice);
        buyNow = buyNow <= 0 ? 0 : PricingService.ClampPrice(buyNow, minPrice, maxPrice);

        if (buyNow > 0 && buyNow < startingBid)
        {
            error = "Buy-now must be >= starting bid (or 0 to disable).";
            return false;
        }

        ulong steamId = user.PlatformId;
        if (!InventoryService.TryGetInventoryEntity(character, out var inventoryEntity))
        {
            error = "Could not read your inventory.";
            return false;
        }

        // Listing fee is charged in primary currency upfront.
        PrefabGUID currency = new(VAuctionConfigService.PrimaryCurrencyPrefab);
        int listingFee = PricingService.CalculatePercentFee(startingBid, VAuctionConfigService.ListingFeePercent);
        if (listingFee > 0)
        {
            if (InventoryService.GetCount(inventoryEntity, currency) < listingFee)
            {
                error = $"Not enough currency for listing fee ({listingFee}).";
                return false;
            }
        }

        // Remove item first.
        if (InventoryService.GetCount(inventoryEntity, itemPrefab) < quantity)
        {
            error = "You do not have the required item quantity.";
            return false;
        }

        if (!InventoryService.TryRemove(inventoryEntity, itemPrefab, quantity))
        {
            error = "Failed to remove item from your inventory.";
            return false;
        }

        // Remove fee after item removal; if fee removal fails, refund the item.
        if (listingFee > 0 && !InventoryService.TryRemove(inventoryEntity, currency, listingFee))
        {
            InventoryService.TryAdd(character, itemPrefab, quantity);
            error = "Failed to remove listing fee from your inventory.";
            return false;
        }

        long now = NowUnix();
        ulong id = AuctionId.Create(now);
        string key = AuctionId.ToKey(id);

        string displayName = itemPrefab.GetLocalizedName();
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Equals("LocalizationKey.Empty", StringComparison.OrdinalIgnoreCase))
        {
            displayName = itemPrefab.GetPrefabName();
        }

        var listing = new AuctionListing
        {
            Id = id,
            AuctionKey = key,
            SellerSteamId = steamId,
            SellerName = user.CharacterName.Value,
            Category = InferCategory(itemPrefab),
            Item = new AuctionItem
            {
                PrefabGuid = itemPrefab.GuidHash,
                Quantity = quantity,
                DisplayName = displayName
            },
            CreatedUnix = now,
            ExpiresUnix = now + (durationHours * 3600L),
            Status = AuctionStatus.Active,
            StartingBid = startingBid,
            CurrentBid = 0,
            BuyNow = buyNow,
            CurrencyPrefabGuid = currency.GuidHash,
            HighestBidderSteamId = null,
            HighestBidderName = string.Empty
        };

        lock (_lock)
        {
            int activeForSeller = AuctionRepository.Active.Values.Count(v => v.Status == AuctionStatus.Active && v.SellerSteamId == steamId);
            if (activeForSeller >= VAuctionConfigService.MaxListingsPerPlayer)
            {
                // Refund item + fee via escrow to avoid inventory edge cases.
                EscrowService.AddToEscrow(steamId, key, itemPrefab, quantity);
                if (listingFee > 0) EscrowService.AddToEscrow(steamId, key, currency, listingFee);
                error = $"You already have the maximum active listings ({VAuctionConfigService.MaxListingsPerPlayer}).";
                return false;
            }

            AuctionRepository.Upsert(listing);
            AuctionRepository.Save();
        }

        created = listing;
        return true;
    }

    public bool TryPlaceBid(User user, Unity.Entities.Entity character, string auctionKey, int amount, out string error)
    {
        error = string.Empty;
        if (!VAuctionConfigService.Enabled)
        {
            error = "VAuction is disabled.";
            return false;
        }

        if (amount <= 0)
        {
            error = "Bid amount must be > 0.";
            return false;
        }

        if (!InventoryService.TryGetInventoryEntity(character, out var inventoryEntity))
        {
            error = "Could not read your inventory.";
            return false;
        }

        lock (_lock)
        {
            AuctionListing listing = FindActiveByKey(auctionKey);
            if (listing == null)
            {
                error = "Auction not found.";
                return false;
            }

            long now = NowUnix();
            if (listing.Status != AuctionStatus.Active || listing.ExpiresUnix <= now)
            {
                error = "Auction is not active.";
                return false;
            }

            ulong steamId = user.PlatformId;
            if (listing.SellerSteamId == steamId)
            {
                error = "You cannot bid on your own auction.";
                return false;
            }

            int minNext = CalculateMinNextBid(listing);
            if (amount < minNext)
            {
                error = $"Bid must be at least {minNext}.";
                return false;
            }

            PrefabGUID currency = new(listing.CurrencyPrefabGuid);
            if (InventoryService.GetCount(inventoryEntity, currency) < amount)
            {
                error = "Not enough currency.";
                return false;
            }

            // Take full amount from bidder and escrow it (by removing from inventory).
            if (!InventoryService.TryRemove(inventoryEntity, currency, amount))
            {
                error = "Failed to remove currency.";
                return false;
            }

            // Refund previous highest bidder to escrow (currency item).
            if (listing.CurrentBid > 0 && listing.HighestBidderSteamId.HasValue)
            {
                EscrowService.AddToEscrow(listing.HighestBidderSteamId.Value, listing.AuctionKey, currency, listing.CurrentBid);
            }

            listing.CurrentBid = amount;
            listing.HighestBidderSteamId = steamId;
            listing.HighestBidderName = user.CharacterName.Value;
            listing.BidHistory.Add(new AuctionBid
            {
                BidderSteamId = steamId,
                BidderName = user.CharacterName.Value,
                Amount = amount,
                Unix = now
            });

            // Anti-snipe
            int window = VAuctionConfigService.AntiSnipeWindowMinutes;
            int extension = VAuctionConfigService.AntiSnipeExtensionMinutes;
            if (window > 0 && extension > 0 && (listing.ExpiresUnix - now) <= (window * 60L))
            {
                listing.ExpiresUnix += (extension * 60L);
            }

            AuctionRepository.Upsert(listing);
            AuctionRepository.Save();
            return true;
        }
    }

    public bool TryBuyNow(User user, Unity.Entities.Entity character, string auctionKey, out string error)
    {
        error = string.Empty;
        if (!VAuctionConfigService.Enabled)
        {
            error = "VAuction is disabled.";
            return false;
        }

        if (!InventoryService.TryGetInventoryEntity(character, out var inventoryEntity))
        {
            error = "Could not read your inventory.";
            return false;
        }

        lock (_lock)
        {
            AuctionListing listing = FindActiveByKey(auctionKey);
            if (listing == null)
            {
                error = "Auction not found.";
                return false;
            }

            long now = NowUnix();
            if (listing.Status != AuctionStatus.Active || listing.ExpiresUnix <= now)
            {
                error = "Auction is not active.";
                return false;
            }

            if (listing.BuyNow <= 0)
            {
                error = "This auction has no buy-now price.";
                return false;
            }

            ulong steamId = user.PlatformId;
            if (listing.SellerSteamId == steamId)
            {
                error = "You cannot buy your own auction.";
                return false;
            }

            PrefabGUID currency = new(listing.CurrencyPrefabGuid);
            if (InventoryService.GetCount(inventoryEntity, currency) < listing.BuyNow)
            {
                error = "Not enough currency.";
                return false;
            }

            // Take buy-now currency from buyer.
            if (!InventoryService.TryRemove(inventoryEntity, currency, listing.BuyNow))
            {
                error = "Failed to remove currency.";
                return false;
            }

            // Refund highest bidder if any (their escrowed bid amount).
            if (listing.CurrentBid > 0 && listing.HighestBidderSteamId.HasValue)
            {
                EscrowService.AddToEscrow(listing.HighestBidderSteamId.Value, listing.AuctionKey, currency, listing.CurrentBid);
            }

            ResolveSale(listing, finalPrice: listing.BuyNow, winnerSteamId: steamId);

            AuctionRepository.Remove(listing.Id);
            AuctionRepository.AppendHistory(listing);
            AuctionRepository.Save();
            return true;
        }
    }

    public bool TryCancel(User user, Unity.Entities.Entity character, string auctionKey, out string error)
    {
        error = string.Empty;
        if (!VAuctionConfigService.Enabled)
        {
            error = "VAuction is disabled.";
            return false;
        }

        lock (_lock)
        {
            AuctionListing listing = FindActiveByKey(auctionKey);
            if (listing == null)
            {
                error = "Auction not found.";
                return false;
            }

            if (listing.SellerSteamId != user.PlatformId)
            {
                error = "You can only cancel your own auctions.";
                return false;
            }

            if (listing.CurrentBid > 0)
            {
                error = "You cannot cancel an auction that has bids.";
                return false;
            }

            listing.Status = AuctionStatus.Cancelled;
            PrefabGUID itemPrefab = new(listing.Item.PrefabGuid);
            EscrowService.AddToEscrow(listing.SellerSteamId, listing.AuctionKey, itemPrefab, listing.Item.Quantity);

            PrefabGUID currency = new(listing.CurrencyPrefabGuid);
            int fee = PricingService.CalculatePercentFee(listing.StartingBid, VAuctionConfigService.ListingFeePercent);
            int refund = PricingService.CalculatePercentFee(fee, VAuctionConfigService.CancelRefundPercent);
            if (refund > 0)
            {
                EscrowService.AddToEscrow(listing.SellerSteamId, listing.AuctionKey, currency, refund);
            }

            AuctionRepository.Remove(listing.Id);
            AuctionRepository.AppendHistory(listing);
            AuctionRepository.Save();
            return true;
        }
    }

    public AuctionListing FindActiveByKey(string auctionKey)
    {
        if (string.IsNullOrWhiteSpace(auctionKey)) return null;
        string key = auctionKey.Trim().ToLowerInvariant();
        return AuctionRepository.Active.Values.FirstOrDefault(v => v.Status == AuctionStatus.Active && v.AuctionKey.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public int CalculateMinNextBid(AuctionListing listing)
    {
        int minIncPercent = VAuctionConfigService.MinBidIncrementPercent;
        int current = listing.CurrentBid > 0 ? listing.CurrentBid : listing.StartingBid;
        int inc = PricingService.CalculatePercentFee(current, minIncPercent);
        if (inc < 1) inc = 1;
        return listing.CurrentBid > 0 ? listing.CurrentBid + inc : listing.StartingBid;
    }

    public void ProcessExpirations()
    {
        if (!VAuctionConfigService.Enabled) return;

        lock (_lock)
        {
            long now = NowUnix();
            List<AuctionListing> expired = AuctionRepository.Active.Values
                .Where(v => v.Status == AuctionStatus.Active && v.ExpiresUnix <= now)
                .ToList();

            if (expired.Count == 0) return;

            foreach (AuctionListing listing in expired)
            {
                ResolveExpiration(listing);
                AuctionRepository.Remove(listing.Id);
                AuctionRepository.AppendHistory(listing);
            }

            AuctionRepository.Save();
        }
    }

    void ResolveExpiration(AuctionListing listing)
    {
        PrefabGUID currency = new(listing.CurrencyPrefabGuid);

        if (listing.CurrentBid > 0 && listing.HighestBidderSteamId.HasValue)
        {
            ResolveSale(listing, listing.CurrentBid, listing.HighestBidderSteamId.Value);
        }
        else
        {
            // Return item to seller.
            listing.Status = AuctionStatus.Expired;
            EscrowService.AddToEscrow(listing.SellerSteamId, listing.AuctionKey, new PrefabGUID(listing.Item.PrefabGuid), listing.Item.Quantity);
        }
    }

    void ResolveSale(AuctionListing listing, int finalPrice, ulong winnerSteamId)
    {
        PrefabGUID currency = new(listing.CurrencyPrefabGuid);

        // Winner receives item.
        EscrowService.AddToEscrow(winnerSteamId, listing.AuctionKey, new PrefabGUID(listing.Item.PrefabGuid), listing.Item.Quantity);

        // Seller receives payout (minus tax).
        int tax = PricingService.CalculatePercentFee(finalPrice, VAuctionConfigService.SaleTaxPercent);
        int payout = Math.Max(0, finalPrice - tax);
        if (payout > 0)
        {
            EscrowService.AddToEscrow(listing.SellerSteamId, listing.AuctionKey, currency, payout);
        }

        listing.Status = AuctionStatus.Sold;
    }

    AuctionCategory InferCategory(PrefabGUID prefab)
    {
        try
        {
            string name = prefab.GetPrefabName();
            if (name.Contains("Weapon", StringComparison.OrdinalIgnoreCase)) return AuctionCategory.Weapons;
            if (name.Contains("Armor", StringComparison.OrdinalIgnoreCase)) return AuctionCategory.Armor;
            return AuctionCategory.Resources;
        }
        catch
        {
            return AuctionCategory.Resources;
        }
    }

    public void SendBrowsePage(User user, string categoryRaw, int page)
    {
        if (Core.EclipseSyncService == null || !Core.EclipseSyncService.IsEnabled) return;

        ulong steamId = user.PlatformId;
        AuctionCategory category = ParseCategory(categoryRaw);

        int pageSize = Math.Max(1, VAuctionConfigService.PageSize);
        List<AuctionListing> listings;

        lock (_lock)
        {
            IEnumerable<AuctionListing> q = AuctionRepository.Active.Values.Where(v => v.Status == AuctionStatus.Active);
            if (category != AuctionCategory.All)
            {
                q = q.Where(v => v.Category == category);
            }

            listings = q.OrderBy(v => v.ExpiresUnix).ThenBy(v => v.AuctionKey).ToList();
        }

        int total = listings.Count;
        int pageCount = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        int clampedPage = Math.Clamp(page, 1, pageCount);

        AuctionListing[] slice = listings
            .Skip((clampedPage - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        var summaries = new EclipseAuctionPayloads.AuctionListingSummary[slice.Length];
        for (int i = 0; i < slice.Length; i++)
        {
            AuctionListing l = slice[i];
            int flags = 0;
            if (l.CurrentBid > 0) flags |= 1 << 0;
            if (l.SellerSteamId == steamId) flags |= 1 << 2;
            if (l.HighestBidderSteamId.HasValue && l.HighestBidderSteamId.Value == steamId) flags |= 1 << 3;

            summaries[i] = new EclipseAuctionPayloads.AuctionListingSummary(
                AuctionKey: l.AuctionKey,
                Category: l.Category.ToString(),
                ItemName: l.Item.DisplayName,
                PrefabGuid: l.Item.PrefabGuid,
                Quantity: l.Item.Quantity,
                CurrentBid: l.CurrentBid,
                BuyNow: l.BuyNow,
                CurrencyPrefabGuid: l.CurrencyPrefabGuid,
                ExpiresUnix: l.ExpiresUnix,
                Flags: flags);
        }

        string payload = EclipseAuctionPayloads.BuildAuctionPage(
            category: category.ToString(),
            page: clampedPage,
            pageCount: pageCount,
            total: total,
            serverUnix: NowUnix(),
            listings: summaries);

        Core.EclipseSyncService.TrySend(user, payload);
    }

    public void SendDetail(User user, string auctionKey)
    {
        if (Core.EclipseSyncService == null || !Core.EclipseSyncService.IsEnabled) return;

        AuctionListing listing;
        lock (_lock)
        {
            listing = FindActiveByKey(auctionKey);
        }

        if (listing == null) return;

        var detail = new EclipseAuctionPayloads.AuctionListingDetail(
            AuctionKey: listing.AuctionKey,
            SellerName: listing.SellerName,
            ItemName: listing.Item.DisplayName,
            PrefabGuid: listing.Item.PrefabGuid,
            Quantity: listing.Item.Quantity,
            StartingBid: listing.StartingBid,
            CurrentBid: listing.CurrentBid,
            BuyNow: listing.BuyNow,
            CurrencyPrefabGuid: listing.CurrencyPrefabGuid,
            ExpiresUnix: listing.ExpiresUnix,
            Status: listing.Status.ToString().ToLowerInvariant(),
            MinNextBid: CalculateMinNextBid(listing));

        string payload = EclipseAuctionPayloads.BuildAuctionDetail(detail);
        Core.EclipseSyncService.TrySend(user, payload);
    }

    static AuctionCategory ParseCategory(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return AuctionCategory.All;
        string s = raw.Trim().ToLowerInvariant();
        return s switch
        {
            "all" => AuctionCategory.All,
            "weapons" or "weapon" => AuctionCategory.Weapons,
            "armor" or "armour" => AuctionCategory.Armor,
            "resources" or "resource" => AuctionCategory.Resources,
            _ => AuctionCategory.All
        };
    }
}

