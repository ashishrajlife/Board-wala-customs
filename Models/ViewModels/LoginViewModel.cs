using System.ComponentModel.DataAnnotations;

namespace ValousWorld.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email or phone is required.")]
    [Display(Name = "Email or Phone")]
    public string EmailOrPhone { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public bool RememberMe { get; set; }
}