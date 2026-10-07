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
    private readonly IRazorpayService _razorpay;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        AppDbContext db,
        ICartService cart,
        IVoucherService vouchers,
        IRazorpayService razorpay,
        ILogger<OrderService> logger)
    {
        _db = db;
        _cart = cart;
        _vouchers = vouchers;
        _razorpay = razorpay;
        _logger = logger;
    }

    public async Task<string> GenerateOrderNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.Orders.CountAsync(o => o.PlacedAt.Year == year);
        return $"VW-{year}-{(count + 1):D4}";
    }

    public async Task<Order> CreateOrderFromCartAsync(
        int userId, int addressId, decimal shipping,
        string? voucherCode = null,
        string paymentMethod = PaymentMethods.Razorpay,
        decimal codFee = 0m)
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

        var isCod = paymentMethod == PaymentMethods.Cod;

        var order = new Order
        {
            OrderNumber = await GenerateOrderNumberAsync(),
            UserId = userId,
            Status = isCod ? OrderStatus.Confirmed : OrderStatus.Created,
            Subtotal = subtotal,
            Discount = savings + voucherDiscount,
            Shipping = shipping,
            CodFee = codFee,
            Total = subtotal + shipping + codFee - voucherDiscount,
            PaymentMethod = paymentMethod,
            PaymentStatus = isCod ? PaymentStatus.CodPending : PaymentStatus.Created,

            ShippingFullName = address.FullName,
            ShippingPhone = address.Phone,
            ShippingLine1 = address.Line1,
            ShippingLine2 = address.Line2,
            ShippingCity = address.City,
            ShippingState = address.State,
            ShippingPincode = address.Pincode,
            ShippingCountry = address.Country,

            PlacedAt = DateTime.UtcNow,
            ExpiresAt = isCod ? null : DateTime.UtcNow.AddMinutes(OrderExpiryMinutes)
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

        _logger.LogInformation("[ORDER] Created {OrderNumber} for user {UserId} | Method {Method} | Total ₹{Total}",
            order.OrderNumber, userId, order.PaymentMethod, order.Total);

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

        if (order.PaymentMethod == PaymentMethods.Cod) return null;

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
    // EXPIRY JOB
    // ============================================================
    public async Task<int> ExpireAbandonedOrdersAsync()
    {
        var now = DateTime.UtcNow;

        var expired = await _db.Orders
            .Where(o => o.PaymentMethod != PaymentMethods.Cod
                     && o.PaymentStatus != PaymentStatus.Paid
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

    // ============================================================
    // CANCEL ORDER
    // ============================================================
    public async Task<(bool success, string? error, bool refundInitiated)> CancelOrderAsync(
        int orderId, int userId, string reason, string cancelledBy)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

        if (order == null) return (false, "Order not found.", false);

        // Only cancellable before shipment
                // Block only the terminal states
        var blocked = order.Status == OrderStatus.Cancelled
                   || order.Status == OrderStatus.Delivered
                   || order.Status == OrderStatus.Refunded
                   || order.PaymentStatus == PaymentStatus.Refunded;

        if (blocked)
            return (false, $"This order cannot be cancelled (status: {order.Status}).", false);

        var refundInitiated = false;

        // Mark cancelled first — the truth wins even if refund API blows up
        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.CancelledBy = cancelledBy;
        order.CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Not specified" : reason;
        order.UpdatedAt = DateTime.UtcNow;

        // ---- Refund logic ----
        if (order.PaymentStatus == PaymentStatus.Paid &&
            !string.IsNullOrWhiteSpace(order.RazorpayPaymentId))
        {
            try
            {
                var (ok, refundId, error) = await _razorpay.CreateRefundAsync(
                    order.RazorpayPaymentId, order.Total, reason);

                if (ok)
                {
                    order.RefundId = refundId;
                    order.RefundedAmount = order.Total;
                    order.RefundedAt = DateTime.UtcNow;
                    order.RefundStatus = "Initiated";
                    order.PaymentStatus = PaymentStatus.Refunded;
                    refundInitiated = true;

                    _logger.LogInformation("[CANCEL] Refund initiated for {OrderNumber} — RefundId {RefundId}",
                        order.OrderNumber, refundId);
                }
                else
                {
                    order.RefundStatus = "Failed";
                    _logger.LogError("[CANCEL] Refund FAILED for {OrderNumber}: {Error}",
                        order.OrderNumber, error);
                }
            }
            catch (Exception ex)
            {
                order.RefundStatus = "Failed";
                _logger.LogError(ex, "[CANCEL] Refund exception for {OrderNumber}", order.OrderNumber);
            }
        }
        else if (order.PaymentStatus == PaymentStatus.CodPending)
        {
            order.PaymentStatus = PaymentStatus.Cancelled;
        }
        else if (order.PaymentStatus == PaymentStatus.Pending ||
                 order.PaymentStatus == PaymentStatus.Created ||
                 order.PaymentStatus == PaymentStatus.Failed)
        {
            order.PaymentStatus = PaymentStatus.Cancelled;
        }

        await _db.SaveChangesAsync();

        // Log the cancellation as a payment attempt for audit
        try
        {
            await LogPaymentAttemptAsync(order, "Cancelled",
                errorCode: "USER_CANCEL", errorDesc: reason,
                reason: reason, step: cancelledBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CANCEL] LogPaymentAttempt failed for {OrderNumber}", order.OrderNumber);
        }

        _logger.LogInformation("[CANCEL] Order {OrderNumber} cancelled by {By} | Reason: {Reason}",
            order.OrderNumber, cancelledBy, reason);

        return (true, null, refundInitiated);
    }

    // ============================================================
    // MARK COD COLLECTED (Delhivery webhook will call this later)
    // ============================================================
    public async Task MarkCodCollectedAsync(Order order)
    {
        if (order.PaymentStatus == PaymentStatus.Paid) return;

        order.PaymentStatus = PaymentStatus.Paid;
        order.CodCollectedAt = DateTime.UtcNow;
        order.PaidAt = DateTime.UtcNow;
        order.Status = OrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("[ORDER] COD collected for {OrderNumber} — ₹{Total}",
            order.OrderNumber, order.Total);
    }
}