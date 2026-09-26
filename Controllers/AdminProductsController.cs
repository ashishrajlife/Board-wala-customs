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

    [Route("")]
    public async Task<IActionResult> Index()
    {
        var products = await _db.Products
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View("~/Views/Admin/Products/Index.cshtml", products);
    }

    [Route("Create")]
    public async Task<IActionResult> Create()
    {
        await LoadCategoriesAsync();
        return View("~/Views/Admin/Products/Create.cshtml",
            new Product { IsActive = true, IsNewArrival = true, Stock = 100 });
    }

    [HttpPost, Route("Create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Create(Product model, IFormFile? primaryFile, IFormFile? secondaryFile)
    {
        ModelState.Remove(nameof(Product.Category));

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model.CategoryId);
            return View("~/Views/Admin/Products/Create.cshtml", model);
        }

        try
        {
            if (primaryFile != null && primaryFile.Length > 0)
                model.PrimaryImageUrl = await _files.SaveImageAsync(primaryFile, "products");

            if (secondaryFile != null && secondaryFile.Length > 0)
                model.SecondaryImageUrl = await _files.SaveImageAsync(secondaryFile, "products");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadCategoriesAsync(model.CategoryId);
            return View("~/Views/Admin/Products/Create.cshtml", model);
        }

        if (string.IsNullOrWhiteSpace(model.PrimaryImageUrl))
        {
            ModelState.AddModelError("primaryFile", "Primary image is required.");
            await LoadCategoriesAsync(model.CategoryId);
            return View("~/Views/Admin/Products/Create.cshtml", model);
        }

        model.Slug = string.IsNullOrWhiteSpace(model.Slug) ? Slugify(model.Name) : Slugify(model.Slug);
        model.SavePercent = model.MRP > 0
            ? (int)Math.Round((model.MRP - model.SalePrice) / model.MRP * 100)
            : 0;
        model.CreatedAt = DateTime.UtcNow;

        _db.Products.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Product created.";
        return RedirectToAction(nameof(Index));
    }

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
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, Product model, IFormFile? primaryFile, IFormFile? secondaryFile)
    {
        if (id != model.ProductId) return BadRequest();

        ModelState.Remove(nameof(Product.Category));

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model.CategoryId);
            return View("~/Views/Admin/Products/Edit.cshtml", model);
        }

        var existing = await _db.Products.FindAsync(id);
        if (existing == null) return NotFound();

        try
        {
            // Primary image — replace if new file uploaded
            if (primaryFile != null && primaryFile.Length > 0)
            {
                var oldUrl = existing.PrimaryImageUrl;
                existing.PrimaryImageUrl = await _files.SaveImageAsync(primaryFile, "products");
                _files.DeleteImage(oldUrl);
            }
            else if (!string.IsNullOrWhiteSpace(model.PrimaryImageUrl))
            {
                existing.PrimaryImageUrl = model.PrimaryImageUrl;
            }

            // Secondary image — replace if new file uploaded
            if (secondaryFile != null && secondaryFile.Length > 0)
            {
                var oldUrl = existing.SecondaryImageUrl;
                existing.SecondaryImageUrl = await _files.SaveImageAsync(secondaryFile, "products");
                _files.DeleteImage(oldUrl);
            }
            else if (!string.IsNullOrWhiteSpace(model.SecondaryImageUrl))
            {
                existing.SecondaryImageUrl = model.SecondaryImageUrl;
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadCategoriesAsync(model.CategoryId);
            return View("~/Views/Admin/Products/Edit.cshtml", model);
        }

        existing.Name = model.Name;
        existing.Slug = string.IsNullOrWhiteSpace(model.Slug) ? Slugify(model.Name) : Slugify(model.Slug);
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

        var primary = product.PrimaryImageUrl;
        var secondary = product.SecondaryImageUrl;

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        _files.DeleteImage(primary);
        _files.DeleteImage(secondary);

        TempData["Success"] = "Product deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategoriesAsync(int? selected = null)
    {
        var cats = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
        ViewBag.Categories = new SelectList(cats, "CategoryId", "Name", selected);
    }

    private static string Slugify(string input)
        => input.Trim().ToLower().Replace(" ", "-").Replace("'", "").Replace("\"", "");
}