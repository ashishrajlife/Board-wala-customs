using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;

namespace ValousWorld.Web.Controllers;

public class ShopController : Controller
{
    private readonly AppDbContext _db;
    public ShopController(AppDbContext db) => _db = db;

    // ============================================================
    // CATEGORY PAGE
    // ============================================================
    [Route("category/{id:int}")]
    public async Task<IActionResult> Category(int id)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == id && c.IsActive);
        if (category == null) return NotFound();

        var products = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.CategoryId == category.CategoryId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        ViewBag.Category = category;
        return View("Category", products);
    }

    // ============================================================
    // PRODUCT DETAIL PAGE
    // ============================================================
    [Route("product/{id:int}")]
    public async Task<IActionResult> Product(int id)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);
        if (product == null) return NotFound();

        ViewBag.Related = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive
                        && p.CategoryId == product.CategoryId
                        && p.ProductId != product.ProductId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(4)
            .ToListAsync();

        return View(product);
    }
}