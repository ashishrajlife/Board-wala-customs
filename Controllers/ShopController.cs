using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;

namespace ValousWorld.Web.Controllers;

public class ShopController : Controller
{
    private readonly AppDbContext _db;
    public ShopController(AppDbContext db) => _db = db;

    [Route("category/{CategoryId}")]
    public async Task<IActionResult> Category(string CategoryId)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.IsActive);
        if (category == null) return NotFound();

        var products = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.CategoryId == category.CategoryId)
            .ToListAsync();

        ViewBag.Category = category;
        return View("Category", products);
    }

    [Route("product/{ProductId}")]
    public async Task<IActionResult> Product(string ProductId)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.IsActive);
        if (product == null) return NotFound();

        ViewBag.Related = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
            .Take(4)
            .ToListAsync();

        return View(product);
    }
}