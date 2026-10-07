using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValousWorld.Web.Services;
using ValousWorld.Web.Services;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "User")]
[Route("orders")]
public class OrderController : Controller
{
    private readonly IOrderService _orders;
   private readonly IInvoiceService _invoice;
    public OrderController(IOrderService orders, IInvoiceService invoice)
    {
        _orders = orders;
        _invoice = invoice;
    }

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
    // GET /orders/invoice/5
    [HttpGet, Route("invoice/{id:int}")]
    public async Task<IActionResult> Invoice(int id)
    {
        var userId = GetUserId();
        var order = await _orders.GetOrderAsync(id, userId);
        if (order == null) return NotFound();

        var pdfBytes = _invoice.GenerateInvoice(order);
        var fileName = _invoice.GetInvoiceFileName(order);

        return File(pdfBytes, "application/pdf", fileName);
    }

        // POST /orders/cancel/{id}
    [HttpPost, Route("cancel/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string reason)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["CancelError"] = "Please select a reason for cancellation.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var (ok, error, refundInitiated) =
            await _orders.CancelOrderAsync(id, userId, reason, "User");

        if (!ok)
        {
            TempData["CancelError"] = error ?? "Unable to cancel this order.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        TempData["CancelSuccess"] = refundInitiated
            ? "Order cancelled. Your refund has been initiated and will reflect in 5–7 business days."
            : "Order cancelled successfully.";

        return RedirectToAction(nameof(Detail), new { id });
    }

}