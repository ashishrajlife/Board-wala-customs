using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Razorpay.Api;
using ValousWorld.Web.Helpers;

namespace ValousWorld.Web.Services;

public class RealRazorpayService : IRazorpayService
{
    private readonly IntegrationSettings _settings;
    private readonly ILogger<RealRazorpayService> _logger;

    public RealRazorpayService(
        IOptions<IntegrationSettings> settings,
        ILogger<RealRazorpayService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<(bool success, string? razorpayOrderId, string? error)> CreateOrderAsync(
        ValousWorld.Web.Models.Entities.Order order)
    {
        try
        {
            var client = new RazorpayClient(_settings.Razorpay.KeyId, _settings.Razorpay.KeySecret);

            var amountInPaise = (int)Math.Round(order.Total * 100, MidpointRounding.AwayFromZero);

            var options = new Dictionary<string, object>
            {
                { "amount", amountInPaise },
                { "currency", "INR" },
                { "receipt", order.OrderNumber },
                { "notes", new Dictionary<string, string>
                    {
                        { "internal_order_id", order.OrderId.ToString() },
                        { "customer", order.ShippingFullName }
                    }
                }
            };

            Razorpay.Api.Order rzpOrder = client.Order.Create(options);
            var orderId = rzpOrder["id"].ToString();

            // _logger.LogInformation("[RAZORPAY] Order created: {OrderNumber} → {RzpOrderId} (₹{Total} = {Paise}p)",
            //     order.OrderNumber, orderId, order.Total, amountInPaise);

            return Task.FromResult<(bool, string?, string?)>((true, orderId, null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RAZORPAY] CreateOrder failed for {OrderNumber}", order.OrderNumber);
            return Task.FromResult<(bool, string?, string?)>((false, null, ex.Message));
        }
    }

    public Task<(bool success, string? paymentId, string? error)> VerifyPaymentAsync(
        string razorpayOrderId, string razorpayPaymentId, string signature)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(razorpayOrderId) ||
                string.IsNullOrWhiteSpace(razorpayPaymentId) ||
                string.IsNullOrWhiteSpace(signature))
            {
                return Task.FromResult<(bool, string?, string?)>(
                    (false, null, "Payment parameters missing."));
            }

            var payload = $"{razorpayOrderId}|{razorpayPaymentId}";
            var expectedSignature = ComputeHmacSha256(payload, _settings.Razorpay.KeySecret);

            var isValid = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(signature.ToLower())
            );

            if (!isValid)
            {
                _logger.LogWarning("[RAZORPAY] Signature mismatch. Expected={Expected}, Got={Got}",
                    expectedSignature, signature);
                return Task.FromResult<(bool, string?, string?)>(
                    (false, null, "Signature verification failed."));
            }

            _logger.LogInformation("[RAZORPAY] Payment verified: {PaymentId}", razorpayPaymentId);
            return Task.FromResult<(bool, string?, string?)>((true, razorpayPaymentId, null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RAZORPAY] Signature verification failed");
            return Task.FromResult<(bool, string?, string?)>((false, null, ex.Message));
        }
    }

    public Task<(bool success, string? status, decimal? amount, string? error)> FetchPaymentStatusAsync(
        string razorpayPaymentId)
    {
        try
        {
            var client = new RazorpayClient(_settings.Razorpay.KeyId, _settings.Razorpay.KeySecret);
            var payment = client.Payment.Fetch(razorpayPaymentId);

            var status = payment["status"]?.ToString();
            var amount = payment["amount"] != null ? Convert.ToDecimal(payment["amount"]) / 100m : 0m;

            return Task.FromResult<(bool, string?, decimal?, string?)>((true, status, amount, null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RAZORPAY] FetchPaymentStatus failed for {PaymentId}", razorpayPaymentId);
            return Task.FromResult<(bool, string?, decimal?, string?)>((false, null, null, ex.Message));
        }
    }

    public Task<(bool success, string? refundId, string? error)> CreateRefundAsync(
        string razorpayPaymentId, decimal amount, string? notes = null)
    {
        try
        {
            var client = new RazorpayClient(_settings.Razorpay.KeyId, _settings.Razorpay.KeySecret);

            var options = new Dictionary<string, object>
            {
                { "amount", (int)Math.Round(amount * 100, MidpointRounding.AwayFromZero) },
                { "notes", new Dictionary<string, string>
                    {
                        { "reason", notes ?? "Admin initiated refund" }
                    }
                }
            };

            var refund = client.Payment.Fetch(razorpayPaymentId).Refund(options);
            var refundId = refund["id"].ToString();

            // _logger.LogInformation("[RAZORPAY] Refund created for {PaymentId}: {RefundId}",
            //     razorpayPaymentId, refundId);

            return Task.FromResult<(bool, string?, string?)>((true, refundId, null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RAZORPAY] Refund failed for {PaymentId}", razorpayPaymentId);
            return Task.FromResult<(bool, string?, string?)>((false, null, ex.Message));
        }
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}