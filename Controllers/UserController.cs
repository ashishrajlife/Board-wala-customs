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

    public UserController(IOrderService orders,AppDbContext db)
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
        var orders = await _orders.GetUserOrdersAsync(userId);
        return View("~/Views/User/Dashboard.cshtml", orders);
    }

        // ============================================================
    // EDIT PROFILE (GET) - Page load karne ke liye
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var userId = GetUserId();
        // Hum direct _db.Users se data nikalenge (kyunki UserController me _db inject nahi hai, use constructor me add karna hoga)
        
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
    // EDIT PROFILE (POST) - Data save karne ke liye
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(UserProfileViewModel model)
    {
        if (!ModelState.IsValid) return View("~/Views/User/EditProfile.cshtml", model);

        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // Check karo ki naya email ya phone kisi aur user ka toh nahi hai
        if (await _db.Users.AnyAsync(u => u.Email == model.Email && u.UserId != userId))
        {
            ModelState.AddModelError("Email", "This email is already registered with another account.");
            return View("~/Views/User/EditProfile.cshtml", model);
        }
        if (await _db.Users.AnyAsync(u => u.Phone == model.Phone && u.UserId != userId))
        {
            ModelState.AddModelError("Phone", "This phone number is already in use.");
            return View("~/Views/User/EditProfile.cshtml", model);
        }

        // Data update karo
        user.FullName = model.FullName;
        user.Email = model.Email;
        user.Phone = model.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Profile updated successfully!";
        return RedirectToAction(nameof(Dashboard)); // Wapas dashboard pe bhej do
    }
}