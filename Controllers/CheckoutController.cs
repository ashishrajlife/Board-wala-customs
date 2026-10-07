using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Helpers;
using ValousWorld.Web.Models.Entities;
using ValousWorld.Web.Services;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "User")]
[Route("checkout")]
public class CheckoutController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICartService _cart;
    private readonly IOrderService _orders;
    private readonly IRazorpayService _razorpay;
    private readonly IWhatsAppService _whatsapp;
    private readonly IDeliveryService _delivery;
    private readonly IConfiguration _configuration;
    private readonly IPaymentFinalizer _finalizer;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        AppDbContext db,
        ICartService cart,
        IOrderService orders,
        IRazorpayService razorpay,
        IWhatsAppService whatsapp,
        IDeliveryService delivery,
        IConfiguration configuration,
        IPaymentFinalizer finalizer,
        ILogger<CheckoutController> logger)
    {
        _db = db;
        _cart = cart;
        _orders = orders;
        _razorpay = razorpay;
        _whatsapp = whatsapp;
        _delivery = delivery;
        _configuration = configuration;
        _finalizer = finalizer;
        _logger = logger;
    }

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var uid) ? uid : 0;
    }

    // ============================================================
    // CHECKOUT PAGE
    // ============================================================
    [Route("")]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var cart = await _cart.GetCartAsync(userId);

        if (cart == null || cart.Items == null || !cart.Items.Any())
        {
            TempData["CartError"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

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

    // ============================================================
    // PLACE ORDER
    // ============================================================
       [HttpPost, Route("place")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Place(
        int selectedAddressId, string? voucherCode, string? paymentMethod)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            paymentMethod = string.IsNullOrWhiteSpace(paymentMethod)
                ? PaymentMethods.Razorpay
                : paymentMethod;

            if (paymentMethod != PaymentMethods.Razorpay &&
                paymentMethod != PaymentMethods.Cod)
            {
                TempData["CheckoutError"] = "Invalid payment method.";
                return RedirectToAction(nameof(Index));
            }

            var subtotal = await _cart.GetSubtotalAsync(userId);
            decimal shipping = ShippingCalculator.Calculate(subtotal);
            bool isCod = paymentMethod == PaymentMethods.Cod;
            decimal codFee = ShippingCalculator.CodFee(subtotal, isCod);

            var order = await _orders.CreateOrderFromCartAsync(
                userId, selectedAddressId, shipping, voucherCode, paymentMethod, codFee);

            // ---- COD: skip Razorpay entirely ----
            if (isCod)
            {
                await _finalizer.FinalizeCodAsync(order.OrderId, "checkout");
                return RedirectToAction(nameof(Success), new { id = order.OrderId });
            }

            // ---- Online (Razorpay) ----
            var (rzSuccess, rzOrderId, rzError) = await _razorpay.CreateOrderAsync(order);
            if (!rzSuccess)
            {
                _logger.LogError("[CHECKOUT] Razorpay init failed for {OrderNumber}: {Error}",
                    order.OrderNumber, rzError);

                await _orders.MarkPaymentFailedAsync(order, "INIT_FAILED", rzError);

                TempData["CheckoutError"] = rzError ?? "Payment initialization failed.";
                return RedirectToAction(nameof(Index));
            }

            order.RazorpayOrderId = rzOrderId;
            order.Status = OrderStatus.PaymentPending;
            order.PaymentStatus = PaymentStatus.Pending;
            order.LastPaymentAttemptAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _orders.LogPaymentAttemptAsync(order, "Initiated");

            return RedirectToAction(nameof(Payment), new { id = order.OrderId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CHECKOUT] Place order failed for user {UserId}", userId);
            TempData["CheckoutError"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // ============================================================
    // PAYMENT PAGE
    // ============================================================
    [HttpGet, Route("payment/{id:int}")]
    public async Task<IActionResult> Payment(int id)
    {
        var userId = GetUserId();
        var order = await _orders.GetOrderAsync(id, userId);
        if (order == null) return NotFound();

        // COD orders never touch Razorpay
        if (order.PaymentMethod == PaymentMethods.Cod)
            return RedirectToAction(nameof(Success), new { id });

        if (order.PaymentStatus == PaymentStatus.Paid)
            return RedirectToAction(nameof(Success), new { id });

        if (order.Status == OrderStatus.Cancelled)
        {
            TempData["CheckoutError"] = "This order has been cancelled.";
            return RedirectToAction("Index", "Home");
        }

        ViewBag.RazorpayKeyId = _configuration["Integrations:Razorpay:KeyId"];
        return View("~/Views/Checkout/Payment.cshtml", order);
    }

    // ============================================================
    // RETRY PAYMENT
    // ============================================================
    [HttpPost, Route("retry/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(int id)
    {
        var userId = GetUserId();
        var order = await _orders.GetOrderForRetryAsync(id, userId);
        if (order == null)
        {
            TempData["CheckoutError"] = "This order cannot be retried.";
            return RedirectToAction("Index", "Order");
        }

        // Create fresh Razorpay order
        var (rzSuccess, rzOrderId, rzError) = await _razorpay.CreateOrderAsync(order);
        if (!rzSuccess)
        {
            TempData["CheckoutError"] = rzError ?? "Payment initialization failed.";
            return RedirectToAction(nameof(Payment), new { id });
        }

        order.RazorpayOrderId = rzOrderId;
        order.Status = OrderStatus.PaymentPending;
        order.PaymentStatus = PaymentStatus.Pending;
        order.LastPaymentAttemptAt = DateTime.UtcNow;
        order.ExpiresAt = DateTime.UtcNow.AddMinutes(30);
        order.FailureReason = null;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _orders.LogPaymentAttemptAsync(order, "Initiated");

        return RedirectToAction(nameof(Payment), new { id });
    }

    // ============================================================
    // VERIFY PAYMENT (called after Razorpay success)
    // ============================================================
    [HttpPost, Route("verify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPayment(
        int orderId,
        string razorpay_payment_id,
        string razorpay_order_id,
        string razorpay_signature)
    {
        var userId = GetUserId();
        var order = await _orders.GetOrderAsync(orderId, userId);
        if (order == null) return NotFound();

        // Idempotency — already paid
        if (order.PaymentStatus == PaymentStatus.Paid)
            return RedirectToAction(nameof(Success), new { id = orderId });

        var (success, paymentId, error) = await _razorpay.VerifyPaymentAsync(
            razorpay_order_id, razorpay_payment_id, razorpay_signature);

        if (!success)
        {
            _logger.LogWarning("[CHECKOUT] Verification failed for {OrderNumber}: {Error}",
                order.OrderNumber, error);

            await _orders.MarkPaymentFailedAsync(order, "VERIFY_FAILED", error);
            await _orders.LogPaymentAttemptAsync(order, "Failed",
                errorCode: "VERIFY_FAILED", errorDesc: error);

            TempData["CheckoutError"] = error ?? "Payment verification failed.";
            return RedirectToAction(nameof(Payment), new { id = orderId });
        }

        // Mark paid
        var result = await _finalizer.FinalizeAsync(
        order.OrderId, razorpay_payment_id, razorpay_signature, null, "callback");

        if (result == FinalizeResult.AmountMismatch)
        {
            TempData["CheckoutError"] = "Payment amount mismatch. Please contact support.";
            return RedirectToAction(nameof(Payment), new { id = orderId });
        }

        return RedirectToAction(nameof(Success), new { id = order.OrderId });
    }

    // ============================================================
    // FAIL PAYMENT
    // ============================================================
    [HttpPost, Route("fail")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FailPayment(
        int orderId,
        string? errorCode,
        string? errorDesc,
        string? errorReason,
        string? errorStep)
    {
        var userId = GetUserId();
        var order = await _orders.GetOrderAsync(orderId, userId);
        if (order == null) return NotFound();

        // Already paid? Don't override
        if (order.PaymentStatus == PaymentStatus.Paid)
            return RedirectToAction(nameof(Success), new { id = orderId });

        await _orders.MarkPaymentFailedAsync(order, errorCode, errorDesc, errorReason);
        await _orders.LogPaymentAttemptAsync(order, "Failed",
            errorCode, errorDesc, errorReason, errorStep);

        TempData["CheckoutError"] = !string.IsNullOrEmpty(errorDesc)
            ? errorDesc
            : "Payment was cancelled or failed. You can try again.";

        return RedirectToAction(nameof(Payment), new { id = orderId });
    }

    // ============================================================
    // SUCCESS PAGE
    // ============================================================
    [HttpGet, Route("success/{id:int}")]
    public async Task<IActionResult> Success(int id)
    {
        var userId = GetUserId();
        var order = await _orders.GetOrderAsync(id, userId);
        if (order == null) return NotFound();

        // Razorpay orders must be paid; COD orders are Confirmed with CodPending
        var showable = order.PaymentStatus == PaymentStatus.Paid
                    || (order.PaymentMethod == PaymentMethods.Cod
                        && (order.Status == OrderStatus.Confirmed
                            || order.Status == OrderStatus.Shipped));

        if (!showable)
            return RedirectToAction(nameof(Payment), new { id });

        return View("~/Views/Checkout/Success.cshtml", order);
    }

    [HttpGet, Route("status/{id:int}")]
    public async Task<IActionResult> Status(int id)
    {
        var userId = GetUserId();
        var order = await _orders.GetOrderAsync(id, userId);
        if (order == null) return NotFound();

        Response.Headers["Cache-Control"] = "no-store";
        return Json(new
        {
            paid = order.PaymentStatus == PaymentStatus.Paid,
            paymentStatus = order.PaymentStatus,
            orderStatus = order.Status,
            successUrl = Url.Action(nameof(Success), new { id })
        });
    }
}