using System.ComponentModel.DataAnnotations;

namespace ValousWorld.Web.Models.ViewModels;

public class UserProfileViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [StringLength(15)]
    public string Phone { get; set; } = string.Empty;
}