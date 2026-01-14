namespace VAuction.Services;

internal static class PricingService
{
    public static int ClampPrice(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static int CalculatePercentFee(int amount, int percent)
    {
        if (amount <= 0 || percent <= 0) return 0;
        // Integer math with rounding down; consistent and safe.
        return (amount * percent) / 100;
    }
}

