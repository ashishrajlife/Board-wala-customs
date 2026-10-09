using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

/// <summary>
/// Maps Delhivery's raw status strings → our internal OrderStatus constants.
/// Permissive: unknown statuses fall through to Shipped so nothing gets stuck.
/// </summary>
public static class DelhiveryStatusMapper
{
    public static string Map(string? delhiveryStatus)
    {
        if (string.IsNullOrWhiteSpace(delhiveryStatus))
            return OrderStatus.Confirmed;

        var s = delhiveryStatus.Trim().ToLowerInvariant();

        // Delivered
        if (s.Contains("delivered") || s == "dl")
            return OrderStatus.Delivered;

        // RTO / return
        if (s.Contains("rto") || s.Contains("return") || s.Contains("dto"))
            return OrderStatus.Returned;

        // Cancelled
        if (s.Contains("cancel"))
            return OrderStatus.Cancelled;

        // In-flight
        if (s.Contains("in transit") || s.Contains("in-transit")) return OrderStatus.Shipped;
        if (s.Contains("out for delivery") || s.Contains("ofd")) return OrderStatus.Shipped;
        if (s.Contains("dispatched") || s.Contains("picked") || s == "pu") return OrderStatus.Shipped;

        // Manifested / open
        if (s.Contains("manifest") || s.Contains("open") ||
            s.Contains("scheduled") || s.Contains("pending"))
            return OrderStatus.Confirmed;

        // Undelivered / NDR — still in flight (retry expected)
        if (s.Contains("undelivered") || s.Contains("ndr") || s == "ud")
            return OrderStatus.Shipped;

        return OrderStatus.Shipped;
    }
}