using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public interface IRazorpayService
{
    Task<(bool success, string? razorpayOrderId, string? error)> CreateOrderAsync(Order order);
    Task<(bool success, string? paymentId, string? error)> VerifyPaymentAsync(
        string razorpayOrderId, string razorpayPaymentId, string signature);
}