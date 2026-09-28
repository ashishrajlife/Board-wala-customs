namespace ValousWorld.Web.Services;

public class MockRazorpayService : IRazorpayService
{
    private readonly ILogger<MockRazorpayService> _logger;

    public MockRazorpayService(ILogger<MockRazorpayService> logger) => _logger = logger;

    public Task<(bool success, string? razorpayOrderId, string? error)> CreateOrderAsync(Models.Entities.Order order)
    {
        var mockId = $"mock_order_{Guid.NewGuid():N}";
        _logger.LogInformation("[RAZORPAY MOCK] Create order {OrderNumber} → {MockId}", order.OrderNumber, mockId);
        return Task.FromResult<(bool, string?, string?)>((true, mockId, null));
    }

    public Task<(bool success, string? paymentId, string? error)> VerifyPaymentAsync(
        string razorpayOrderId, string razorpayPaymentId, string signature)
    {
        var mockPaymentId = $"mock_pay_{Guid.NewGuid():N}";
        _logger.LogInformation("[RAZORPAY MOCK] Verify payment for {OrderId} → {PaymentId}",
            razorpayOrderId, mockPaymentId);
        return Task.FromResult<(bool, string?, string?)>((true, mockPaymentId, null));
    }
}