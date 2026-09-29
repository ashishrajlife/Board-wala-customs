using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ValousWorld.Web.Models.Entities;

public class CartItem
{
    [Key]
    public int CartItemId { get; set; }

    public int CartId { get; set; }
    public Cart? Cart { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [StringLength(50)]
    public string? Size { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    public int Quantity { get; set; } = 1;

    // Snapshot at add time — future-proof for orders
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }       // sale price (or MRP if no discount)

    [Column(TypeName = "decimal(10,2)")]
    public decimal OriginalMRP { get; set; }     // MRP at add time

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Computed helper
    [NotMapped]
    public decimal LineTotal => UnitPrice * Quantity;
}