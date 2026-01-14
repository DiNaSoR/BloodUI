namespace VAuction.Domain;

internal sealed class AuctionItem
{
    public int PrefabGuid { get; set; }
    public int Quantity { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

