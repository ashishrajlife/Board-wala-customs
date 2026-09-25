using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Dashboard()
    {
        ViewBag.UserCount = await _db.Users.CountAsync();
        ViewBag.ProductCount = await _db.Products.CountAsync();
        ViewBag.CategoryCount = await _db.Categories.CountAsync();
        ViewBag.NewArrivals = await _db.Products.CountAsync(p => p.IsNewArrival);
          return View("~/Views/Admin/Dashboard.cshtml");
    }
}