using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public class DelhiveryTrackingSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<DelhiveryTrackingSyncService> _logger;

    private static readonly TimeSpan Interval   = TimeSpan.FromMinutes(20);
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(30);

    public DelhiveryTrackingSyncService(IServiceScopeFactory scopes, ILogger<DelhiveryTrackingSyncService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(90), ct);

        while (!ct.IsCancellationRequested)
        {
            try { await RunOnceAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "[DELHIVERY-SYNC] Run failed"); }

            await Task.Delay(Interval, ct);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var delivery = scope.ServiceProvider.GetRequiredService<IDeliveryService>();
        var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var cutoff = DateTime.UtcNow - StaleAfter;

        var candidates = await db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Shipped
                     && o.DelhiveryWaybill != null
                     && (o.LastTrackingSyncAt == null || o.LastTrackingSyncAt < cutoff))
            .OrderBy(o => o.LastTrackingSyncAt ?? o.ShippedAt)
            .Take(30)
            .Select(o => new { o.DelhiveryWaybill })
            .ToListAsync(ct);

        if (candidates.Count == 0) return;

        _logger.LogInformation("[DELHIVERY-SYNC] Checking {Count} shipments", candidates.Count);

        foreach (var c in candidates)
        {
            var (ok, status, _) = await delivery.FetchStatusAsync(c.DelhiveryWaybill!);
            if (!ok || string.IsNullOrWhiteSpace(status)) continue;

            var (syncOk, err) = await orders.SyncShipmentStatusAsync(c.DelhiveryWaybill!, status);
            if (!syncOk) _logger.LogWarning("[DELHIVERY-SYNC] Sync failed for {AWB}: {Err}",
                c.DelhiveryWaybill, err);
        }
    }
}