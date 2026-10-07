using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValousWorld.Web.Services;
using ValousWorld.Web.ViewModels.Admin;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Orders")]
public class AdminOrdersController : Controller
{
    private readonly IOrderService _orders;
    private readonly IDeliveryService _delivery;
    private readonly ILogger<AdminOrdersController> _logger;

    public AdminOrdersController(
        IOrderService orders,
        IDeliveryService delivery,
        ILogger<AdminOrdersController> logger)
    {
        _orders = orders;
        _delivery = delivery;
        _logger = logger;
    }

    // ============================================================
    // LIST
    // ============================================================
    [Route("")]
    public async Task<IActionResult> Index(
        string? status, string? method, string? q, int page = 1)
    {
        const int pageSize = 20;

        var (orders, total) = await _orders.GetAllOrdersAsync(
            status, method, q, page, pageSize);

        var vm = new AdminOrderListVm
        {
            Orders = orders,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Status = status,
            PaymentMethod = method,
            Search = q
        };

        // Global counters (unaffected by filters) — one small query group
        vm.CountAll        = await _orders.GetOrderCountAsync(null, null, null);
        vm.CountConfirmed  = await _orders.GetOrderCountAsync("Confirmed", null, null);
        vm.CountShipped    = await _orders.GetOrderCountAsync("Shipped", null, null);
        vm.CountDelivered  = await _orders.GetOrderCountAsync("Delivered", null, null);
        vm.CountCancelled  = await _orders.GetOrderCountAsync("Cancelled", null, null);

        // COD pending = method COD AND status != Delivered AND status != Cancelled
        vm.CountCodPending = await _orders.GetCodPendingCountAsync();

        return View("~/Views/Admin/Orders/Index.cshtml", vm);
    }

    // ============================================================
    // DETAIL
    // ============================================================
    [Route("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var order = await _orders.GetOrderForAdminAsync(id);
        if (order == null) return NotFound();

        var vm = new AdminOrderDetailVm
        {
            Order = order,
            Customer = order.User
        };

        return View("~/Views/Admin/Orders/Detail.cshtml", vm);
    }

    // ============================================================
    // ADMIN — CANCEL ORDER
    // ============================================================
    [HttpPost, Route("{id:int}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Please provide a reason for cancellation.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var (ok, error, refundInitiated) =
            await _orders.CancelOrderAsAdminAsync(id, "[Admin] " + reason);

        if (!ok)
        {
            TempData["Error"] = error ?? "Unable to cancel this order.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        TempData["Success"] = refundInitiated
            ? "Order cancelled. Refund has been initiated."
            : "Order cancelled successfully.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ============================================================
    // ADMIN — MARK COD AS DELIVERED (cash collected)
    // ============================================================
    [HttpPost, Route("{id:int}/mark-delivered")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDelivered(int id)
    {
        var order = await _orders.GetOrderForAdminAsync(id);
        if (order == null) return NotFound();

        if (order.PaymentMethod != "COD" ||
            order.Status != "Shipped" ||
            order.PaymentStatus != "CodPending")
        {
            TempData["Error"] = "This order cannot be marked delivered.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        await _orders.MarkCodCollectedAsync(order);
        TempData["Success"] = "Order marked delivered. COD cash recorded as collected.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ============================================================
    // ADMIN — REFUND (manual, for Razorpay-paid orders)
    // ============================================================
    [HttpPost, Route("{id:int}/refund")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(int id, string reason)
    {
        var order = await _orders.GetOrderForAdminAsync(id);
        if (order == null) return NotFound();

        if (order.PaymentMethod != "Razorpay" ||
            order.PaymentStatus != "Paid" ||
            !string.IsNullOrEmpty(order.RefundId))
        {
            TempData["Error"] = "This order is not eligible for manual refund.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        // Use existing cancel flow to trigger refund (its refund block already handles this)
        var (ok, error, refundInitiated) =
            await _orders.CancelOrderAsAdminAsync(id, "[Admin Refund] " + reason);

        TempData[ok && refundInitiated ? "Success" : "Error"] =
            ok && refundInitiated
                ? "Refund initiated successfully."
                : (error ?? "Refund failed.");

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ============================================================
    // ADMIN — RESYNC TRACKING FROM DELHIVERY
    // ============================================================
    [HttpPost, Route("{id:int}/resync")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resync(int id)
    {
        var order = await _orders.GetOrderForAdminAsync(id);
        if (order == null) return NotFound();

        if (string.IsNullOrWhiteSpace(order.TrackingNumber))
        {
            TempData["Error"] = "Order has no tracking number yet.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        try
        {
            // var (ok, status, error) = await _delivery.FetchStatusAsync(order.TrackingNumber);
            // if (!ok || string.IsNullOrWhiteSpace(status))
            // {
            //     TempData["Error"] = error ?? "No status returned from Delhivery.";
            //     return RedirectToAction(nameof(Detail), new { id });
            // }

            // var (syncOk, syncErr) = await _orders.SyncShipmentStatusAsync(order.TrackingNumber, status);
            // TempData[syncOk ? "Success" : "Error"] =
            //     syncOk ? $"Status refreshed: {status}" : (syncErr ?? "Sync failed.");

            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN] Resync failed for order {Id}", id);
            TempData["Error"] = "Resync failed. Check logs.";
            return RedirectToAction(nameof(Detail), new { id });
        }
    }

        // ============================================================
    // ADMIN — INVOICE DOWNLOAD
    // ============================================================
    [HttpGet, Route("{id:int}/invoice")]
    public async Task<IActionResult> Invoice(int id, [FromServices] IInvoiceService invoice)
    {
        var order = await _orders.GetOrderForAdminAsync(id);
        if (order == null) return NotFound();

        var pdf = invoice.GenerateInvoice(order);
        var fileName = invoice.GetInvoiceFileName(order);
        return File(pdf, "application/pdf", fileName);
    }
}