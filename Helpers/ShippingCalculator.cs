namespace ValousWorld.Web.Helpers;

public static class ShippingCalculator
{
    public const decimal FreeShippingThreshold = 999m;
    public const decimal StandardShippingFee = 79m;

    public static decimal Calculate(decimal subtotal)
    {
        if (subtotal <= 0) return 0m;
        return subtotal >= FreeShippingThreshold ? 0m : StandardShippingFee;
    }

    public static decimal AmountForFreeShipping(decimal subtotal)
    {
        var remaining = FreeShippingThreshold - subtotal;
        return remaining > 0 ? remaining : 0m;
    }
}