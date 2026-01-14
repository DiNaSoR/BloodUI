namespace VAuction.Domain;

internal sealed class EscrowEntry
{
    public string AuctionKey { get; set; } = string.Empty;
    public int PrefabGuid { get; set; }
    public int Quantity { get; set; }
}

