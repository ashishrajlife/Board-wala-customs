using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;

namespace ValousWorld.Web.Controllers;

public class ShopController : Controller
{
    private readonly AppDbContext _db;
    public ShopController(AppDbContext db) => _db = db;

    [Route("category/{slug}")]
    public async Task<IActionResult> Category(string slug)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);
        if (category == null) return NotFound();

        var products = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.CategoryId == category.CategoryId)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();

        ViewBag.Category = category;
        return View("Category", products);
    }

    [Route("product/{slug}")]
    public async Task<IActionResult> Product(string slug)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);
        if (product == null) return NotFound();

        ViewBag.Related = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
            .Take(4)
            .ToListAsync();

        return View(product);
    }
}