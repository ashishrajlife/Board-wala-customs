using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ValousWorld.Web.Models.Entities;

public class Product
{
    [Key]
    public int ProductId { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal MRP { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal SalePrice { get; set; }

    public int SavePercent { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [StringLength(200)] public string? Sizes { get; set; }
    [StringLength(200)] public string? Colors { get; set; }

    // ---- 4 real image columns stored in DB ----
    [StringLength(300)] public string Image1 { get; set; } = string.Empty;  // required
    [StringLength(300)] public string Image2 { get; set; } = string.Empty;  // required
    [StringLength(300)] public string? Image3 { get; set; }
    [StringLength(300)] public string? Image4 { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public bool IsNewArrival { get; set; } = true;
    public int Stock { get; set; } = 100;
    public int DisplayOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // ---------------- Compatibility helpers ----------------
    // Keeps old code (views/controllers) working AND stores on Image1/Image2.
    [NotMapped]
    public string PrimaryImageUrl
    {
        get => Image1;
        set => Image1 = value ?? string.Empty;
    }

    [NotMapped]
    public string? SecondaryImageUrl
    {
        get => Image2;
        set => Image2 = value ?? string.Empty;
    }
}