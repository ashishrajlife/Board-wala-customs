using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public interface IOrderService
{
    Task<Order> CreateOrderFromCartAsync(int userId, int addressId, decimal shipping);
    Task<Order?> GetOrderAsync(int orderId, int userId);
    Task<List<Order>> GetUserOrdersAsync(int userId);
    Task<string> GenerateOrderNumberAsync();
}