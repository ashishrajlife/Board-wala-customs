using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public class CartService : ICartService
{
    private readonly AppDbContext _db;

    public CartService(AppDbContext db) => _db = db;

    public async Task<Cart> GetOrCreateCartAsync(int userId)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId, CreatedAt = DateTime.UtcNow };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }

        return cart;
    }

    public async Task<Cart?> GetCartAsync(int userId)
    {
        return await _db.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.Category)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<int> GetItemCountAsync(int userId)
    {
        return await _db.CartItems
            .Where(ci => ci.Cart!.UserId == userId)
            .SumAsync(ci => (int?)ci.Quantity) ?? 0;
    }

    public async Task<(bool success, string? error)> AddItemAsync(
        int userId, int productId, string? size, string? color, int quantity)
    {
        if (quantity < 1) quantity = 1;
        if (quantity > 10) quantity = 10;

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.IsActive);

        if (product == null)
            return (false, "Product not available.");

        if (product.Stock <= 0)
            return (false, "Out of stock.");

        var cart = await GetOrCreateCartAsync(userId);

        // Determine unit price — sale price if valid, else MRP
        var unitPrice = (product.SalePrice.HasValue
                        && product.SalePrice.Value > 0
                        && product.SalePrice.Value < product.MRP)
                            ? product.SalePrice.Value
                            : product.MRP;

        // Normalize size/color (treat empty as null)
        size = string.IsNullOrWhiteSpace(size) ? null : size.Trim();
        color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();

        // Merge if same product + size + color already exists
        var existing = cart.Items.FirstOrDefault(i =>
            i.ProductId == productId &&
            string.Equals(i.Size, size, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(i.Color, color, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.Quantity = Math.Min(existing.Quantity + quantity, 10);
        }
        else
        {
            _db.CartItems.Add(new CartItem
            {
                CartId = cart.CartId,
                ProductId = productId,
                Size = size,
                Color = color,
                Quantity = quantity,
                UnitPrice = unitPrice,
                OriginalMRP = product.MRP,
                AddedAt = DateTime.UtcNow
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool success, string? error)> UpdateQuantityAsync(
        int userId, int cartItemId, int quantity)
    {
        if (quantity < 1) quantity = 1;
        if (quantity > 10) quantity = 10;

        var item = await _db.CartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId
                                    && ci.Cart!.UserId == userId);

        if (item == null) return (false, "Item not found.");

        item.Quantity = quantity;

        if (item.Cart != null)
            item.Cart.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> RemoveItemAsync(int userId, int cartItemId)
    {
        var item = await _db.CartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId
                                    && ci.Cart!.UserId == userId);

        if (item == null) return false;

        _db.CartItems.Remove(item);

        if (item.Cart != null)
            item.Cart.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ClearCartAsync(int userId)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null) return false;

        _db.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> GetSubtotalAsync(int userId)
    {
        return await _db.CartItems
            .Where(ci => ci.Cart!.UserId == userId)
            .SumAsync(ci => (decimal?)ci.Quantity * ci.UnitPrice) ?? 0m;
    }

    public async Task<decimal> GetTotalSavingsAsync(int userId)
    {
        return await _db.CartItems
            .Where(ci => ci.Cart!.UserId == userId)
            .SumAsync(ci => (decimal?)((ci.OriginalMRP - ci.UnitPrice) * ci.Quantity)) ?? 0m;
    }
}