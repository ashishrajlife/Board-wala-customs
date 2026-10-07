namespace ValousWorld.Web.Services;

public class MockDeliveryService : IDeliveryService
{
    private readonly ILogger<MockDeliveryService> _logger;

    public MockDeliveryService(ILogger<MockDeliveryService> logger) => _logger = logger;

    public Task<(bool success, string? courier, string? tracking, string? error)> CreateShipmentAsync(Models.Entities.Order order)
    {
        var tracking = $"MOCK{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(100000, 999999)}";
        _logger.LogInformation("[DELIVERY MOCK] Shipment created for {OrderNumber} → {Tracking}",
            order.OrderNumber, tracking);
        return Task.FromResult<(bool, string?, string?, string?)>((true, "MockExpress", tracking, null));
    }

    public Task<(bool success, string? error)> CancelShipmentAsync(string trackingNumber)
    {
        _logger.LogInformation("[DELIVERY MOCK] Shipment cancelled for {Tracking}", trackingNumber);
        return Task.FromResult<(bool, string?)>((true, null));
    }
}