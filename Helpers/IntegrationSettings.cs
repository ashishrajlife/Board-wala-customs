namespace ValousWorld.Web.Helpers;

public class IntegrationSettings
{
    public RazorpaySettings Razorpay { get; set; } = new();
    public IntegrationMode WhatsApp { get; set; } = new();
    public DeliverySettings Delivery { get; set; } = new();
    public OtpSettings Otp { get; set; } = new();

    // Legacy shape retained for WhatsApp + any other IntegrationMode usages
    public class IntegrationMode
    {
        public string Mode { get; set; } = "Mock";  // Mock | Live
        public string ApiKey { get; set; } = "";
        public string PhoneNumberId { get; set; } = "";
    }
}

public class RazorpaySettings
{
    public string Mode { get; set; } = "Mock";   // Mock | Live
    public string KeyId { get; set; } = "";
    public string KeySecret { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
}

public class DeliverySettings
{
    public string Mode { get; set; } = "Mock";   // Mock | Staging | Live
    public string ApiToken { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string PickupLocation { get; set; } = "";
    public string PickupPincode { get; set; } = "492001";

    public string StagingBaseUrl { get; set; } = "https://staging-express.delhivery.com";
    public string LiveBaseUrl    { get; set; } = "https://track.delhivery.com";

    public int DefaultWeightGrams { get; set; } = 500;
    public int DefaultLengthCm    { get; set; } = 30;
    public int DefaultWidthCm     { get; set; } = 20;
    public int DefaultHeightCm    { get; set; } = 10;

    public bool IsMock    => string.Equals(Mode, "Mock", StringComparison.OrdinalIgnoreCase);
    public bool IsLive    => string.Equals(Mode, "Live", StringComparison.OrdinalIgnoreCase);
    public bool IsStaging => string.Equals(Mode, "Staging", StringComparison.OrdinalIgnoreCase);
    public string BaseUrl => IsLive ? LiveBaseUrl : StagingBaseUrl;
}

public class OtpSettings
{
    public string Mode { get; set; } = "Mock";
    public string FixedOtp { get; set; } = "1234";
    public int ExpiryMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
    public int MaxRequestsPer10Min { get; set; } = 3;
}