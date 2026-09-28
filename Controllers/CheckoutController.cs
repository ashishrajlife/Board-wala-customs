using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Services;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "User")]
[Route("checkout")]
public class CheckoutController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICartService _cart;

    public CheckoutController(AppDbContext db, ICartService cart)
    {
        _db = db;
        _cart = cart;
    }

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var uid) ? uid : 0;
    }

    // GET /checkout
    [Route("")]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();

        // Load cart with items
        var cart = await _cart.GetCartAsync(userId);

        if (cart == null || cart.Items == null || !cart.Items.Any())
        {
            TempData["CartError"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        // Load user addresses
        var addresses = await _db.Addresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.UpdatedAt ?? a.CreatedAt)
            .ToListAsync();

        ViewBag.Addresses = addresses;
        ViewBag.Subtotal = await _cart.GetSubtotalAsync(userId);
        ViewBag.Savings = await _cart.GetTotalSavingsAsync(userId);
        ViewBag.ItemCount = await _cart.GetItemCountAsync(userId);

        return View("~/Views/Checkout/Index.cshtml", cart);
    }
}