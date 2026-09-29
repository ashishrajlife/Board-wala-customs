using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ValousWorld.Web.Models.Entities;

public class PaymentAttempt
{
    [Key]
    public int PaymentAttemptId { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [StringLength(100)]
    public string? RazorpayOrderId { get; set; }

    [StringLength(100)]
    public string? RazorpayPaymentId { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = "Initiated";   // Initiated, Failed, Success, Abandoned

    [StringLength(100)]
    public string? ErrorCode { get; set; }

    [StringLength(500)]
    public string? ErrorDescription { get; set; }

    [StringLength(50)]
    public string? ErrorReason { get; set; }

    [StringLength(50)]
    public string? ErrorStep { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}