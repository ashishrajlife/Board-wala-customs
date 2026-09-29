using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;

namespace ValousWorld.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

   public async Task<IActionResult> Index()
    {
        // Products for landing
        var products = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.IsNewArrival)
            .OrderByDescending(p => p.CreatedAt)
            .Take(12)
            .ToListAsync();

        // Categories for landing
        ViewBag.Categories = await _db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.TotalProducts = await _db.Products
            .CountAsync(p => p.IsActive);

        return View("~/Views/Home/Index.cshtml", products);
    }
}