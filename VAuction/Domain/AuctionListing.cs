using System.Collections.Generic;

namespace VAuction.Domain;

internal sealed class AuctionListing
{
    public ulong Id { get; set; }
    public string AuctionKey { get; set; } = string.Empty;

    public ulong SellerSteamId { get; set; }
    public string SellerName { get; set; } = string.Empty;

    public AuctionCategory Category { get; set; }
    public AuctionItem Item { get; set; } = new();

    public long CreatedUnix { get; set; }
    public long ExpiresUnix { get; set; }
    public AuctionStatus Status { get; set; } = AuctionStatus.Active;

    public int StartingBid { get; set; }
    public int CurrentBid { get; set; }
    public int BuyNow { get; set; }
    public int CurrencyPrefabGuid { get; set; }

    public ulong? HighestBidderSteamId { get; set; }
    public string HighestBidderName { get; set; }

    public List<AuctionBid> BidHistory { get; set; } = [];
}

