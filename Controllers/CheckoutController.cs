using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Services;
using ValousWorld.Web.Models.Entities;

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


    public CheckoutController(
    AppDbContext db,
    ICartService cart,
    IOrderService orders,
    IRazorpayService razorpay,
    IWhatsAppService whatsapp,
    IDeliveryService delivery,
    IConfiguration configuration)
{
    _db = db;
    _cart = cart;
    _orders = orders;
    _razorpay = razorpay;
    _whatsapp = whatsapp;
    _delivery = delivery;
    _configuration = configuration;
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
    // POST /checkout/place
[HttpPost, Route("place")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Place(int selectedAddressId, decimal shippingAmount = 0)
{
    var userId = GetUserId();
    if (userId == 0) return Unauthorized();

    try
    {
        // 1. Create order
        var order = await _orders.CreateOrderFromCartAsync(userId, selectedAddressId, shippingAmount);

        // 2. Init Razorpay (mock returns fake order id)
        var (rzSuccess, rzOrderId, rzError) = await _razorpay.CreateOrderAsync(order);
        if (!rzSuccess)
        {
            TempData["CheckoutError"] = rzError ?? "Payment initialization failed.";
            return RedirectToAction(nameof(Index));
        }

        order.RazorpayOrderId = rzOrderId;
        await _db.SaveChangesAsync();

        // 3. Redirect to payment page
        return RedirectToAction(nameof(Payment), new { id = order.OrderId });
    }
    catch (Exception ex)
    {
        TempData["CheckoutError"] = ex.Message;
        return RedirectToAction(nameof(Index));
    }
}

[HttpGet, Route("payment/{id:int}")]
public async Task<IActionResult> Payment(int id)
{
    var userId = GetUserId();
    var order = await _orders.GetOrderAsync(id, userId);
    if (order == null) return NotFound();

    ViewBag.RazorpayKeyId = _configuration["Integrations:Razorpay:KeyId"];
    return View("~/Views/Checkout/Payment.cshtml", order);
}

// POST /checkout/verify
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

    var (success, paymentId, error) = await _razorpay.VerifyPaymentAsync(
        razorpay_order_id, razorpay_payment_id, razorpay_signature);

    if (!success)
    {
        order.PaymentStatus = "Failed";
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["CheckoutError"] = error ?? "Payment verification failed.";
        return RedirectToAction(nameof(Payment), new { id = orderId });
    }

    order.PaymentStatus = "Paid";
    order.RazorpayPaymentId = razorpay_payment_id;
    order.RazorpaySignature = razorpay_signature;
    order.Status = OrderStatus.Confirmed;
    order.UpdatedAt = DateTime.UtcNow;

    var (shipSuccess, courier, tracking, _) = await _delivery.CreateShipmentAsync(order);
    if (shipSuccess)
    {
        order.CourierName = courier;
        order.TrackingNumber = tracking;
        order.ShippedAt = DateTime.UtcNow;
        order.Status = OrderStatus.Shipped;
    }

    await _db.SaveChangesAsync();
    await _cart.ClearCartAsync(userId);
    await _whatsapp.SendOrderConfirmationAsync(order);

    return RedirectToAction(nameof(Success), new { id = order.OrderId });
}

// POST /checkout/fail
[HttpPost, Route("fail")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> FailPayment(int orderId)
{
    var userId = GetUserId();
    var order = await _orders.GetOrderAsync(orderId, userId);
    if (order == null) return NotFound();

    order.PaymentStatus = "Failed";
    order.UpdatedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();

    TempData["CheckoutError"] = "Payment was cancelled or failed.";
    return RedirectToAction(nameof(Payment), new { id = orderId });
}

// GET /checkout/success/5
[HttpGet, Route("success/{id:int}")]
public async Task<IActionResult> Success(int id)
{
    var userId = GetUserId();
    var order = await _orders.GetOrderAsync(id, userId);
    if (order == null) return NotFound();

    return View("~/Views/Checkout/Success.cshtml", order);
}
}