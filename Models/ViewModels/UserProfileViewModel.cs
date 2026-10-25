using System.ComponentModel.DataAnnotations;

namespace ValousWorld.Web.Models.ViewModels
{
    public class UserProfileViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone number must be 10 digits.")]
        public string Phone { get; set; } = string.Empty;
        
        public string? NewPassword { get; set; }
        
        public string? ConfirmPassword { get; set; }
        public string? OtpCode { get; set; }
        
        public bool IsOtpSent { get; set; } = false; 
    }
}