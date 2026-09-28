using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValousWorld.Web.Services;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "User")]
public class CartController : Controller
{
    private readonly ICartService _cart;

    public CartController(ICartService cart) => _cart = cart;

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var uid) ? uid : 0;
    }

    // GET /cart
    [Route("cart")]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var cart = await _cart.GetCartAsync(userId);

        ViewBag.Subtotal = await _cart.GetSubtotalAsync(userId);
        ViewBag.Savings = await _cart.GetTotalSavingsAsync(userId);
        ViewBag.ItemCount = await _cart.GetItemCountAsync(userId);

        return View("~/Views/Cart/Index.cshtml", cart);
    }

    // POST /cart/add
    [HttpPost, Route("cart/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, string? size, string? color, int quantity = 1, string? returnUrl = null)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, error) = await _cart.AddItemAsync(userId, productId, size, color, quantity);

        if (!success)
        {
            TempData["CartError"] = error ?? "Could not add to cart.";
        }
        else
        {
            TempData["CartSuccess"] = "Item added to cart.";
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Cart");
    }

    // POST /cart/update
    [HttpPost, Route("cart/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int cartItemId, int quantity)
    {
        var userId = GetUserId();
        await _cart.UpdateQuantityAsync(userId, cartItemId, quantity);
        return RedirectToAction(nameof(Index));
    }

    // POST /cart/remove
    [HttpPost, Route("cart/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int cartItemId, string? returnUrl = null)
    {
        var userId = GetUserId();
        await _cart.RemoveItemAsync(userId, cartItemId);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    // GET /cart/count — for AJAX (future-proof)
    [HttpGet, Route("cart/count")]
    public async Task<IActionResult> Count()
    {
        var userId = GetUserId();
        var count = await _cart.GetItemCountAsync(userId);
        return Json(new { count });
    }
}