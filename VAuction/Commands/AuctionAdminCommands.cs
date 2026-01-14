using VampireCommandFramework;

namespace VAuction.Commands;

[CommandGroup(name: "auction.admin", "aucadmin")]
internal static class AuctionAdminCommands
{
    [Command(name: "stats", adminOnly: true, usage: ".aucadmin stats", description: "Show basic VAuction stats.")]
    public static void Stats(ChatCommandContext ctx)
    {
        int active = VAuction.Persistence.AuctionRepository.Active.Count;
        int history = VAuction.Persistence.AuctionRepository.History.Count;
        int escrowUsers = VAuction.Persistence.EscrowRepository.EscrowBySteamId.Count;
        ctx.Reply($"VAuction stats: active={active}, history={history}, escrowUsers={escrowUsers}");
    }
}

