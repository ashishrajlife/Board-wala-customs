using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "User")]
[Route("address")]
public class AddressController : Controller
{
    private readonly AppDbContext _db;

    public AddressController(AppDbContext db) => _db = db;

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var uid) ? uid : 0;
    }

    // GET /address — list
    [Route("")]
    public async Task<IActionResult> Index(string? returnUrl = null)
    {
        var userId = GetUserId();
        var list = await _db.Addresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.UpdatedAt ?? a.CreatedAt)
            .ToListAsync();

        ViewBag.ReturnUrl = SafeReturnUrl(returnUrl);
        return View("~/Views/Address/Index.cshtml", list);
    }

    // GET /address/add
    [Route("add")]
    public IActionResult Add(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = SafeReturnUrl(returnUrl);
        return View("~/Views/Address/Form.cshtml", new Address());
    }

    // POST /address/add
    [HttpPost, Route("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Address model, string? returnUrl = null)
    {
        ModelState.Remove(nameof(Address.User));

        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = SafeReturnUrl(returnUrl);
            return View("~/Views/Address/Form.cshtml", model);
        }

        var userId = GetUserId();
        model.UserId = userId;
        model.CreatedAt = DateTime.UtcNow;

        var hasAny = await _db.Addresses.AnyAsync(a => a.UserId == userId);
        if (!hasAny) model.IsDefault = true;

        if (model.IsDefault)
        {
            var others = await _db.Addresses
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync();
            foreach (var o in others) o.IsDefault = false;
        }

        _db.Addresses.Add(model);
        await _db.SaveChangesAsync();

        TempData["AddressSuccess"] = "Address added.";

        var safe = SafeReturnUrl(returnUrl);
        if (safe != null) return Redirect(safe);

        return RedirectToAction(nameof(Index));
    }

    // GET /address/edit/5
    [Route("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, string? returnUrl = null)
    {
        var userId = GetUserId();
        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == userId);
        if (address == null) return NotFound();

        ViewBag.ReturnUrl = SafeReturnUrl(returnUrl);
        return View("~/Views/Address/Form.cshtml", address);
    }

    // POST /address/edit/5
    [HttpPost, Route("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Address model, string? returnUrl = null)
    {
        ModelState.Remove(nameof(Address.User));

        if (id != model.AddressId) return BadRequest();
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = SafeReturnUrl(returnUrl);
            return View("~/Views/Address/Form.cshtml", model);
        }

        var userId = GetUserId();
        var existing = await _db.Addresses
            .FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == userId);
        if (existing == null) return NotFound();

        existing.FullName = model.FullName;
        existing.Phone = model.Phone;
        existing.Line1 = model.Line1;
        existing.Line2 = model.Line2;
        existing.City = model.City;
        existing.State = model.State;
        existing.Pincode = model.Pincode;
        existing.Country = string.IsNullOrWhiteSpace(model.Country) ? "India" : model.Country;
        existing.UpdatedAt = DateTime.UtcNow;

        if (model.IsDefault && !existing.IsDefault)
        {
            existing.IsDefault = true;
            var others = await _db.Addresses
                .Where(a => a.UserId == userId && a.AddressId != id && a.IsDefault)
                .ToListAsync();
            foreach (var o in others) o.IsDefault = false;
        }

        await _db.SaveChangesAsync();
        TempData["AddressSuccess"] = "Address updated.";

        var safe = SafeReturnUrl(returnUrl);
        if (safe != null) return Redirect(safe);

        return RedirectToAction(nameof(Index));
    }

    // POST /address/delete/5
    [HttpPost, Route("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var userId = GetUserId();
        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == userId);

        var safe = SafeReturnUrl(returnUrl);

        if (address == null)
        {
            TempData["AddressError"] = "Address not found.";
            return RedirectToAction(nameof(Index), new { returnUrl = safe });
        }

        _db.Addresses.Remove(address);
        await _db.SaveChangesAsync();

        if (address.IsDefault)
        {
            var next = await _db.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
                .FirstOrDefaultAsync();
            if (next != null)
            {
                next.IsDefault = true;
                await _db.SaveChangesAsync();
            }
        }

        TempData["AddressSuccess"] = "Address removed.";
        return RedirectToAction(nameof(Index), new { returnUrl = safe });
    }

    // POST /address/set-default/5
    [HttpPost, Route("set-default/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(int id, string? returnUrl = null)
    {
        var userId = GetUserId();
        var all = await _db.Addresses.Where(a => a.UserId == userId).ToListAsync();

        foreach (var a in all)
            a.IsDefault = (a.AddressId == id);

        await _db.SaveChangesAsync();
        TempData["AddressSuccess"] = "Default address updated.";

        var safe = SafeReturnUrl(returnUrl);
        return RedirectToAction(nameof(Index), new { returnUrl = safe });
    }

    // --- helper: only allow local return URLs ---
    private string? SafeReturnUrl(string? returnUrl)
        => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;
}