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

    public UserController(IOrderService orders, AppDbContext db)
    {
        _orders = orders;
        _db = db;
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

    // ============================================================
    // EDIT PROFILE (GET)
    // ============================================================
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
            Phone = user.Phone ?? ""
        };

        return View("~/Views/User/EditProfile.cshtml", model);
    }

    // ============================================================
    // EDIT PROFILE (POST) - Direct Save (No OTP)
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(UserProfileViewModel model)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // 1. Duplicate Email/Phone Check
        if (model.Email != user.Email && await _db.Users.AnyAsync(u => u.Email == model.Email && u.UserId != userId))
            ModelState.AddModelError("Email", "This email is already registered.");
        
        if (model.Phone != user.Phone && await _db.Users.AnyAsync(u => u.Phone == model.Phone && u.UserId != userId))
            ModelState.AddModelError("Phone", "This phone number is already in use.");

        // 2. Password Match Check (Agar naya password daala hai)
        if (!string.IsNullOrWhiteSpace(model.NewPassword) && model.NewPassword != model.ConfirmPassword)
            ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");

        if (!ModelState.IsValid)
            return View("~/Views/User/EditProfile.cshtml", model);

        // 3. Save Data
        user.FullName = model.FullName;
        user.Email = model.Email;
        user.Phone = model.Phone;
        
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            user.Password = model.NewPassword; // Plain text as per your current setup
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Profile updated successfully!";
        return RedirectToAction(nameof(Dashboard));
    }
}