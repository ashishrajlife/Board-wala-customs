using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Models.Entities;
using ValousWorld.Web.Services;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Products")]
public class AdminProductsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IFileService _files;

    public AdminProductsController(AppDbContext db, IFileService files)
    {
        _db = db;
        _files = files;
    }

    // ============================================================
    // INDEX
    // ============================================================
    [Route("")]
    public async Task<IActionResult> Index()
    {
        var products = await _db.Products
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View("~/Views/Admin/Products/Index.cshtml", products);
    }

    // ============================================================
    // CREATE
    // ============================================================
    [Route("Create")]
    public async Task<IActionResult> Create()
    {
        await LoadCategoriesAsync();
        return View("~/Views/Admin/Products/Create.cshtml",
            new Product { IsActive = true, IsNewArrival = true, Stock = 100 });
    }

    [HttpPost, Route("Create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Create(
        Product model,
        IFormFile? img1,
        IFormFile? img2,
        IFormFile? img3,
        IFormFile? img4)
    {
        ModelState.Remove(nameof(Product.Category));
        ModelState.Remove(nameof(Product.PrimaryImageUrl));
        ModelState.Remove(nameof(Product.SecondaryImageUrl));

        if (string.IsNullOrWhiteSpace(model.Slug))
            model.Slug = Slugify(model.Name);

        // At least 2 images required
        var uploadedCount = new[] { img1, img2, img3, img4 }
            .Count(f => f != null && f.Length > 0);
        if (uploadedCount < 2)
            ModelState.AddModelError("", "At least 2 images are required.");

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model.CategoryId);
            return View("~/Views/Admin/Products/Create.cshtml", model);
        }

        try
        {
            var uploads = new[] { img1, img2, img3, img4 };
            var saved = new List<string>();
            foreach (var f in uploads)
            {
                if (f != null && f.Length > 0)
                    saved.Add(await _files.SaveImageAsync(f, "products"));
            }

            model.Image1 = saved.ElementAtOrDefault(0) ?? string.Empty;
            model.Image2 = saved.ElementAtOrDefault(1) ?? string.Empty;
            model.Image3 = saved.ElementAtOrDefault(2);
            model.Image4 = saved.ElementAtOrDefault(3);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadCategoriesAsync(model.CategoryId);
            return View("~/Views/Admin/Products/Create.cshtml", model);
        }

        model.SavePercent = model.MRP > 0
            ? (int)Math.Round((model.MRP - model.SalePrice) / model.MRP * 100)
            : 0;
        model.CreatedAt = DateTime.UtcNow;

        _db.Products.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Product created.";
        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    // EDIT
    // ============================================================
    [Route("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        await LoadCategoriesAsync(product.CategoryId);
        return View("~/Views/Admin/Products/Edit.cshtml", product);
    }

    [HttpPost, Route("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Edit(
        int id,
        Product model,
        IFormFile? img1,
        IFormFile? img2,
        IFormFile? img3,
        IFormFile? img4)
    {
        if (id != model.ProductId) return BadRequest();

        ModelState.Remove(nameof(Product.Category));
        ModelState.Remove(nameof(Product.PrimaryImageUrl));
        ModelState.Remove(nameof(Product.SecondaryImageUrl));

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model.CategoryId);
            return View("~/Views/Admin/Products/Edit.cshtml", model);
        }

        var existing = await _db.Products.FindAsync(id);
        if (existing == null) return NotFound();

        try
        {
            // Replace image only if a new file was uploaded; else keep existing
            if (img1 != null && img1.Length > 0)
            {
                var old = existing.Image1;
                existing.Image1 = await _files.SaveImageAsync(img1, "products");
                _files.DeleteImage(old);
            }
            if (img2 != null && img2.Length > 0)
            {
                var old = existing.Image2;
                existing.Image2 = await _files.SaveImageAsync(img2, "products");
                _files.DeleteImage(old);
            }
            if (img3 != null && img3.Length > 0)
            {
                var old = existing.Image3;
                existing.Image3 = await _files.SaveImageAsync(img3, "products");
                _files.DeleteImage(old);
            }
            if (img4 != null && img4.Length > 0)
            {
                var old = existing.Image4;
                existing.Image4 = await _files.SaveImageAsync(img4, "products");
                _files.DeleteImage(old);
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadCategoriesAsync(model.CategoryId);
            return View("~/Views/Admin/Products/Edit.cshtml", model);
        }

        existing.Name = model.Name;
        existing.Slug = string.IsNullOrWhiteSpace(model.Slug)
            ? Slugify(model.Name)
            : Slugify(model.Slug);
        existing.ShortDescription = model.ShortDescription;
        existing.Description = model.Description;
        existing.MRP = model.MRP;
        existing.SalePrice = model.SalePrice;
        existing.SavePercent = model.MRP > 0
            ? (int)Math.Round((model.MRP - model.SalePrice) / model.MRP * 100)
            : 0;
        existing.CategoryId = model.CategoryId;
        existing.Sizes = model.Sizes;
        existing.Colors = model.Colors;
        existing.IsActive = model.IsActive;
        existing.IsFeatured = model.IsFeatured;
        existing.IsNewArrival = model.IsNewArrival;
        existing.Stock = model.Stock;
        existing.DisplayOrder = model.DisplayOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Product updated.";
        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    // DELETE
    // ============================================================
    [HttpPost, Route("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
        {
            TempData["Error"] = "Product not found.";
            return RedirectToAction(nameof(Index));
        }

        var imgs = new[]
        {
            product.Image1,
            product.Image2,
            product.Image3,
            product.Image4
        };

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        foreach (var img in imgs)
            _files.DeleteImage(img);

        TempData["Success"] = "Product deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    // HELPERS
    // ============================================================
    private async Task LoadCategoriesAsync(int? selected = null)
    {
        var cats = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
        ViewBag.Categories = new SelectList(cats, "CategoryId", "Name", selected);
    }

    private static string Slugify(string input)
        => input.Trim().ToLower()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "");
}