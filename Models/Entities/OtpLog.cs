using System.ComponentModel.DataAnnotations;

namespace ValousWorld.Web.Models.Entities;

public class OtpLog
{
    [Key]
    public int OtpLogId { get; set; }

    [Required, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string OtpCode { get; set; } = string.Empty;

    [StringLength(30)]
    public string Purpose { get; set; } = "Login";  // Login, Register, Reset

    public bool IsUsed { get; set; } = false;

    public int AttemptCount { get; set; } = 0;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}