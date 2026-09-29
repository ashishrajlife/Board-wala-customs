namespace ValousWorld.Web.Services;

public class OrderExpiryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OrderExpiryBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public OrderExpiryBackgroundService(
        IServiceProvider services,
        ILogger<OrderExpiryBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[EXPIRY] Background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();
                var count = await orders.ExpireAbandonedOrdersAsync();

                if (count > 0)
                    _logger.LogInformation("[EXPIRY] Expired {Count} orders", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EXPIRY] Error during expiry sweep");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}