using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using ValousWorld.Web.Data;
using ValousWorld.Web.Models.Entities;
using ValousWorld.Web.Services;

namespace ValousWorld.Web.Controllers;

[AllowAnonymous]
[Route("api/webhook")]
public class WebhookController : Controller
{
    private readonly IConfiguration _config;
    private readonly ILogger<WebhookController> _logger;
    private readonly IOrderService _orders;
    private readonly IPaymentFinalizer _finalizer;
    private readonly AppDbContext _db;

    public WebhookController(IConfiguration config, ILogger<WebhookController> logger,
        IOrderService orders, IPaymentFinalizer finalizer, AppDbContext db)
    {
        _config = config; _logger = logger; _orders = orders; _finalizer = finalizer; _db = db;
    }

    [HttpPost, Route("razorpay")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Razorpay()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        var signature = Request.Headers["X-Razorpay-Signature"].ToString();
        var secret = _config["Integrations:Razorpay:WebhookSecret"];

        // Fail closed: never accept unsigned webhooks
        if (string.IsNullOrWhiteSpace(secret) || secret.StartsWith("SET_THIS"))
        {
            _logger.LogError("[WEBHOOK] WebhookSecret not configured");
            return StatusCode(500);
        }

        if (string.IsNullOrEmpty(signature) || !VerifySignature(body, signature, secret))
        {
            _logger.LogWarning("[WEBHOOK] Invalid signature - rejected");
            return Unauthorized();
        }

        var eventId = Request.Headers["X-Razorpay-Event-Id"].ToString();
        if (string.IsNullOrEmpty(eventId))
            eventId = "body-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)));

        // Deduplicate
        var evt = await _db.WebhookEvents.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (evt != null && (evt.Status == "Processed" || evt.Status == "Unmatched" || evt.Status == "Flagged"))
            return Ok();

        if (evt == null)
        {
            evt = new WebhookEvent { EventId = eventId, Payload = body, Status = "Received" };
            _db.WebhookEvents.Add(evt);
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException) { return Ok(); } // concurrent duplicate
        }

        var evtId = evt.Id;

        try
        {
            var json = JObject.Parse(body);
            var eventType = json["event"]?.ToString() ?? "";
            var payment = json["payload"]?["payment"]?["entity"];

            evt.EventType = eventType;
            evt.RazorpayOrderId = payment?["order_id"]?.ToString();
            evt.RazorpayPaymentId = payment?["id"]?.ToString();
            evt.Attempts++;

            var outcome = eventType switch
            {
                "payment.captured" => await HandleCaptured(payment),
                "payment.failed" => await HandleFailed(payment),
                "refund.processed" => await HandleRefund(json),
                _ => "Ignored"
            };

            evt.Status = outcome == "Ignored" ? "Processed" : outcome;
            evt.Error = (outcome == "Unmatched" || outcome == "Flagged") ? outcome : null;
            evt.ProcessedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WEBHOOK] Processing failed for event {EventId}", eventId);

            _db.ChangeTracker.Clear();
            var e2 = await _db.WebhookEvents.FirstAsync(x => x.Id == evtId);
            e2.Status = "Failed";
            e2.Attempts++;
            e2.Error = ex.Message.Length > 900 ? ex.Message[..900] : ex.Message;
            await _db.SaveChangesAsync();

            return StatusCode(500); // Razorpay will retry
        }
    }

    // ---------------- HANDLERS ----------------

    private async Task<string> HandleCaptured(JToken? payment)
    {
        if (payment == null) return "Ignored";

        var rzpOrderId = payment["order_id"]?.ToString();
        var rzpPaymentId = payment["id"]?.ToString();
        if (string.IsNullOrEmpty(rzpPaymentId)) return "Ignored";

        var amount = (payment["amount"]?.Value<decimal>() ?? 0) / 100m;
        var currency = payment["currency"]?.ToString();

        var order = await FindOrderAsync(payment, rzpOrderId);
        if (order == null)
        {
            _logger.LogError("[WEBHOOK] UNMATCHED captured payment {PayId} (rzp order {RzpOrderId}) - customer charged, no order!",
                rzpPaymentId, rzpOrderId);
            return "Unmatched";
        }

        if (currency != "INR")
        {
            _logger.LogError("[WEBHOOK] Non-INR payment {PayId} for {Order}", rzpPaymentId, order.OrderNumber);
            return "Flagged";
        }

        var result = await _finalizer.FinalizeAsync(order.OrderId, rzpPaymentId, "", amount, "webhook");

        if (result == FinalizeResult.AmountMismatch) return "Flagged";

        if (result == FinalizeResult.AlreadyPaid && order.RazorpayPaymentId != rzpPaymentId)
        {
            _logger.LogError("[WEBHOOK] DUPLICATE PAYMENT {PayId} for already-paid {Order} - refund it from dashboard",
                rzpPaymentId, order.OrderNumber);
            return "Flagged";
        }

        return "Processed";
    }

    private async Task<string> HandleFailed(JToken? payment)
    {
        if (payment == null) return "Ignored";

        var order = await FindOrderAsync(payment, payment["order_id"]?.ToString());
        if (order == null || order.PaymentStatus == PaymentStatus.Paid) return "Ignored";

        // Only log. The user may still retry inside the popup, so do not mark the order Failed here.
        await _orders.LogPaymentAttemptAsync(order, "Failed",
            payment["error_code"]?.ToString(), payment["error_description"]?.ToString());

        return "Processed";
    }

    private async Task<string> HandleRefund(JObject json)
    {
        var refund = json["payload"]?["refund"]?["entity"];
        if (refund == null) return "Ignored";

        var paymentId = refund["payment_id"]?.ToString();
        var refundId = refund["id"]?.ToString();
        var amount = (refund["amount"]?.Value<decimal>() ?? 0) / 100m;

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.RazorpayPaymentId == paymentId);
        if (order == null) return "Unmatched";
        if (order.RefundId == refundId) return "Processed"; // already applied

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
        return "Processed";
    }

    // Finds by the current Razorpay order id, then falls back to notes.internal_order_id
    // (covers payments made on an older Razorpay order after a Retry)
    private async Task<Order?> FindOrderAsync(JToken payment, string? rzpOrderId)
    {
        Order? order = null;
        if (!string.IsNullOrEmpty(rzpOrderId))
            order = await _orders.GetOrderByRazorpayIdAsync(rzpOrderId);

        if (order == null && payment["notes"] is JObject notes &&
            int.TryParse(notes["internal_order_id"]?.ToString(), out var internalId))
        {
            order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == internalId);
        }
        return order;
    }

    private static bool VerifySignature(string body, string signature, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var expected = BitConverter.ToString(hash).Replace("-", "").ToLower();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.ToLower()));
    }

        // ============================================================
    // DELHIVERY TRACKING WEBHOOK
    // ============================================================
    [HttpPost, Route("delhivery")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Delhivery()
    {
        string raw;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            raw = await reader.ReadToEndAsync();

        _logger.LogInformation("[WEBHOOK/DELHIVERY] Received: {Body}", raw);

        // Persist for audit
        try
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
            var evtId = "dhv-" + hash[..32];

            var existing = await _db.WebhookEvents.FirstOrDefaultAsync(e => e.EventId == evtId);
            if (existing != null) return Ok();

            _db.WebhookEvents.Add(new WebhookEvent
            {
                EventId = evtId,
                EventType = "Delhivery.ShipmentUpdate",
                Payload = raw,
                Status = "Received"
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "[WEBHOOK/DELHIVERY] Save failed"); }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            var root = doc.RootElement;

            string? waybill = null;
            string? status = null;

            if (root.TryGetProperty("Shipment", out var sh))
            {
                if (sh.TryGetProperty("AWB", out var awb)) waybill = awb.GetString();
                if (sh.TryGetProperty("Status", out var st) &&
                    st.TryGetProperty("Status", out var stStr))
                    status = stStr.GetString();
            }
            else if (root.TryGetProperty("AWB", out var awb2))
            {
                waybill = awb2.GetString();
                if (root.TryGetProperty("Status", out var st2))
                    status = st2.ValueKind == System.Text.Json.JsonValueKind.String
                        ? st2.GetString()
                        : st2.TryGetProperty("Status", out var ss) ? ss.GetString() : null;
            }

            if (string.IsNullOrWhiteSpace(waybill) || string.IsNullOrWhiteSpace(status))
            {
                _logger.LogWarning("[WEBHOOK/DELHIVERY] Missing waybill/status.");
                return Ok(new { received = true });
            }

            var (ok, err) = await _orders.SyncShipmentStatusAsync(waybill, status);
            if (!ok) _logger.LogWarning("[WEBHOOK/DELHIVERY] Sync failed: {Err}", err);

            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WEBHOOK/DELHIVERY] Parse failed");
            return Ok(new { received = true }); // always 200 so Delhivery doesn't retry-storm
        }
    }

}