using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public interface IOrderService
{
    Task<Order> CreateOrderFromCartAsync(int userId, int addressId, decimal shipping);
    Task<Order?> GetOrderAsync(int orderId, int userId);
    Task<Order?> GetOrderByRazorpayIdAsync(string razorpayOrderId);
    Task<List<Order>> GetUserOrdersAsync(int userId);
    Task<string> GenerateOrderNumberAsync();

    // -------- Production-grade methods --------
    Task<Order?> GetOrderForRetryAsync(int orderId, int userId);
    Task MarkPaymentFailedAsync(Order order, string? errorCode, string? errorDesc, string? reason = null);
    Task MarkPaymentPaidAsync(Order order, string razorpayPaymentId, string signature);
    Task LogPaymentAttemptAsync(Order order, string status, string? errorCode = null, string? errorDesc = null, string? reason = null, string? step = null);
    Task<int> ExpireAbandonedOrdersAsync();
}