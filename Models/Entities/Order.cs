using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ValousWorld.Web.Models.Entities;

public class Order
{
    [Key]
    public int OrderId { get; set; }

    [Required, StringLength(50)]
    public string OrderNumber { get; set; } = string.Empty;   // VW-2026-0001

    public int UserId { get; set; }
    public User? User { get; set; }

    // ---- Status ----
    [StringLength(30)]
    public string Status { get; set; } = OrderStatus.Pending;  // Pending, Confirmed, Shipped, Delivered, Cancelled

    // ---- Amounts ----
    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Discount { get; set; }   // savings

    [Column(TypeName = "decimal(10,2)")]
    public decimal Shipping { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    // ---- Payment ----
    [StringLength(50)]
    public string PaymentMethod { get; set; } = "Razorpay";   // Razorpay, COD

    [StringLength(30)]
    public string PaymentStatus { get; set; } = "Pending";     // Pending, Paid, Failed, Refunded

    [StringLength(100)]
    public string? RazorpayOrderId { get; set; }

    [StringLength(100)]
    public string? RazorpayPaymentId { get; set; }

    [StringLength(100)]
    public string? RazorpaySignature { get; set; }

    // ---- Shipping Address (SNAPSHOT) ----
    [StringLength(100)]
    public string ShippingFullName { get; set; } = string.Empty;

    [StringLength(20)]
    public string ShippingPhone { get; set; } = string.Empty;

    [StringLength(200)]
    public string ShippingLine1 { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ShippingLine2 { get; set; }

    [StringLength(100)]
    public string ShippingCity { get; set; } = string.Empty;

    [StringLength(100)]
    public string ShippingState { get; set; } = string.Empty;

    [StringLength(10)]
    public string ShippingPincode { get; set; } = string.Empty;

    [StringLength(100)]
    public string ShippingCountry { get; set; } = "India";

    // ---- Delivery / Tracking ----
    [StringLength(100)]
    public string? CourierName { get; set; }

    [StringLength(100)]
    public string? TrackingNumber { get; set; }

    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    // ---- Timestamps ----
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public static class OrderStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
}