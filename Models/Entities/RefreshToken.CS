using System.ComponentModel.DataAnnotations;

namespace ValousWorld.Web.Models.Entities;

public class RefreshToken
{
    [Key]
    public int TokenId { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}