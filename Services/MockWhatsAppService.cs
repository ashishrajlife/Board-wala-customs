namespace ValousWorld.Web.Services;

public class MockWhatsAppService : IWhatsAppService
{
    private readonly ILogger<MockWhatsAppService> _logger;

    public MockWhatsAppService(ILogger<MockWhatsAppService> logger) => _logger = logger;

    public Task SendOrderConfirmationAsync(Models.Entities.Order order)
    {
        _logger.LogInformation(
            "[WHATSAPP MOCK] Order confirmation → {Phone} | Order: {OrderNumber} | Total: ₹{Total}",
            order.ShippingPhone, order.OrderNumber, order.Total);
        return Task.CompletedTask;
    }

    public Task SendOrderStatusUpdateAsync(Models.Entities.Order order)
    {
        _logger.LogInformation(
            "[WHATSAPP MOCK] Status update → {Phone} | Order: {OrderNumber} | Status: {Status}",
            order.ShippingPhone, order.OrderNumber, order.Status);
        return Task.CompletedTask;
    }
}