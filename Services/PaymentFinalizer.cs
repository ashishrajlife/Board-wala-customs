using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;

namespace ValousWorld.Web.Services;

public enum FinalizeResult { Finalized, AlreadyPaid, AmountMismatch, NotFound }

public interface IPaymentFinalizer
{
    Task<FinalizeResult> FinalizeAsync(int orderId, string razorpayPaymentId,
        string signature, decimal? paidAmount, string source);
}

public class PaymentFinalizer : IPaymentFinalizer
{
    private readonly AppDbContext _db;
    private readonly IOrderService _orders;
    private readonly IDeliveryService _delivery;
    private readonly IWhatsAppService _whatsapp;
    private readonly ICartService _cart;
    private readonly ILogger<PaymentFinalizer> _logger;

    public PaymentFinalizer(AppDbContext db, IOrderService orders, IDeliveryService delivery,
        IWhatsAppService whatsapp, ICartService cart, ILogger<PaymentFinalizer> logger)
    {
        _db = db; _orders = orders; _delivery = delivery;
        _whatsapp = whatsapp; _cart = cart; _logger = logger;
    }

    public async Task<FinalizeResult> FinalizeAsync(int orderId, string razorpayPaymentId,
        string signature, decimal? paidAmount, string source)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        // Per-order lock: a second caller waits here until the first one commits
        var lockResult = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        await _db.Database.ExecuteSqlRawAsync(
            "EXEC @result = sp_getapplock @Resource = @res, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000",
            lockResult, new SqlParameter("@res", $"order-pay-{orderId}"));
        if ((int)lockResult.Value < 0)
            throw new InvalidOperationException($"Could not lock order {orderId}");

        // Projection hits the DB, so this is always fresh
        var fresh = await _db.Orders.AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => new { o.PaymentStatus, o.Status, o.Total })
            .FirstOrDefaultAsync();

        if (fresh == null) return FinalizeResult.NotFound;

        if (fresh.PaymentStatus == Models.Entities.PaymentStatus.Paid)
        {
            await tx.CommitAsync();
            return FinalizeResult.AlreadyPaid;
        }

        if (paidAmount.HasValue && Math.Abs(fresh.Total - paidAmount.Value) > 0.01m)
        {
            _logger.LogError("[FINALIZE] AMOUNT MISMATCH order {OrderId}: expected {Exp}, got {Got}, payment {PayId}",
                orderId, fresh.Total, paidAmount, razorpayPaymentId);
            await tx.CommitAsync();
            return FinalizeResult.AmountMismatch;
        }

        if (fresh.Status == Models.Entities.OrderStatus.Expired ||
            fresh.Status == Models.Entities.OrderStatus.Cancelled)
        {
            _logger.LogWarning("[FINALIZE] Payment {PayId} arrived for {Status} order {OrderId}. Review stock/refund.",
                razorpayPaymentId, fresh.Status, orderId);
        }

        var order = await _db.Orders.Include(o => o.Items).FirstAsync(o => o.OrderId == orderId);

        await _orders.MarkPaymentPaidAsync(order, razorpayPaymentId, signature);
        await _orders.LogPaymentAttemptAsync(order, "Success");
        await tx.CommitAsync();

        _logger.LogInformation("[FINALIZE] {OrderNumber} paid via {Source}", order.OrderNumber, source);

        // Side effects: only the winner reaches here. Failures here must not undo the payment.
        try
        {
            var (shipSuccess, courier, tracking, _) = await _delivery.CreateShipmentAsync(order);
            if (shipSuccess)
            {
                order.CourierName = courier;
                order.TrackingNumber = tracking;
                order.ShippedAt = DateTime.UtcNow;
                order.Status = Models.Entities.OrderStatus.Shipped;
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "[FINALIZE] Shipment failed for {OrderNumber}", order.OrderNumber); }

        try { await _cart.ClearCartAsync(order.UserId); }
        catch (Exception ex) { _logger.LogError(ex, "[FINALIZE] Clear cart failed for {OrderNumber}", order.OrderNumber); }

        try { await _whatsapp.SendOrderConfirmationAsync(order); }
        catch (Exception ex) { _logger.LogError(ex, "[FINALIZE] WhatsApp failed for {OrderNumber}", order.OrderNumber); }

        return FinalizeResult.Finalized;
    }
}