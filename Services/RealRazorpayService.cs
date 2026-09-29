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

            var options = new Dictionary<string, object>
            {
                { "amount", Convert.ToInt32(Math.Ceiling(order.Total * 100)) },
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
                _logger.LogWarning("[RAZORPAY] Verify called with missing params");
                return Task.FromResult<(bool, string?, string?)>(
                    (false, null, "Payment parameters missing."));
            }

            // Razorpay official algorithm:
            //   HMAC-SHA256( order_id + "|" + payment_id , key_secret )
            var payload = $"{razorpayOrderId}|{razorpayPaymentId}";
            var secret = _settings.Razorpay.KeySecret;
            var expectedSignature = ComputeHmacSha256(payload, secret);

            var isValid = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(signature)
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

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}