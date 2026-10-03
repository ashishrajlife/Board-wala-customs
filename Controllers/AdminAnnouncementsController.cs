using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Announcements")]
public class AdminAnnouncementsController : Controller
{
    private readonly AppDbContext _db;

    public AdminAnnouncementsController(AppDbContext db) => _db = db;

    // ============================================================
    // INDEX
    // ============================================================
    [Route("")]
    public async Task<IActionResult> Index()
    {
        var list = await _db.Announcements
            .OrderBy(a => a.DisplayOrder)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return View("~/Views/Admin/Announcements/Index.cshtml", list);
    }

    // ============================================================
    // CREATE
    // ============================================================
    [Route("Create")]
    public IActionResult Create()
    {
        return View("~/Views/Admin/Announcements/Form.cshtml", new Announcement
        {
            IsActive = true
        });
    }

    [HttpPost, Route("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Announcement model)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/Announcements/Form.cshtml", model);

        model.Text = model.Text.Trim();
        model.CreatedAt = DateTime.UtcNow;

        _db.Announcements.Add(model);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Announcement created.";
        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    // EDIT
    // ============================================================
    [Route("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Announcements.FindAsync(id);
        if (item == null) return NotFound();

        return View("~/Views/Admin/Announcements/Form.cshtml", item);
    }

    [HttpPost, Route("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Announcement model)
    {
        if (id != model.AnnouncementId) return BadRequest();

        if (!ModelState.IsValid)
            return View("~/Views/Admin/Announcements/Form.cshtml", model);

        var existing = await _db.Announcements.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Text = model.Text.Trim();
        existing.DisplayOrder = model.DisplayOrder;
        existing.IsActive = model.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Announcement updated.";
        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    // DELETE
    // ============================================================
    [HttpPost, Route("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Announcements.FindAsync(id);
        if (item == null)
        {
            TempData["Error"] = "Announcement not found.";
            return RedirectToAction(nameof(Index));
        }

        _db.Announcements.Remove(item);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Announcement deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("ToggleActive/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var a = await _db.Announcements.FindAsync(id);
        if (a == null)
        {
            TempData["Error"] = "Announcement not found.";
            return RedirectToAction(nameof(Index));
        }

        a.IsActive = !a.IsActive;
        await _db.SaveChangesAsync();

        TempData["Success"] = a.IsActive ? "Announcement is now ACTIVE." : "Announcement is now INACTIVE.";
        return RedirectToAction(nameof(Index));
    }

}