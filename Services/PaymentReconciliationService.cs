using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public class PaymentReconciliationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<PaymentReconciliationService> _logger;

    public PaymentReconciliationService(IServiceScopeFactory scopes, ILogger<PaymentReconciliationService> logger)
    {
        _scopes = scopes; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
        while (!ct.IsCancellationRequested)
        {
            try { await RunOnce(ct); }
            catch (Exception ex) { _logger.LogError(ex, "[RECONCILE] Run failed"); }
            await Task.Delay(TimeSpan.FromMinutes(3), ct);
        }
    }

    private async Task RunOnce(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var razorpay = scope.ServiceProvider.GetRequiredService<IRazorpayService>();
        var finalizer = scope.ServiceProvider.GetRequiredService<IPaymentFinalizer>();

        var now = DateTime.UtcNow;
        var olderThan = now.AddMinutes(-5);
        var newerThan = now.AddHours(-3);

        var candidates = await db.Orders.AsNoTracking()
            .Where(o => o.PaymentStatus != PaymentStatus.Paid
                     && o.RazorpayOrderId != null
                     && o.LastPaymentAttemptAt != null
                     && o.LastPaymentAttemptAt < olderThan
                     && o.LastPaymentAttemptAt > newerThan
                     && o.Status != OrderStatus.Cancelled
                     && o.Status != OrderStatus.Refunded)
            .OrderBy(o => o.LastPaymentAttemptAt)
            .Select(o => new { o.OrderId, o.RazorpayOrderId })
            .Take(50)
            .ToListAsync(ct);

        foreach (var c in candidates)
        {
            var (ok, paymentId, amount, error) = await razorpay.FetchCapturedPaymentForOrderAsync(c.RazorpayOrderId!);
            if (!ok) { _logger.LogWarning("[RECONCILE] Fetch failed for {Id}: {Err}", c.OrderId, error); continue; }
            if (paymentId == null) continue;

            var result = await finalizer.FinalizeAsync(c.OrderId, paymentId, "", amount, "reconcile");
            _logger.LogInformation("[RECONCILE] Order {Id} → {Result}", c.OrderId, result);
        }
    }
}