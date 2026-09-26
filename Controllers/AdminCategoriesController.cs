using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Models.Entities;
using ValousWorld.Web.Services;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Categories")]
public class AdminCategoriesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IFileService _files;

    public AdminCategoriesController(AppDbContext db, IFileService files)
    {
        _db = db;
        _files = files;
    }

    [Route("")]
    public async Task<IActionResult> Index()
    {
        var categories = await _db.Categories
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        // Count products for each category, defaulting to 0 if none
        var counts = await _db.Products
            .GroupBy(p => p.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        // Ensure every category has an entry (0 for empty)
        var fullCounts = categories.ToDictionary(
            c => c.CategoryId,
            c => counts.TryGetValue(c.CategoryId, out var n) ? n : 0
        );

        ViewBag.ProductCounts = fullCounts;
        return View("~/Views/Admin/Categories/Index.cshtml", categories);
    }

    [Route("Create")]
    public IActionResult Create()
        => View("~/Views/Admin/Categories/Create.cshtml", new Category());

    [HttpPost, Route("Create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create(Category model, IFormFile? imageFile)
    {
        // Clear navigation/auto properties from validation
        ModelState.Remove(nameof(Category.Products));

        if (!ModelState.IsValid)
            return View("~/Views/Admin/Categories/Create.cshtml", model);

        // Handle upload
        if (imageFile != null && imageFile.Length > 0)
        {
            try
            {
                model.ImageUrl = await _files.SaveImageAsync(imageFile, "categories");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("imageFile", ex.Message);
                return View("~/Views/Admin/Categories/Create.cshtml", model);
            }
        }

        model.Slug = string.IsNullOrWhiteSpace(model.Slug)
            ? Slugify(model.Name)
            : Slugify(model.Slug);

        _db.Categories.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Category created.";
        return RedirectToAction(nameof(Index));
    }

    [Route("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c == null) return NotFound();
        return View("~/Views/Admin/Categories/Edit.cshtml", c);
    }

    [HttpPost, Route("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, Category model, IFormFile? imageFile)
    {
        if (id != model.CategoryId) return BadRequest();

        ModelState.Remove(nameof(Category.Products));

        if (!ModelState.IsValid)
            return View("~/Views/Admin/Categories/Edit.cshtml", model);

        var existing = await _db.Categories.FindAsync(id);
        if (existing == null) return NotFound();

        // Replace image only if a new file was uploaded
        if (imageFile != null && imageFile.Length > 0)
        {
            try
            {
                var oldUrl = existing.ImageUrl;
                existing.ImageUrl = await _files.SaveImageAsync(imageFile, "categories");
                _files.DeleteImage(oldUrl); // cleanup old file
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("imageFile", ex.Message);
                return View("~/Views/Admin/Categories/Edit.cshtml", model);
            }
        }
        else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            // Admin manually edited the URL textbox
            existing.ImageUrl = model.ImageUrl;
        }

        existing.Name = model.Name;
        existing.Slug = string.IsNullOrWhiteSpace(model.Slug)
            ? Slugify(model.Name)
            : Slugify(model.Slug);
        existing.DisplayOrder = model.DisplayOrder;
        existing.IsActive = model.IsActive;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null)
        {
            TempData["Error"] = "Category not found.";
            return RedirectToAction(nameof(Index));
        }

        // Check if any products are linked to this category
        var productCount = await _db.Products.CountAsync(p => p.CategoryId == id);
        if (productCount > 0)
        {
            TempData["Error"] = $"Cannot delete '{category.Name}' — {productCount} product(s) still belong to this category. " +
                                $"Move or delete those products first.";
            return RedirectToAction(nameof(Index));
        }

        var img = category.ImageUrl;
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        _files.DeleteImage(img);
        TempData["Success"] = "Category deleted.";
        return RedirectToAction(nameof(Index));
    }

    private static string Slugify(string input)
        => input.Trim().ToLower().Replace(" ", "-").Replace("'", "").Replace("\"", "");
}