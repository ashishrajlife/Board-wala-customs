using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ValousWorld.Web.Models.Entities;

public class Order
{
    [Key]
    public int OrderId { get; set; }

    [Required, StringLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    public int UserId { get; set; }
    public User? User { get; set; }

    // ---- Status ----
    [StringLength(30)]
    public string Status { get; set; } = OrderStatus.Created;

    // ---- Amounts ----
    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Discount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Shipping { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    // ---- Payment ----
    [StringLength(50)]
    public string PaymentMethod { get; set; } = "Razorpay";

    [StringLength(30)]
    public string PaymentStatus { get; set; } = "Created";

    [StringLength(100)]
    public string? RazorpayOrderId { get; set; }

    [StringLength(100)]
    public string? RazorpayPaymentId { get; set; }

    [StringLength(200)]
    public string? RazorpaySignature { get; set; }

    // ---- Payment tracking (NEW) ----
    public int PaymentAttempts { get; set; } = 0;

    public DateTime? LastPaymentAttemptAt { get; set; }

    [StringLength(500)]
    public string? FailureReason { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? PaidAt { get; set; }

    // ---- Refund ----
    [StringLength(100)]
    public string? RefundId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? RefundedAmount { get; set; }

    public DateTime? RefundedAt { get; set; }

    // ---- Shipping Address (SNAPSHOT) ----
    [StringLength(100)] public string ShippingFullName { get; set; } = string.Empty;
    [StringLength(20)]  public string ShippingPhone { get; set; } = string.Empty;
    [StringLength(200)] public string ShippingLine1 { get; set; } = string.Empty;
    [StringLength(200)] public string? ShippingLine2 { get; set; }
    [StringLength(100)] public string ShippingCity { get; set; } = string.Empty;
    [StringLength(100)] public string ShippingState { get; set; } = string.Empty;
    [StringLength(10)]  public string ShippingPincode { get; set; } = string.Empty;
    [StringLength(100)] public string ShippingCountry { get; set; } = "India";

    // ---- Delivery ----
    [StringLength(100)] public string? CourierName { get; set; }
    [StringLength(100)] public string? TrackingNumber { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

        // ---- COD ----
    [Column(TypeName = "decimal(10,2)")]
    public decimal CodFee { get; set; } = 0m;

    public DateTime? CodCollectedAt { get; set; }

    // ---- Cancellation ----
    public DateTime? CancelledAt { get; set; }

    [StringLength(50)]
    public string? CancelledBy { get; set; }        // "User" /"System"

    [StringLength(300)]
    public string? CancellationReason { get; set; }

    [StringLength(50)]
    public string? RefundStatus { get; set; }       // "Initiated" / "Refunded" / "Failed"

    // ---- Timestamps ----
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<PaymentAttempt> PaymentAttemptLog { get; set; } = new List<PaymentAttempt>();
}

// ============================================================
// ORDER STATUS
// ============================================================
public static class OrderStatus
{
    public const string Created = "Created";
    public const string PaymentPending = "PaymentPending";
    public const string Confirmed = "Confirmed";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
    public const string Failed = "Failed";
    public const string Expired = "Expired";
    public const string Refunded = "Refunded";
    public const string Returned = "Returned";           // future-proof
}

// ============================================================
// PAYMENT STATUS
// ============================================================
public static class PaymentStatus
{
    public const string Created = "Created";
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Failed = "Failed";
    public const string Refunded = "Refunded";
    public const string PartiallyRefunded = "PartiallyRefunded";
    public const string CodPending = "CodPending";       // COD placed, cash not yet collected
    public const string Cancelled = "Cancelled";         // Order cancelled before payment
}

// ============================================================
// PAYMENT METHODS
// ============================================================
public static class PaymentMethods
{
    public const string Razorpay = "Razorpay";
    public const string Cod = "COD";
}