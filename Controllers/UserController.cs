using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValousWorld.Web.Data;
using ValousWorld.Web.Models.ViewModels;
using ValousWorld.Web.Services;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "User,Admin")]
public class UserController : Controller
{
    private readonly AppDbContext _db;
    private readonly IOrderService _orders;
    private readonly IOtpService _otpService;

    public UserController(IOrderService orders, AppDbContext db, IOtpService otpService)
    {
        _orders = orders;
        _db = db;
        _otpService = otpService;
    }

    private int GetUserId()
    {
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var uid) ? uid : 0;
    }

    public async Task<IActionResult> Dashboard()
    {
        var userId = GetUserId();
       var user = await _db.Users.FindAsync(userId);
        ViewBag.UserName = user?.FullName ?? "User";
        var orders = await _orders.GetUserOrdersAsync(userId);
        return View("~/Views/User/Dashboard.cshtml", orders);
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var model = new UserProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? "",
            Phone = user.Phone ?? "",
            IsOtpSent = false
        };

        return View("~/Views/User/EditProfile.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(UserProfileViewModel model, string actionType)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // Duplicate Email/Phone Check
        if (model.Email != user.Email && await _db.Users.AnyAsync(u => u.Email == model.Email && u.UserId != userId))
        {
            ModelState.AddModelError("Email", "This email is already registered.");
        }
        if (model.Phone != user.Phone && await _db.Users.AnyAsync(u => u.Phone == model.Phone && u.UserId != userId))
        {
            ModelState.AddModelError("Phone", "This phone number is already in use.");
        }

        if (!ModelState.IsValid)
            return View("~/Views/User/EditProfile.cshtml", model);

        // Check agar phone ya email change ho raha hai
        bool isContactChanging = (model.Email != user.Email || model.Phone != user.Phone);

        if (isContactChanging)
        {
            // 1. Agar user ne "Send OTP" dabaya
            if (actionType == "SendOtp")
            {
                var (success, error) = await _otpService.SendOtpAsync(model.Phone, "ProfileUpdate");
                if (success)
                {
                    TempData["Success"] = $"OTP sent to {model.Phone} (Demo OTP is 1234).";
                    model.NewPassword = model.NewPassword; 
                    model.ConfirmPassword = model.ConfirmPassword;
                    model.IsOtpSent = true; 
                    return View("~/Views/User/EditProfile.cshtml", model);
                }
                else
                {
                    ModelState.AddModelError("", error ?? "Failed to send OTP.");
                    return View("~/Views/User/EditProfile.cshtml", model);
                }
            }

            // 2. Agar user ne "Verify & Save" dabaya aur OTP khali hai
            if (string.IsNullOrWhiteSpace(model.OtpCode))
            {
                ModelState.AddModelError("OtpCode", "Please enter the OTP.");
                model.IsOtpSent = true;
                return View("~/Views/User/EditProfile.cshtml", model);
            }

            // 3. OTP Verify karo (Mock service hamesha 1234 accept karegi)
            var (verifySuccess, verifyError) = await _otpService.VerifyOtpAsync(model.Phone, model.OtpCode, "ProfileUpdate");
            if (!verifySuccess)
            {
                ModelState.AddModelError("OtpCode", verifyError ?? "Invalid OTP.");
                model.IsOtpSent = true;
                return View("~/Views/User/EditProfile.cshtml", model);
            }
        }

        // --- SAVE DATA ---
        user.FullName = model.FullName;
        user.Email = model.Email;
        user.Phone = model.Phone;
        
        // Password Update Logic (Plain Text - As requested)
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
                return View("~/Views/User/EditProfile.cshtml", model);
            }
            
            // Plain text me save kar rahe hain
            user.Password = model.NewPassword; 
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Profile updated successfully!";
        return RedirectToAction(nameof(Dashboard));
    }
}