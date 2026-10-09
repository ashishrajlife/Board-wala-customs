using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public interface IDeliveryService
{
    Task<(bool success, string? courier, string? tracking, string? error)> CreateShipmentAsync(Order order);
    Task<(bool success, string? error)> CancelShipmentAsync(string trackingNumber);
    Task<(bool success, bool serviceable, bool codAvailable, string? error)> CheckServiceabilityAsync(string pincode);
    Task<(bool success, string? status, string? error)> FetchStatusAsync(string waybill);
}