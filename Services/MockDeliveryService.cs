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

    public Task<(bool success, bool serviceable, bool codAvailable, string? error)> CheckServiceabilityAsync(string pincode)
    {
        // Test conventions:
        //   Pincode ending in "0000"  → NOT serviceable (for negative-path testing)
        //   Pincode starting with "99" → serviceable but COD unavailable
        //   Everything else            → serviceable + COD available
        var notServiceable = pincode.EndsWith("0000");
        var codUnavailable = pincode.StartsWith("99");

        var serviceable = !notServiceable;
        var cod = serviceable && !codUnavailable;

        _logger.LogInformation("[DELIVERY MOCK] Serviceability {Pin} → svc={Svc}, cod={Cod}",
            pincode, serviceable, cod);

        return Task.FromResult<(bool, bool, bool, string?)>((true, serviceable, cod, null));
    }

    public Task<(bool success, string? status, string? error)> FetchStatusAsync(string waybill)
    {
        _logger.LogInformation("[DELIVERY MOCK] Fetch status for {Waybill}", waybill);
        return Task.FromResult<(bool, string?, string?)>((true, "In Transit", null));
    }
}