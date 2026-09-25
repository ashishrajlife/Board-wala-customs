using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Products")]
public class AdminProductsController : Controller
{
    private readonly AppDbContext _db;
    public AdminProductsController(AppDbContext db) => _db = db;

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
    public async Task<IActionResult> Create(Product model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model.CategoryId);
            return View("~/Views/Admin/Products/Create.cshtml", model);
        }

        model.Slug = string.IsNullOrWhiteSpace(model.Slug) ? Slugify(model.Name) : Slugify(model.Slug);
        model.SavePercent = model.MRP > 0 ? (int)Math.Round((model.MRP - model.SalePrice) / model.MRP * 100) : 0;
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
    public async Task<IActionResult> Edit(int id, Product model)
    {
        if (id != model.ProductId) return BadRequest();
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model.CategoryId);
            return View("~/Views/Admin/Products/Edit.cshtml", model);
        }

        var existing = await _db.Products.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = model.Name;
        existing.Slug = string.IsNullOrWhiteSpace(model.Slug) ? Slugify(model.Name) : Slugify(model.Slug);
        existing.ShortDescription = model.ShortDescription;
        existing.Description = model.Description;
        existing.MRP = model.MRP;
        existing.SalePrice = model.SalePrice;
        existing.SavePercent = model.MRP > 0 ? (int)Math.Round((model.MRP - model.SalePrice) / model.MRP * 100) : 0;
        existing.CategoryId = model.CategoryId;
        existing.Sizes = model.Sizes;
        existing.Colors = model.Colors;
        existing.PrimaryImageUrl = model.PrimaryImageUrl;
        existing.SecondaryImageUrl = model.SecondaryImageUrl;
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
        if (product != null)
        {
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Product deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategoriesAsync(int? selected = null)
    {
        var cats = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
        ViewBag.Categories = new SelectList(cats, "CategoryId", "Name", selected);
    }

    private static string Slugify(string input)
    {
        return input.Trim().ToLower()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "");
    }
}