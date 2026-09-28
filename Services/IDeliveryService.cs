using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public interface IDeliveryService
{
    Task<(bool success, string? courier, string? tracking, string? error)> CreateShipmentAsync(Order order);
}