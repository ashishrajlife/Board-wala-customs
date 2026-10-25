using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;

namespace ValousWorld.Web.Controllers;

public class HomeController : Controller
{
    private const int PageSize = 24;   // 4 rows × 6 cols (desktop), 12 rows × 2 cols (mobile)

    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int page = 1)
    {
        if (page < 1) page = 1;

        // ---------- Paged new-arrival products ----------
        var baseQuery = _db.Products
            .Include(p => p.Category)
            .Where(p => p.Category.IsActive && p.IsNewArrival && p.IsActive);

        var totalCount = await baseQuery.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        if (page > totalPages) page = totalPages;

        var products = await baseQuery.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        // ---------- View data ----------
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages  = totalPages;
        ViewBag.TotalCount  = totalCount;
        ViewBag.PageSize    = PageSize;

        ViewBag.Categories = await _db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.TotalProducts = await _db.Products
            .CountAsync(p => p.IsActive && p.Category.IsActive);

        return View("~/Views/Home/Index.cshtml", products);
    }

    public IActionResult Privacy() => View();
    public IActionResult Terms() => View();
    public IActionResult Returns() => View();
    public IActionResult About() => View();
}