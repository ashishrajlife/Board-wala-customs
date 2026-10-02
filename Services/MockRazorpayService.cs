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
    // Ye 2 methods add karo MockRazorpayService me:

    public Task<(bool success, string? status, decimal? amount, string? error)> FetchPaymentStatusAsync(
        string razorpayPaymentId)
    {
        _logger.LogInformation("[RAZORPAY MOCK] Fetch status: {PaymentId}", razorpayPaymentId);
        return Task.FromResult<(bool, string?, decimal?, string?)>((true, "captured", null, null));
    }

    public Task<(bool success, string? refundId, string? error)> CreateRefundAsync(
        string razorpayPaymentId, decimal amount, string? notes = null)
    {
        var mockRefundId = $"mock_refund_{Guid.NewGuid():N}";
        _logger.LogInformation("[RAZORPAY MOCK] Refund: {PaymentId} → {RefundId}", razorpayPaymentId, mockRefundId);
        return Task.FromResult<(bool, string?, string?)>((true, mockRefundId, null));
    }
    public Task<(bool success, string? paymentId, decimal? amount, string? error)> FetchCapturedPaymentForOrderAsync(
    string razorpayOrderId)
    {
        return Task.FromResult<(bool, string?, decimal?, string?)>((true, null, null, null));
    }
}