using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ValousWorld.Web.Models.Entities;

public class ProductImage
{
    [Key]
    public int ProductImageId { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}