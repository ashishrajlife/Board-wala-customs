using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public interface IRazorpayService
{
    Task<(bool success, string? razorpayOrderId, string? error)> CreateOrderAsync(Order order);

    Task<(bool success, string? paymentId, string? error)> VerifyPaymentAsync(
        string razorpayOrderId, string razorpayPaymentId, string signature);

    Task<(bool success, string? status, decimal? amount, string? error)> FetchPaymentStatusAsync(
        string razorpayPaymentId);

    Task<(bool success, string? refundId, string? error)> CreateRefundAsync(
        string razorpayPaymentId, decimal amount, string? notes = null);
}