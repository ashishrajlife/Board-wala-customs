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
        var products = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.IsNewArrival)
            .OrderByDescending(p => p.CreatedAt)
            .Take(12)
            .ToListAsync();

        ViewBag.TotalProducts = await _db.Products.CountAsync(p => p.IsActive);
        return View(products);
    }
}