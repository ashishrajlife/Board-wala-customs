using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public class OrderService : IOrderService
{
    private const int OrderExpiryMinutes = 30;

    private readonly AppDbContext _db;
    private readonly ICartService _cart;
    private readonly IVoucherService _vouchers;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext db, ICartService cart, IVoucherService vouchers, ILogger<OrderService> logger)
    {
        _db = db;
        _cart = cart;
        _vouchers = vouchers;
        _logger = logger;
    }

    public async Task<string> GenerateOrderNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.Orders.CountAsync(o => o.PlacedAt.Year == year);
        return $"VW-{year}-{(count + 1):D4}";
    }

    public async Task<Order> CreateOrderFromCartAsync(int userId, int addressId, decimal shipping, string? voucherCode = null)
    {
        var cart = await _cart.GetCartAsync(userId)
            ?? throw new InvalidOperationException("Cart not found.");

        if (cart.Items == null || !cart.Items.Any())
            throw new InvalidOperationException("Cart is empty.");

        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == userId)
            ?? throw new InvalidOperationException("Address not found.");

        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        var savings = cart.Items.Sum(i => (i.OriginalMRP - i.UnitPrice) * i.Quantity);
        var voucherDiscount = 0m;
        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            var voucher = await _vouchers.ValidateAsync(userId, voucherCode);
            if (!voucher.IsValid)
                throw new InvalidOperationException(voucher.Message);

            voucherDiscount = voucher.DiscountAmount;
        }

        var order = new Order
        {
            OrderNumber = await GenerateOrderNumberAsync(),
            UserId = userId,
            Status = OrderStatus.Created,
            Subtotal = subtotal,
            Discount = savings + voucherDiscount,
            Shipping = shipping,
            Total = subtotal + shipping - voucherDiscount,
            PaymentMethod = "Razorpay",
            PaymentStatus = PaymentStatus.Created,

            ShippingFullName = address.FullName,
            ShippingPhone = address.Phone,
            ShippingLine1 = address.Line1,
            ShippingLine2 = address.Line2,
            ShippingCity = address.City,
            ShippingState = address.State,
            ShippingPincode = address.Pincode,
            ShippingCountry = address.Country,

            PlacedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OrderExpiryMinutes)
        };

        foreach (var ci in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product?.Name ?? "",
                ProductImage = ci.Product?.Image1,
                Size = ci.Size,
                Color = ci.Color,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice,
                OriginalMRP = ci.OriginalMRP,
                LineTotal = ci.UnitPrice * ci.Quantity
            });
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        _logger.LogInformation("[ORDER] Created {OrderNumber} for user {UserId} | Total ₹{Total}",
            order.OrderNumber, userId, order.Total);

        return order;
    }

    public async Task<Order?> GetOrderAsync(int orderId, int userId)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.PaymentAttemptLog)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);
    }

    public async Task<Order?> GetOrderByRazorpayIdAsync(string razorpayOrderId)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.RazorpayOrderId == razorpayOrderId);
    }

    public async Task<List<Order>> GetUserOrdersAsync(int userId)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync();
    }

    // ============================================================
    // RETRY
    // ============================================================
    public async Task<Order?> GetOrderForRetryAsync(int orderId, int userId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

        if (order == null) return null;

        // Only allow retry for created/failed/expired orders
        var retryable = order.PaymentStatus == PaymentStatus.Created
                     || order.PaymentStatus == PaymentStatus.Pending
                     || order.PaymentStatus == PaymentStatus.Failed
                     || order.Status == OrderStatus.Expired;

        return retryable ? order : null;
    }

    // ============================================================
    // PAYMENT STATE MACHINE
    // ============================================================
    public async Task MarkPaymentFailedAsync(Order order, string? errorCode, string? errorDesc, string? reason = null)
    {
        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            _logger.LogWarning("[ORDER] Skipping MarkFailed — order {OrderNumber} already paid",
                order.OrderNumber);
            return;
        }

        order.PaymentStatus = PaymentStatus.Failed;
        order.Status = OrderStatus.Failed;
        order.FailureReason = $"[{errorCode}] {errorDesc}".Trim();
        order.LastPaymentAttemptAt = DateTime.UtcNow;
        order.PaymentAttempts += 1;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogWarning("[ORDER] {OrderNumber} marked FAILED: {Reason}",
            order.OrderNumber, order.FailureReason);
    }

    public async Task MarkPaymentPaidAsync(Order order, string razorpayPaymentId, string signature)
    {
        // Idempotency — already paid? Skip
        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            _logger.LogInformation("[ORDER] {OrderNumber} already paid — idempotent skip",
                order.OrderNumber);
            return;
        }

        order.PaymentStatus = PaymentStatus.Paid;
        order.RazorpayPaymentId = razorpayPaymentId;
        order.RazorpaySignature = signature;
        order.Status = OrderStatus.Confirmed;
        order.PaidAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("[ORDER] ✅ {OrderNumber} marked PAID (payment {PaymentId})",
            order.OrderNumber, razorpayPaymentId);
    }

    public async Task LogPaymentAttemptAsync(
        Order order, string status,
        string? errorCode = null, string? errorDesc = null,
        string? reason = null, string? step = null)
    {
        _db.PaymentAttempts.Add(new PaymentAttempt
        {
            OrderId = order.OrderId,
            RazorpayOrderId = order.RazorpayOrderId,
            RazorpayPaymentId = order.RazorpayPaymentId,
            Status = status,
            ErrorCode = errorCode,
            ErrorDescription = errorDesc,
            ErrorReason = reason,
            ErrorStep = step,
            Amount = order.Total,
            AttemptedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    // ============================================================
    // EXPIRY JOB (call from background service or manual action)
    // ============================================================
    public async Task<int> ExpireAbandonedOrdersAsync()
    {
        var now = DateTime.UtcNow;

        var expired = await _db.Orders
            .Where(o => o.PaymentStatus != PaymentStatus.Paid
                     && o.PaymentStatus != PaymentStatus.Refunded
                     && o.Status != OrderStatus.Cancelled
                     && o.ExpiresAt != null
                     && o.ExpiresAt < now)
            .ToListAsync();

        foreach (var o in expired)
        {
            o.Status = OrderStatus.Expired;
            o.PaymentStatus = PaymentStatus.Failed;
            o.FailureReason = "Payment not completed within time limit.";
            o.UpdatedAt = now;
        }

        if (expired.Any())
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("[ORDER] Expired {Count} abandoned orders", expired.Count);
        }

        return expired.Count;
    }
}