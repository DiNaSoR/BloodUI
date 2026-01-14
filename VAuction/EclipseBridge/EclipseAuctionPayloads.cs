using System;
using System.Globalization;
using System.Text;

namespace VAuction.EclipseBridge;

/// <summary>
/// Defines the VAuction ↔ EclipsePlus wire protocol payload formats and builders.
/// Keep payloads small (server uses FixedString512Bytes) and avoid commas/pipes in strings.
/// </summary>
internal static class EclipseAuctionPayloads
{
    // NOTE: Must match EclipsePlus client enum ordering in Patches/ClientChatSystemPatch.cs
    public enum NetworkEventSubType
    {
        // 0..7 reserved by Eclipse/Bloodcraft
        AuctionPageToClient = 8,
        AuctionDetailToClient = 9,
        AuctionMyListingsToClient = 10,
        AuctionMyBidsToClient = 11,
        AuctionEscrowToClient = 12,
        AuctionNotificationToClient = 13
    }

    public const string SchemaVersion = "v1";

    public static string BuildAuctionPage(
        string category,
        int page,
        int pageCount,
        int total,
        long serverUnix,
        ReadOnlySpan<AuctionListingSummary> listings)
    {
        var sb = new StringBuilder(256);
        sb.Append('[').Append((int)NetworkEventSubType.AuctionPageToClient).Append("]:");
        sb.Append(SchemaVersion).Append(',')
            .Append(SanitizeToken(category)).Append(',')
            .Append(page.ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(pageCount.ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(total.ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(serverUnix.ToString(CultureInfo.InvariantCulture));

        for (int i = 0; i < listings.Length; i++)
        {
            AuctionListingSummary l = listings[i];
            sb.Append('|')
              .Append(SanitizeToken(l.AuctionKey)).Append(',')
              .Append(SanitizeToken(l.Category)).Append(',')
              .Append(SanitizeToken(l.ItemName)).Append(',')
              .Append(l.PrefabGuid.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(l.Quantity.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(l.CurrentBid.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(l.BuyNow.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(l.CurrencyPrefabGuid.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(l.ExpiresUnix.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(l.Flags.ToString(CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    public static string BuildAuctionDetail(AuctionListingDetail d)
    {
        var sb = new StringBuilder(256);
        sb.Append('[').Append((int)NetworkEventSubType.AuctionDetailToClient).Append("]:");
        sb.Append(SchemaVersion).Append(',')
          .Append(SanitizeToken(d.AuctionKey)).Append(',')
          .Append(SanitizeToken(d.SellerName)).Append(',')
          .Append(SanitizeToken(d.ItemName)).Append(',')
          .Append(d.PrefabGuid.ToString(CultureInfo.InvariantCulture)).Append(',')
          .Append(d.Quantity.ToString(CultureInfo.InvariantCulture)).Append(',')
          .Append(d.StartingBid.ToString(CultureInfo.InvariantCulture)).Append(',')
          .Append(d.CurrentBid.ToString(CultureInfo.InvariantCulture)).Append(',')
          .Append(d.BuyNow.ToString(CultureInfo.InvariantCulture)).Append(',')
          .Append(d.CurrencyPrefabGuid.ToString(CultureInfo.InvariantCulture)).Append(',')
          .Append(d.ExpiresUnix.ToString(CultureInfo.InvariantCulture)).Append(',')
          .Append(SanitizeToken(d.Status)).Append(',')
          .Append(d.MinNextBid.ToString(CultureInfo.InvariantCulture));

        return sb.ToString();
    }

    public static string BuildNotification(string kind, string auctionKey, int amount, int currencyPrefabGuid, string text)
    {
        var sb = new StringBuilder(256);
        sb.Append('[').Append((int)NetworkEventSubType.AuctionNotificationToClient).Append("]:");
        sb.Append(SchemaVersion).Append(',')
          .Append(SanitizeToken(kind)).Append(',')
          .Append(SanitizeToken(auctionKey)).Append(',')
          .Append(amount.ToString(CultureInfo.InvariantCulture)).Append(',')
          .Append(currencyPrefabGuid.ToString(CultureInfo.InvariantCulture)).Append(',')
          .Append(SanitizeToken(text));

        return sb.ToString();
    }

    /// <summary>
    /// Removes delimiters that would break parsing (',' and '|') and trims to keep payload small.
    /// </summary>
    public static string SanitizeToken(string input, int maxLen = 32)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        string s = input.Trim();
        if (s.Length > maxLen) s = s.Substring(0, maxLen);

        // Replace delimiters with spaces.
        s = s.Replace(',', ' ').Replace('|', ' ');
        return s;
    }

    internal readonly record struct AuctionListingSummary(
        string AuctionKey,
        string Category,
        string ItemName,
        int PrefabGuid,
        int Quantity,
        int CurrentBid,
        int BuyNow,
        int CurrencyPrefabGuid,
        long ExpiresUnix,
        int Flags);

    internal readonly record struct AuctionListingDetail(
        string AuctionKey,
        string SellerName,
        string ItemName,
        int PrefabGuid,
        int Quantity,
        int StartingBid,
        int CurrentBid,
        int BuyNow,
        int CurrencyPrefabGuid,
        long ExpiresUnix,
        string Status,
        int MinNextBid);
}

