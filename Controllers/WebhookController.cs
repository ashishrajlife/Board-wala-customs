using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ValousWorld.Web.Data;
using ValousWorld.Web.Models.Entities;
using ValousWorld.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace ValousWorld.Web.Controllers;

[AllowAnonymous]
[Route("api/webhook")]
public class WebhookController : Controller
{
    private readonly IConfiguration _config;
    private readonly ILogger<WebhookController> _logger;
    private readonly IOrderService _orders;
    private readonly IWhatsAppService _whatsapp;
    private readonly IDeliveryService _delivery;
    private readonly AppDbContext _db;

    public WebhookController(
        IConfiguration config,
        ILogger<WebhookController> logger,
        IOrderService orders,
        IWhatsAppService whatsapp,
        IDeliveryService delivery,
        Data.AppDbContext db)
    {
        _config = config;
        _logger = logger;
        _orders = orders;
        _whatsapp = whatsapp;
        _delivery = delivery;
        _db = db;
    }

    [HttpPost, Route("razorpay")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Razorpay()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var signature = Request.Headers["X-Razorpay-Signature"].ToString();
        var webhookSecret = _config["Integrations:Razorpay:WebhookSecret"];

        _logger.LogInformation("[WEBHOOK] Received. Sig present: {HasSig}", !string.IsNullOrEmpty(signature));

        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            _logger.LogWarning("[WEBHOOK] Secret not configured — accepting in dev mode");
            return Ok();
        }

        if (!VerifySignature(body, signature, webhookSecret))
        {
            _logger.LogWarning("[WEBHOOK] Invalid signature — rejected");
            return Unauthorized();
        }

        var json = JObject.Parse(body);
        var eventType = json["event"]?.ToString();
        _logger.LogInformation("[WEBHOOK] Event: {Event}", eventType);

        try
        {
            switch (eventType)
            {
                case "payment.captured":
                case "order.paid":
                    await HandlePaymentSuccess(json);
                    break;

                case "payment.failed":
                    await HandlePaymentFailed(json);
                    break;

                case "refund.processed":
                    await HandleRefundProcessed(json);
                    break;

                default:
                    _logger.LogInformation("[WEBHOOK] Unhandled event: {Event}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WEBHOOK] Error processing {Event}", eventType);
        }

        return Ok();
    }

    // ============================================================
    // HANDLERS
    // ============================================================
    private async Task HandlePaymentSuccess(JObject json)
    {
        var payment = json["payload"]?["payment"]?["entity"];
        if (payment == null) return;

        var razorpayOrderId = payment["order_id"]?.ToString();
        var razorpayPaymentId = payment["id"]?.ToString();
        var amount = (payment["amount"]?.Value<decimal>() ?? 0) / 100m;

        if (string.IsNullOrEmpty(razorpayOrderId)) return;

        var order = await _orders.GetOrderByRazorpayIdAsync(razorpayOrderId);
        if (order == null)
        {
            _logger.LogWarning("[WEBHOOK] Order not found for {RzpOrderId}", razorpayOrderId);
            return;
        }

        // Idempotency
        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            _logger.LogInformation("[WEBHOOK] {OrderNumber} already paid — skip", order.OrderNumber);
            return;
        }

        // Amount sanity check
        if (Math.Abs(order.Total - amount) > 0.01m)
        {
            _logger.LogWarning("[WEBHOOK] Amount mismatch for {OrderNumber}: expected {Expected}, got {Got}",
                order.OrderNumber, order.Total, amount);
        }

        await _orders.MarkPaymentPaidAsync(order, razorpayPaymentId ?? "", "");
        await _orders.LogPaymentAttemptAsync(order, "Success");

        // Shipment
        var (shipSuccess, courier, tracking, _) = await _delivery.CreateShipmentAsync(order);
        if (shipSuccess)
        {
            order.CourierName = courier;
            order.TrackingNumber = tracking;
            order.ShippedAt = DateTime.UtcNow;
            order.Status = OrderStatus.Shipped;
            await _db.SaveChangesAsync();
        }

        await _whatsapp.SendOrderConfirmationAsync(order);

        _logger.LogInformation("[WEBHOOK] ✅ {OrderNumber} paid via webhook", order.OrderNumber);
    }

    private async Task HandlePaymentFailed(JObject json)
    {
        var payment = json["payload"]?["payment"]?["entity"];
        if (payment == null) return;

        var razorpayOrderId = payment["order_id"]?.ToString();
        var errorCode = payment["error_code"]?.ToString();
        var errorDesc = payment["error_description"]?.ToString();

        if (string.IsNullOrEmpty(razorpayOrderId)) return;

        var order = await _orders.GetOrderByRazorpayIdAsync(razorpayOrderId);
        if (order == null) return;

        if (order.PaymentStatus == PaymentStatus.Paid) return;

        await _orders.MarkPaymentFailedAsync(order, errorCode, errorDesc);
        await _orders.LogPaymentAttemptAsync(order, "Failed", errorCode, errorDesc);

        _logger.LogWarning("[WEBHOOK] ❌ {OrderNumber} failed: [{Code}] {Desc}",
            order.OrderNumber, errorCode, errorDesc);
    }

    private async Task HandleRefundProcessed(JObject json)
    {
        var refund = json["payload"]?["refund"]?["entity"];
        if (refund == null) return;

        var paymentId = refund["payment_id"]?.ToString();
        var refundId = refund["id"]?.ToString();
        var amount = (refund["amount"]?.Value<decimal>() ?? 0) / 100m;

        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.RazorpayPaymentId == paymentId);
        if (order == null) return;

        order.RefundId = refundId;
        order.RefundedAmount = amount;
        order.RefundedAt = DateTime.UtcNow;
        order.PaymentStatus = amount >= order.Total
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;

        if (order.PaymentStatus == PaymentStatus.Refunded)
            order.Status = OrderStatus.Refunded;

        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("[WEBHOOK] 💰 Refund {Amount} for {OrderNumber}",
            amount, order.OrderNumber);
    }

    private static bool VerifySignature(string body, string signature, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var expected = BitConverter.ToString(hash).Replace("-", "").ToLower();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.ToLower())
        );
    }
}