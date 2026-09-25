using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ValousWorld.Web.Models.Entities;

public class Product
{
    [Key]
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal MRP { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal SalePrice { get; set; }

    public int SavePercent { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    // Simple sizes as comma-separated string: "S,M,L,XL"
    public string? Sizes { get; set; }

    // Simple colors as comma-separated string: "Black,Brown"
    public string? Colors { get; set; }

    public string? PrimaryImageUrl { get; set; }
    public string? SecondaryImageUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public bool IsNewArrival { get; set; } = true;
    public int Stock { get; set; } = 100;
    public int DisplayOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}