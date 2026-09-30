namespace ValousWorld.Web.Services;

public class RealOtpService : IOtpService
{
    public Task<(bool success, string? error)> SendOtpAsync(string phone)
    {
        // TODO: Implement with MSG91/Fast2SMS
        throw new NotImplementedException("Real OTP service coming soon. Use Mock mode.");
    }

    public Task<(bool success, string? error)> VerifyOtpAsync(string phone, string code)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsRateLimitedAsync(string phone)
    {
        throw new NotImplementedException();
    }
}