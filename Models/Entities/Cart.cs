using System.ComponentModel.DataAnnotations;

namespace ValousWorld.Web.Models.Entities;

public class Cart
{
    [Key]
    public int CartId { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}