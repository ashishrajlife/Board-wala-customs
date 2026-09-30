namespace ValousWorld.Web.Services;

public interface IOtpService
{
    Task<(bool success, string? error)> SendOtpAsync(string phone, string purpose = "Login");
    Task<(bool success, string? error)> VerifyOtpAsync(string phone, string code, string purpose = "Login");
    Task<bool> IsRateLimitedAsync(string phone);
}