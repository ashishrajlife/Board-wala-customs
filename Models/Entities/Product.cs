using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ValousWorld.Web.Models.Entities;

public class Product
{
    [Key]
    public int ProductId { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal MRP { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? SalePrice { get; set; }
    public int? SavePercent { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [StringLength(200)] public string? Sizes { get; set; }
    [StringLength(200)] public string? Colors { get; set; }

    // ---- 4 real image columns stored in DB ----
    [StringLength(300)] public string Image1 { get; set; } = string.Empty;  // required
    [StringLength(300)] public string Image2 { get; set; } = string.Empty;  // required
    [StringLength(300)] public string? Image3 { get; set; }
    [StringLength(300)] public string? Image4 { get; set; }

    // ====================== Video ======================
    [StringLength(300)] 
    public string? VideoUrl { get; set; }

    // ====================== Additional Details ======================
    [StringLength(100)] public string? SKU { get; set; }
    [StringLength(100)] public string? Brand { get; set; }
    [StringLength(200)] public string? Composition { get; set; }
    [StringLength(50)]  public string? GSM { get; set; }
    [StringLength(200)] public string? PrintType { get; set; }
    [StringLength(100)] public string? Neckline { get; set; }
    [StringLength(100)] public string? FitType { get; set; }
    [StringLength(100)] public string? CountryOfProduction { get; set; }
    public string? WashCare { get; set; }
    [StringLength(500)] public string? SizingNote { get; set; }
    [StringLength(200)] public string? MRPNote { get; set; }
    public string? AdditionalNote { get; set; }
    [StringLength(1000)] public string? Tags { get; set; }
    [StringLength(1000)] public string? Benefits { get; set; }
    // =================================================================

    [StringLength(100)] public string? VoucherCode { get; set; }
    public int? VoucherDiscountPercent { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? VoucherMinOrderValue { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? VoucherMaxDiscount { get; set; }
    public DateTime? VoucherValidUntil { get; set; }
    public bool VoucherIsActive { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public bool IsNewArrival { get; set; } = true;
    public int Stock { get; set; } = 100;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // ====================== Shipping (Delhivery) ======================
    public int WeightGrams { get; set; } = 500;
    public int? LengthCm { get; set; }
    public int? WidthCm { get; set; }
    public int? HeightCm { get; set; }
    // =================================================================



    // ---------------- Compatibility helpers ----------------
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