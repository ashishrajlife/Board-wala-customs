using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValousWorld.Web.Services;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "User")]
[Route("orders")]
public class OrderController : Controller
{
    private readonly IOrderService _orders;
    public OrderController(IOrderService orders) => _orders = orders;

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var uid) ? uid : 0;
    }

    [Route("")]
    public async Task<IActionResult> Index()
    {
        var list = await _orders.GetUserOrdersAsync(GetUserId());
        return View("~/Views/Order/Index.cshtml", list);
    }

    [Route("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var order = await _orders.GetOrderAsync(id, GetUserId());
        if (order == null) return NotFound();
        return View("~/Views/Order/Detail.cshtml", order);
    }
}