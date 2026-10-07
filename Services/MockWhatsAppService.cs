namespace ValousWorld.Web.Services;

public class MockWhatsAppService : IWhatsAppService
{
    private readonly ILogger<MockWhatsAppService> _logger;

    public MockWhatsAppService(ILogger<MockWhatsAppService> logger) => _logger = logger;

    public Task SendOrderConfirmationAsync(Models.Entities.Order order)
    {
        var mode = order.PaymentMethod == "COD"
            ? $"COD — collect ₹{order.Total:0.00}"
            : $"Paid ₹{order.Total:0.00}";

        _logger.LogInformation(
            "[WHATSAPP MOCK] Order confirmation → {Phone} | Order: {OrderNumber} | {Mode}",
            order.ShippingPhone, order.OrderNumber, mode);
        return Task.CompletedTask;
    }

    public Task SendOrderStatusUpdateAsync(Models.Entities.Order order)
    {
        _logger.LogInformation(
            "[WHATSAPP MOCK] Status update → {Phone} | Order: {OrderNumber} | Status: {Status}",
            order.ShippingPhone, order.OrderNumber, order.Status);
        return Task.CompletedTask;
    }

    public Task SendOrderCancelledAsync(Models.Entities.Order order)
    {
        _logger.LogInformation(
            "[WHATSAPP MOCK] Order cancelled → {Phone} | Order: {OrderNumber} | Reason: {Reason} | Refund: {Refund}",
            order.ShippingPhone, order.OrderNumber, order.CancellationReason,
            order.RefundId ?? "none");
        return Task.CompletedTask;
    }
}