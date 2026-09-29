using System.ComponentModel.DataAnnotations;

namespace ValousWorld.Web.Models.Entities;

public class Address
{
    [Key]
    public int AddressId { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Line1 { get; set; } = string.Empty;  // House, street

    [StringLength(200)]
    public string? Line2 { get; set; }                 // Landmark, area

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string State { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string Pincode { get; set; } = string.Empty;

    [StringLength(100)]
    public string Country { get; set; } = "India";

    public bool IsDefault { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}