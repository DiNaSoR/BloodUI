namespace VAuction.Domain;

internal sealed class AuctionBid
{
    public ulong BidderSteamId { get; set; }
    public string BidderName { get; set; } = string.Empty;
    public int Amount { get; set; }
    public long Unix { get; set; }
}

