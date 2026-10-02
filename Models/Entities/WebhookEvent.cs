using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ValousWorld.Web.Models.Entities;

[Index(nameof(EventId), IsUnique = true)]
public class WebhookEvent
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string EventId { get; set; } = string.Empty;

    [StringLength(60)]
    public string? EventType { get; set; }

    [StringLength(100)]
    public string? RazorpayOrderId { get; set; }

    [StringLength(100)]
    public string? RazorpayPaymentId { get; set; }

    public string Payload { get; set; } = string.Empty;

    // Received, Processed, Failed, Unmatched, Flagged
    [StringLength(20)]
    public string Status { get; set; } = "Received";

    public int Attempts { get; set; }

    [StringLength(1000)]
    public string? Error { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}