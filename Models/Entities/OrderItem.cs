using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ValousWorld.Web.Models.Entities;

public class OrderItem
{
    [Key]
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    // ---- Snapshot at order time ----
    [Required, StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(300)]
    public string? ProductImage { get; set; }

    [StringLength(50)]
    public string? Size { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal OriginalMRP { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal LineTotal { get; set; }
}