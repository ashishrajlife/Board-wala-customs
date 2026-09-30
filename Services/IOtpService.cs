namespace ValousWorld.Web.Services;

public interface IOtpService
{
    Task<(bool success, string? error)> SendOtpAsync(string phone);
    Task<(bool success, string? error)> VerifyOtpAsync(string phone, string code);
    Task<bool> IsRateLimitedAsync(string phone);
}