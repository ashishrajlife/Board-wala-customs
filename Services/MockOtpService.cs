using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Helpers;
using Microsoft.Extensions.Options;
using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public class MockOtpService : IOtpService
{
    private readonly AppDbContext _db;
    private readonly OtpSettings _settings;
    private readonly ILogger<MockOtpService> _logger;

    public MockOtpService(
        AppDbContext db,
        IOptions<IntegrationSettings> settings,
        ILogger<MockOtpService> logger)
    {
        _db = db;
        _settings = settings.Value.Otp;
        _logger = logger;
    }

    public async Task<(bool success, string? error)> SendOtpAsync(string phone, string purpose = "Login")
    {
        // ============================================================
        // Check 1: Agar Login purpose hai, toh user registered hona chahiye
        // ============================================================
        if (purpose == "Login")
        {
            var userExists = await _db.Users.AnyAsync(u => u.Phone == phone);
            if (!userExists)
            {
                return (false, "PHONE_NOT_REGISTERED");
            }
        }

        // Agar Register purpose hai, toh user already exist nahi hona chahiye
        if (purpose == "Register")
        {
            var userExists = await _db.Users.AnyAsync(u => u.Phone == phone);
            if (userExists)
            {
                return (false, "PHONE_ALREADY_REGISTERED");
            }
        }

        // Baaki existing rate-limit + OTP create logic yahan
        if (await IsRateLimitedAsync(phone))
        {
            return (false, "Too many OTP requests. Please try again after 10 minutes.");
        }

        var oldOtps = await _db.OtpLogs
            .Where(o => o.Phone == phone && o.Purpose == purpose && !o.IsUsed)
            .ToListAsync();

        foreach (var o in oldOtps) o.IsUsed = true;

        var otp = new OtpLog
        {
            Phone = phone,
            OtpCode = _settings.FixedOtp,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
            CreatedAt = DateTime.UtcNow
        };

        _db.OtpLogs.Add(otp);
        await _db.SaveChangesAsync();

        _logger.LogInformation("==================================================");
        _logger.LogInformation("[MOCK OTP] Phone: {Phone}", phone);
        _logger.LogInformation("[MOCK OTP] Purpose: {Purpose}", purpose);
        _logger.LogInformation("[MOCK OTP] OTP Code: {Otp}", _settings.FixedOtp);
        _logger.LogInformation("[MOCK OTP] Expires in: {Min} minutes", _settings.ExpiryMinutes);
        _logger.LogInformation("==================================================");

        return (true, null);
    }

    public async Task<(bool success, string? error)> VerifyOtpAsync(string phone, string code, string purpose = "Login")
    {
        var otp = await _db.OtpLogs
            .Where(o => o.Phone == phone
                     && o.Purpose == purpose
                     && !o.IsUsed
                     && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp == null)
            return (false, "OTP expired or not found. Please request a new one.");

        if (otp.AttemptCount >= _settings.MaxAttempts)
        {
            otp.IsUsed = true;
            await _db.SaveChangesAsync();
            return (false, "Too many attempts. Please request a new OTP.");
        }

        otp.AttemptCount++;

        if (otp.OtpCode != code)
        {
            await _db.SaveChangesAsync();
            return (false, "Invalid OTP.");
        }

        otp.IsUsed = true;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<bool> IsRateLimitedAsync(string phone)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-10);
        var count = await _db.OtpLogs
            .CountAsync(o => o.Phone == phone && o.CreatedAt > cutoff);

        return count >= _settings.MaxRequestsPer10Min;
    }
}