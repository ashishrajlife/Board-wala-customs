using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Services;

namespace ValousWorld.Web.Controllers;

public class ShopController : Controller
{
    private readonly AppDbContext _db;
    private readonly IDeliveryService _delivery;
    private const int PageSize = 12; 

    public ShopController(AppDbContext db, IDeliveryService delivery)
    {
        _db = db;
        _delivery = delivery;
    }

    [Route("category/{id:int}")]
    public async Task<IActionResult> Category(int id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id && c.IsActive);
        if (category == null) return NotFound();

        var products = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.CategoryId == category.CategoryId).AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        ViewBag.Category = category;
        return View("Category", products);
    }

    [Route("product/{id:int}")]
    public async Task<IActionResult> Product(int id)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive && p.Category.IsActive);
        if (product == null) return NotFound();

        ViewBag.Related = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(4)
            .ToListAsync();

        return View(product);
    }

    [Route("shop")]
    public async Task<IActionResult> Shop(
        string? q = null,
        int? categoryId = null,
        string? sort = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? minDiscount = null,
        int? maxDiscount = null,
        string? sizes = null,
        string? colors = null,
        bool? inStock = null,
        int page = 1)
    {
        if (page < 1) page = 1;

        // Base query for filtering
        var query = _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Category.IsActive)
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

        // ----- Filters -----
        if (categoryId.HasValue && categoryId.Value > 0)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (minPrice.HasValue) query = query.Where(p => p.MRP >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => p.MRP <= maxPrice.Value);

        if (minDiscount.HasValue) query = query.Where(p => p.SavePercent >= minDiscount.Value);
        if (maxDiscount.HasValue) query = query.Where(p => p.SavePercent <= maxDiscount.Value);

        if (inStock == true) query = query.Where(p => p.Stock > 0);

        if (!string.IsNullOrWhiteSpace(sizes))
        {
            var sizeList = sizes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var s in sizeList)
            {
                var size = s;
                query = query.Where(p => p.Sizes != null && p.Sizes.Contains(size));
            }
        }

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
            _ => query.OrderByDescending(p => p.IsNewArrival).ThenByDescending(p => p.CreatedAt)
        };

        // ----- Pagination -----
        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        if (page > totalPages) page = totalPages;

        var products = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .AsNoTracking()
            .ToListAsync();

        // ============================================================
        // NAYA LOGIC: SIRF WO SIZES AUR COLORS NIKALO JO DB ME HAIN
        // ============================================================
        var availableProducts = await _db.Products
            .Where(p => p.IsActive && p.Category.IsActive)
            .Select(p => new { p.Sizes, p.Colors })
            .ToListAsync();

        var allSizes = availableProducts
            .Where(p => !string.IsNullOrWhiteSpace(p.Sizes))
            .SelectMany(p => p.Sizes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(s => s.ToUpper()) // Taaki "s" aur "S" dono same treat ho
            .Distinct()
            .OrderBy(s => s.Length).ThenBy(s => s) // S, M, L, XL ke order me
            .ToList();

        var allColors = availableProducts
            .Where(p => !string.IsNullOrWhiteSpace(p.Colors))
            .SelectMany(p => p.Colors.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();

        // ----- ViewBags -----
        ViewBag.Categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
        
        ViewBag.SelectedCategory = categoryId;
        ViewBag.SelectedSort = sort;
        ViewBag.SearchQuery = q;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.MinDiscount = minDiscount;
        ViewBag.MaxDiscount = maxDiscount;
        ViewBag.InStock = inStock;
        
        ViewBag.SelectedSizes = sizes ?? "";
        ViewBag.SelectedColors = colors ?? "";

        // Dynamic filter options
        ViewBag.AllSizes = allSizes;
        ViewBag.AllColors = allColors;

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalCount;

        return View("~/Views/Shop/Shop.cshtml", products);
    }

    [HttpGet, Route("shop/check-pincode")]
    public async Task<IActionResult> CheckPincode(string pincode)
    {
        if (string.IsNullOrWhiteSpace(pincode) || pincode.Length != 6 || !pincode.All(char.IsDigit))
            return Json(new { serviceable = false, cod = false, etaDays = (string?)null, message = "Invalid pincode." });

        try
        {
            var (ok, serviceable, cod, error) = await _delivery.CheckServiceabilityAsync(pincode);
            if (!ok) return Json(new { serviceable = true, cod = true, etaDays = "3-5", warning = true, message = "Could not verify pincode." });
            
            return Json(new
            {
                serviceable,
                cod = serviceable && cod,
                etaDays = serviceable ? "3-5" : (string?)null,
                message = !serviceable ? $"Sorry, we don't deliver to {pincode} yet." : (cod ? $"Delivers to {pincode}" : $"Delivers to {pincode} — prepaid only")
            });
        }
        catch
        {
            return Json(new { serviceable = true, cod = true, etaDays = "3-5", warning = true, message = "Could not verify pincode." });
        }
    }
}