using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ValousWorld.Web.Helpers;
using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public class RealDelhiveryService : IDeliveryService
{
    private readonly HttpClient _http;
    private readonly DeliverySettings _cfg;
    private readonly ILogger<RealDelhiveryService> _logger;

    public RealDelhiveryService(
        HttpClient http,
        IOptions<IntegrationSettings> settings,
        ILogger<RealDelhiveryService> logger)
    {
        _http = http;
        _cfg = settings.Value.Delivery;
        _logger = logger;

        _http.BaseAddress = new Uri(_cfg.BaseUrl.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Token", _cfg.ApiToken);
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    // ============================================================
    // SERVICEABILITY
    // ============================================================
    public async Task<(bool success, bool serviceable, bool codAvailable, string? error)> CheckServiceabilityAsync(string pincode)
    {
        if (string.IsNullOrWhiteSpace(pincode) || pincode.Length != 6)
            return (true, false, false, null);

        try
        {
            var url = $"c/api/pin-codes/json/?filter_codes={pincode}";
            using var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("[DELHIVERY] Serviceability HTTP {Code} for {Pin}", resp.StatusCode, pincode);
                return (false, false, false, $"HTTP {resp.StatusCode}");
            }

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("delivery_codes", out var codes) ||
                codes.ValueKind != JsonValueKind.Array ||
                codes.GetArrayLength() == 0)
            {
                return (true, false, false, null);
            }

            var pc = codes[0].GetProperty("postal_code");
            var serviceable = GetBoolish(pc, "pre_paid") || GetBoolish(pc, "cod") || pc.TryGetProperty("pin", out _);
            var cod = GetBoolish(pc, "cod");

            _logger.LogInformation("[DELHIVERY] Serviceability {Pin} → svc={Svc}, cod={Cod}",
                pincode, serviceable, cod);

            return (true, serviceable, cod, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DELHIVERY] Serviceability exception for {Pin}", pincode);
            return (false, false, false, ex.Message);
        }
    }

    // ============================================================
    // CREATE SHIPMENT (MANIFEST)
    // ============================================================
    public async Task<(bool success, string? courier, string? tracking, string? error)> CreateShipmentAsync(Order order)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_cfg.ClientName) ||
                string.IsNullOrWhiteSpace(_cfg.PickupLocation))
            {
                return (false, null, null,
                    "Delhivery ClientName / PickupLocation not configured.");
            }

            var isCod = order.PaymentMethod == PaymentMethods.Cod;
            var totalWeight = _cfg.DefaultWeightGrams + 100; // +packaging
            if (order.Items != null && order.Items.Any())
                totalWeight = (order.Items.Sum(i => i.Quantity) * _cfg.DefaultWeightGrams) + 100;

            var shipment = new
            {
                name = Sanitize(order.ShippingFullName, 100),
                add = Sanitize($"{order.ShippingLine1} {order.ShippingLine2}".Trim(), 250),
                pin = order.ShippingPincode,
                city = Sanitize(order.ShippingCity, 100),
                state = Sanitize(order.ShippingState, 100),
                country = Sanitize(order.ShippingCountry, 50),
                phone = Sanitize(order.ShippingPhone, 15),
                order = order.OrderNumber,
                payment_mode = isCod ? "COD" : "Prepaid",
                return_pin = _cfg.PickupPincode,
                return_city = "",
                return_phone = "9999999999",
                return_add = "",
                return_state = "",
                return_country = "India",
                products_desc = Sanitize(string.Join(",", order.Items?.Select(i => i.ProductName) ?? Array.Empty<string>()), 250),
                hsn_code = "",
                cod_amount = isCod ? order.Total : 0m,
                order_date = order.PlacedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                total_amount = order.Total,
                seller_add = "",
                seller_name = "",
                seller_inv = order.OrderNumber,
                quantity = order.Items?.Sum(i => i.Quantity) ?? 1,
                waybill = "",
                shipment_width = _cfg.DefaultWidthCm,
                shipment_height = _cfg.DefaultHeightCm,
                weight = totalWeight,
                shipment_length = _cfg.DefaultLengthCm,
                seller_gst_tin = "",
                shipping_mode = "Surface",
                address_type = "home"
            };

            var payload = new
            {
                shipments = new[] { shipment },
                pickup_location = new { name = _cfg.PickupLocation }
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["format"] = "json",
                ["data"] = payloadJson
            });

            using var resp = await _http.PostAsync("api/cmu/create.json", form);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("[DELHIVERY] Create HTTP {Code}: {Body}", resp.StatusCode, body);
                return (false, null, null, $"HTTP {resp.StatusCode}: {Truncate(body, 200)}");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var success = root.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True;

            if (root.TryGetProperty("packages", out var pkgs) &&
                pkgs.ValueKind == JsonValueKind.Array &&
                pkgs.GetArrayLength() > 0)
            {
                var pkg = pkgs[0];
                var waybill = pkg.TryGetProperty("waybill", out var w) ? w.GetString() : null;

                if (success && !string.IsNullOrWhiteSpace(waybill))
                {
                    _logger.LogInformation("[DELHIVERY] Manifested {OrderNumber} → waybill {AWB}",
                        order.OrderNumber, waybill);
                    return (true, "Delhivery", waybill, null);
                }

                var remark = pkg.TryGetProperty("remarks", out var r) ? r.ToString() : "manifest failed";
                _logger.LogError("[DELHIVERY] Manifest rejected {OrderNumber}: {Remark}",
                    order.OrderNumber, remark);
                return (false, null, null, Truncate(remark, 250));
            }

            _logger.LogError("[DELHIVERY] Manifest response unexpected: {Body}", Truncate(body, 300));
            return (false, null, null, "Unexpected Delhivery response.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DELHIVERY] Create exception for {OrderNumber}", order.OrderNumber);
            return (false, null, null, ex.Message);
        }
    }

    // ============================================================
    // CANCEL
    // ============================================================
    public async Task<(bool success, string? error)> CancelShipmentAsync(string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return (true, null);

        try
        {
            var body = new { waybill = trackingNumber, cancellation = "true" };
            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.PostAsync("api/p/edit", content);
            var respBody = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("[DELHIVERY] Cancel HTTP {Code} for {AWB}: {Body}",
                    resp.StatusCode, trackingNumber, respBody);
                return (false, $"HTTP {resp.StatusCode}");
            }

            using var doc = JsonDocument.Parse(respBody);
            var status = doc.RootElement.TryGetProperty("status", out var st) ? st.GetString() : null;

            if (string.Equals(status, "true", StringComparison.OrdinalIgnoreCase) ||
                (doc.RootElement.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True))
            {
                _logger.LogInformation("[DELHIVERY] Cancelled {AWB}", trackingNumber);
                return (true, null);
            }

            var msg = doc.RootElement.TryGetProperty("remark", out var r) ? r.GetString() : respBody;
            return (false, Truncate(msg, 200));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DELHIVERY] Cancel exception for {AWB}", trackingNumber);
            return (false, ex.Message);
        }
    }

    // ============================================================
    // FETCH STATUS
    // ============================================================
    public async Task<(bool success, string? status, string? error)> FetchStatusAsync(string waybill)
    {
        if (string.IsNullOrWhiteSpace(waybill))
            return (false, null, "Waybill missing.");

        try
        {
            var url = $"api/v1/packages/json/?waybill={waybill}";
            using var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (false, null, $"HTTP {resp.StatusCode}");

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("ShipmentData", out var shipments) &&
                shipments.ValueKind == JsonValueKind.Array &&
                shipments.GetArrayLength() > 0)
            {
                var first = shipments[0];
                if (first.TryGetProperty("Shipment", out var sh) &&
                    sh.TryGetProperty("Status", out var st) &&
                    st.TryGetProperty("Status", out var stStr))
                {
                    return (true, stStr.GetString(), null);
                }
            }

            return (true, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DELHIVERY] FetchStatus exception for {AWB}", waybill);
            return (false, null, ex.Message);
        }
    }

    // ============================================================
    // Helpers
    // ============================================================
    private static string Sanitize(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var cleaned = s.Replace("&", "and").Replace("#", "").Replace("%", "").Replace(";", "");
        return cleaned.Length <= max ? cleaned : cleaned[..max];
    }

    private static string Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max));

    private static bool GetBoolish(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var el)) return false;
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString()?.Equals("Y", StringComparison.OrdinalIgnoreCase) == true,
            JsonValueKind.True => true,
            _ => false
        };
    }
}