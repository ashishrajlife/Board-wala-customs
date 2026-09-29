using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValousWorld.Web.Services;

[Authorize(Roles = "User,Admin")]
public class UserController : Controller
{
    private readonly IOrderService _orders;

    public UserController(IOrderService orders)
    {
        _orders = orders;
    }

    private int GetUserId()
    {
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var uid) ? uid : 0;
    }

    public async Task<IActionResult> Dashboard()
    {
        var userId = GetUserId();
        var orders = await _orders.GetUserOrdersAsync(userId);
        return View("~/Views/User/Dashboard.cshtml", orders);
    }
}