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

    // ============================================================
// SHOP / SEARCH / FILTER / SORT
// ============================================================
[Route("shop")]
public async Task<IActionResult> Shop(
    string? q = null,
    int? categoryId = null,
    string? sort = null,
    decimal? minPrice = null,
    decimal? maxPrice = null,
    string? sizes = null,
    string? colors = null)
{
    var query = _db.Products
        .Include(p => p.Category)
        .Where(p => p.IsActive)
        .AsQueryable();

    // ----- Search -----
    if (!string.IsNullOrWhiteSpace(q))
    {
        var term = q.Trim().ToLower();
        query = query.Where(p =>
            p.Name.ToLower().Contains(term) ||
            (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(term)) ||
            (p.Description != null && p.Description.ToLower().Contains(term)) ||
            (p.Tags != null && p.Tags.ToLower().Contains(term)) ||
            (p.Brand != null && p.Brand.ToLower().Contains(term)) ||
            (p.Category != null && p.Category.Name.ToLower().Contains(term)));
    }

    // ----- Category filter -----
    if (categoryId.HasValue && categoryId.Value > 0)
    {
        query = query.Where(p => p.CategoryId == categoryId.Value);
    }

    // ----- Price filter -----
    if (minPrice.HasValue)
        query = query.Where(p => p.MRP >= minPrice.Value);
    if (maxPrice.HasValue)
        query = query.Where(p => p.MRP <= maxPrice.Value);

    // ----- Sizes filter (comma-separated input, e.g. "S,M") -----
    if (!string.IsNullOrWhiteSpace(sizes))
    {
        var sizeList = sizes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var s in sizeList)
        {
            var size = s;
            query = query.Where(p => p.Sizes != null && p.Sizes.Contains(size));
        }
    }

    // ----- Colors filter -----
    if (!string.IsNullOrWhiteSpace(colors))
    {
        var colorList = colors.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var c in colorList)
        {
            var color = c;
            query = query.Where(p => p.Colors != null && p.Colors.Contains(color));
        }
    }

    // ----- Sort -----
    query = sort switch
    {
        "price-asc"  => query.OrderBy(p => p.SalePrice ?? p.MRP),
        "price-desc" => query.OrderByDescending(p => p.SalePrice ?? p.MRP),
        "newest"     => query.OrderByDescending(p => p.CreatedAt),
        "featured"   => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt),
        "best-selling" => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt),
        _ => query.OrderByDescending(p => p.IsNewArrival)
                  .ThenByDescending(p => p.CreatedAt)
    };

    var products = await query.ToListAsync();

    // ----- Load filter options for the sidebar -----
    ViewBag.Categories = await _db.Categories
        .Where(c => c.IsActive)
        .OrderBy(c => c.Name)
        .ToListAsync();

    ViewBag.SelectedCategory = categoryId;
    ViewBag.SelectedSort = sort;
    ViewBag.SearchQuery = q;
    ViewBag.MinPrice = minPrice;
    ViewBag.MaxPrice = maxPrice;
    ViewBag.TotalProducts = products.Count;

    return View("~/Views/Shop/Shop.cshtml", products);
}

}