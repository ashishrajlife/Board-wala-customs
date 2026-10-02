namespace ValousWorld.Web.Services;

public class VoucherService : IVoucherService
{
    private readonly ICartService _cart;

    public VoucherService(ICartService cart) => _cart = cart;

    public async Task<VoucherValidationResult> ValidateAsync(int userId, string? code)
    {
        var normalizedCode = code?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode))
            return Invalid("Enter a voucher code.");

        var cart = await _cart.GetCartAsync(userId);
        var eligibleItems = cart?.Items?
            .Where(item => item.Product != null
                && string.Equals(item.Product.VoucherCode?.Trim(), normalizedCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (eligibleItems == null || eligibleItems.Count == 0)
            return Invalid("This voucher does not apply to any product in your cart.");

        var now = DateTime.UtcNow;
        decimal totalDiscount = 0m;

        foreach (var itemsForProduct in eligibleItems.GroupBy(item => item.ProductId))
        {
            var product = itemsForProduct.First().Product!;
            var percent = product.VoucherDiscountPercent ?? 0m;
            if (!product.VoucherIsActive
                || (product.VoucherValidUntil.HasValue && product.VoucherValidUntil.Value <= now)
                || percent <= 0m
                || percent > 100m)
                continue;

            var eligibleSubtotal = itemsForProduct.Sum(item => item.UnitPrice * item.Quantity);
            if (eligibleSubtotal < (product.VoucherMinOrderValue ?? 0m))
                continue;

            var discount = eligibleSubtotal * percent / 100m;
            if (product.VoucherMaxDiscount is > 0)
                discount = Math.Min(discount, product.VoucherMaxDiscount.Value);

            totalDiscount += Math.Min(discount, eligibleSubtotal);
        }

        totalDiscount = decimal.Round(totalDiscount, 2, MidpointRounding.AwayFromZero);
        return totalDiscount > 0m
            ? new VoucherValidationResult(true, "Voucher applied.", totalDiscount)
            : Invalid("This voucher is inactive, expired, or its minimum order value has not been met.");
    }

    private static VoucherValidationResult Invalid(string message) =>
        new(false, message, 0m);
}