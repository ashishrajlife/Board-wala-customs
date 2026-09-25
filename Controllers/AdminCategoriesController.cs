using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Categories")]
public class AdminCategoriesController : Controller
{
    private readonly AppDbContext _db;
    public AdminCategoriesController(AppDbContext db) => _db = db;

    [Route("")]
    public async Task<IActionResult> Index()
        => View(await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync());

    [Route("Create")]
    public IActionResult Create() => View(new Category());

    [HttpPost, Route("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category model)
    {
        if (!ModelState.IsValid) return View(model);
        model.Slug = string.IsNullOrWhiteSpace(model.Slug) ? Slugify(model.Name) : Slugify(model.Slug);
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
        return View(c);
    }

    [HttpPost, Route("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category model)
    {
        if (id != model.CategoryId) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var existing = await _db.Categories.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = model.Name;
        existing.Slug = string.IsNullOrWhiteSpace(model.Slug) ? Slugify(model.Name) : Slugify(model.Slug);
        existing.ImageUrl = model.ImageUrl;
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
        var c = await _db.Categories.FindAsync(id);
        if (c != null)
        {
            _db.Categories.Remove(c);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Category deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    private static string Slugify(string input)
        => input.Trim().ToLower().Replace(" ", "-").Replace("'", "").Replace("\"", "");
}