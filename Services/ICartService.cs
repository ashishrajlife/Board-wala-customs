using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public interface ICartService
{
    Task<Cart> GetOrCreateCartAsync(int userId);
    Task<Cart?> GetCartAsync(int userId);
    Task<int> GetItemCountAsync(int userId);
    Task<(bool success, string? error)> AddItemAsync(int userId, int productId, string? size, string? color, int quantity);
    Task<(bool success, string? error)> UpdateQuantityAsync(int userId, int cartItemId, int quantity);
    Task<bool> RemoveItemAsync(int userId, int cartItemId);
    Task<bool> ClearCartAsync(int userId);
    Task<decimal> GetSubtotalAsync(int userId);
    Task<decimal> GetTotalSavingsAsync(int userId);
}