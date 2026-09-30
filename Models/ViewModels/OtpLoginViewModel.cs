using System.ComponentModel.DataAnnotations;

namespace ValousWorld.Web.Models.ViewModels;

public class OtpLoginViewModel
{
    [Required(ErrorMessage = "Phone number is required")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Enter 10-digit mobile number")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter valid Indian mobile number")]
    public string Phone { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class OtpVerifyViewModel
{
    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP is required")]
    [StringLength(10, MinimumLength = 4)]
    public string Otp { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}