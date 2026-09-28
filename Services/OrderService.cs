using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly ICartService _cart;

    public OrderService(AppDbContext db, ICartService cart)
    {
        _db = db;
        _cart = cart;
    }

    public async Task<string> GenerateOrderNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.Orders.CountAsync(o => o.PlacedAt.Year == year);
        return $"VW-{year}-{(count + 1):D4}";
    }

    public async Task<Order> CreateOrderFromCartAsync(int userId, int addressId, decimal shipping)
    {
        var cart = await _cart.GetCartAsync(userId)
            ?? throw new InvalidOperationException("Cart not found.");

        if (cart.Items == null || !cart.Items.Any())
            throw new InvalidOperationException("Cart is empty.");

        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == userId)
            ?? throw new InvalidOperationException("Address not found.");

        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        var savings = cart.Items.Sum(i => (i.OriginalMRP - i.UnitPrice) * i.Quantity);

        var order = new Order
        {
            OrderNumber = await GenerateOrderNumberAsync(),
            UserId = userId,
            Status = OrderStatus.Pending,
            Subtotal = subtotal,
            Discount = savings,
            Shipping = shipping,
            Total = subtotal + shipping,
            PaymentMethod = "Razorpay",
            PaymentStatus = "Pending",

            ShippingFullName = address.FullName,
            ShippingPhone = address.Phone,
            ShippingLine1 = address.Line1,
            ShippingLine2 = address.Line2,
            ShippingCity = address.City,
            ShippingState = address.State,
            ShippingPincode = address.Pincode,
            ShippingCountry = address.Country,

            PlacedAt = DateTime.UtcNow
        };

        foreach (var ci in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product?.Name ?? "",
                ProductImage = ci.Product?.Image1,
                Size = ci.Size,
                Color = ci.Color,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice,
                OriginalMRP = ci.OriginalMRP,
                LineTotal = ci.UnitPrice * ci.Quantity
            });
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return order;
    }

    public async Task<Order?> GetOrderAsync(int orderId, int userId)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);
    }

    public async Task<List<Order>> GetUserOrdersAsync(int userId)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync();
    }
}