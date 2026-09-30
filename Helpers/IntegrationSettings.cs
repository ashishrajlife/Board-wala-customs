namespace ValousWorld.Web.Helpers;

public class IntegrationSettings
{
    public IntegrationMode Razorpay { get; set; } = new();
    public IntegrationMode WhatsApp { get; set; } = new();
    public IntegrationMode Delivery { get; set; } = new();
     public OtpSettings Otp { get; set; } = new(); 

    public class IntegrationMode
    {
        public string Mode { get; set; } = "Mock";  // Mock | Live
        public string KeyId { get; set; } = "";
        public string KeySecret { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string PhoneNumberId { get; set; } = "";
        public string PickupPincode { get; set; } = "492001";
    }
}

public class OtpSettings
{
    public string Mode { get; set; } = "Mock";     // Mock | Live
    public string FixedOtp { get; set; } = "1234"; // Mock mode me ye OTP
    public int ExpiryMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
    public int MaxRequestsPer10Min { get; set; } = 3;
}