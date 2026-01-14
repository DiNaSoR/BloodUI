using ProjectM.Network;
using Stunlock.Core;
using VAuction.Services;
using VampireCommandFramework;

namespace VAuction.Commands;

[CommandGroup(name: "auction", "auc")]
internal static class AuctionCommands
{
    [Command(name: "browse", adminOnly: false, usage: ".auction browse [category] [page]", description: "Browse auction listings.")]
    public static void Browse(ChatCommandContext ctx, string category = "all", int page = 1)
    {
        User user = ctx.Event.User;
        Core.AuctionService.SendBrowsePage(user, category, page);
        ctx.Reply($"Opening VAuction listings: {category} (page {page}).");
    }

    [Command(name: "view", adminOnly: false, usage: ".auction view <auctionKey>", description: "View auction details.")]
    public static void View(ChatCommandContext ctx, string auctionKey)
    {
        User user = ctx.Event.User;
        Core.AuctionService.SendDetail(user, auctionKey);
        ctx.Reply($"Viewing auction {auctionKey}.");
    }

    [Command(name: "sellitem", adminOnly: false, usage: ".auction sellitem <prefabGuid> <qty> <startBid> <buyNow> <hours>", description: "List an inventory item by prefab guid.")]
    public static void SellItem(ChatCommandContext ctx, int prefabGuid, int quantity, int startBid, int buyNow, int hours)
    {
        var user = ctx.Event.User;
        var character = ctx.Event.SenderCharacterEntity;

        var itemPrefab = new PrefabGUID(prefabGuid);
        if (!itemPrefab.HasValue())
        {
            ctx.Reply("Invalid prefab guid.");
            return;
        }

        bool ok = Core.AuctionService.TryCreateListing(user, character, itemPrefab, quantity, startBid, buyNow, hours, out string error, out var created);
        if (!ok)
        {
            ctx.Reply(error);
            return;
        }

        Core.AuctionService.SendBrowsePage(user, "all", 1);
        ctx.Reply($"Listed {created.Item.DisplayName} x{created.Item.Quantity} as auction {created.AuctionKey}.");
    }

    [Command(name: "bid", adminOnly: false, usage: ".auction bid <auctionKey> <amount>", description: "Place a bid.")]
    public static void Bid(ChatCommandContext ctx, string auctionKey, int amount)
    {
        var user = ctx.Event.User;
        var character = ctx.Event.SenderCharacterEntity;

        bool ok = Core.AuctionService.TryPlaceBid(user, character, auctionKey, amount, out string error);
        if (!ok)
        {
            ctx.Reply(error);
            return;
        }

        Core.AuctionService.SendDetail(user, auctionKey);
        ctx.Reply($"Bid placed: {amount} on {auctionKey}.");
    }

    [Command(name: "buy", adminOnly: false, usage: ".auction buy <auctionKey>", description: "Buy now.")]
    public static void Buy(ChatCommandContext ctx, string auctionKey)
    {
        var user = ctx.Event.User;
        var character = ctx.Event.SenderCharacterEntity;

        bool ok = Core.AuctionService.TryBuyNow(user, character, auctionKey, out string error);
        if (!ok)
        {
            ctx.Reply(error);
            return;
        }

        int escrowCount = EscrowService.GetEscrowCount(user.PlatformId);
        ctx.Reply($"Purchased {auctionKey}. Items ready in escrow. Use .auction claim (escrow entries: {escrowCount}).");
        Core.AuctionService.SendBrowsePage(user, "all", 1);
    }

    [Command(name: "cancel", adminOnly: false, usage: ".auction cancel <auctionKey>", description: "Cancel an auction (only if no bids).")]
    public static void Cancel(ChatCommandContext ctx, string auctionKey)
    {
        var user = ctx.Event.User;
        var character = ctx.Event.SenderCharacterEntity;

        bool ok = Core.AuctionService.TryCancel(user, character, auctionKey, out string error);
        if (!ok)
        {
            ctx.Reply(error);
            return;
        }

        ctx.Reply($"Cancelled auction {auctionKey}. Items/refunds moved to escrow. Use .auction claim.");
        Core.AuctionService.SendBrowsePage(user, "all", 1);
    }

    [Command(name: "claim", adminOnly: false, usage: ".auction claim", description: "Claim all escrow items.")]
    public static void Claim(ChatCommandContext ctx)
    {
        var user = ctx.Event.User;
        var character = ctx.Event.SenderCharacterEntity;

        int claimed = EscrowService.ClaimAll(user, character);
        int remaining = EscrowService.GetEscrowCount(user.PlatformId);
        ctx.Reply($"Claimed {claimed} escrow entries. Remaining: {remaining}.");
    }
}

