namespace ValousWorld.Web.Services;

public class RealOtpService : IOtpService
{
    public Task<(bool success, string? error)> SendOtpAsync(string phone, string purpose = "Login")
    {
        // TODO: Implement with MSG91/Fast2SMS when ready
        throw new NotImplementedException("Real OTP service coming soon. Please use Mock mode in appsettings.json.");
    }

    public Task<(bool success, string? error)> VerifyOtpAsync(string phone, string code, string purpose = "Login")
    {
        throw new NotImplementedException("Real OTP service coming soon. Please use Mock mode in appsettings.json.");
    }

    public Task<bool> IsRateLimitedAsync(string phone)
    {
        throw new NotImplementedException("Real OTP service coming soon.");
    }
}