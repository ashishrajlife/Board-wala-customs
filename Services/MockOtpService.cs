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
        // Purpose based checks
        // ============================================================
        if (purpose == "Login")
        {
            var userExists = await _db.Users.AnyAsync(u => u.Phone == phone);
            if (!userExists)
                return (false, "PHONE_NOT_REGISTERED");
        }
        else if (purpose == "Register")
        {
            var userExists = await _db.Users.AnyAsync(u => u.Phone == phone);
            if (userExists)
                return (false, "PHONE_ALREADY_REGISTERED");
        }
        else if (purpose == "ProfileUpdate")
        {
            var phoneTaken = await _db.Users.AnyAsync(u => u.Phone == phone);
            if (phoneTaken)
                return (false, "This phone number is already registered to another account.");
        }

        // ============================================================
        // FIX 1: Rate Limit ko bypass karo sirf ProfileUpdate ke liye
        // (Taaki user baar baar profile change karte waqt block na ho)
        // ============================================================
        if (purpose != "ProfileUpdate" && await IsRateLimitedAsync(phone))
        {
            return (false, "Too many OTP requests. Please try again after 10 minutes.");
        }

        // Purane unused OTPs ko expire kar do
        var oldOtps = await _db.OtpLogs
            .Where(o => o.Phone == phone && o.Purpose == purpose && !o.IsUsed)
            .ToListAsync();

        foreach (var o in oldOtps) o.IsUsed = true;

        // Naya OTP create karo
        var otp = new OtpLog
        {
            Phone = phone,
            OtpCode = _settings.FixedOtp, // Demo ke liye 1234
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
        _logger.LogInformation("==================================================");

        return (true, null);
    }

    public async Task<(bool success, string? error)> VerifyOtpAsync(string phone, string code, string purpose = "Login")
    {
        // ============================================================
        // FIX 2: Agar purpose ProfileUpdate hai, toh sidha 1234 check karo
        // Database me dhundhne ki zaroorat nahi. Login/Register waisa hi chalega.
        // ============================================================
        if (purpose == "ProfileUpdate")
        {
            if (code == "1234" || code == _settings.FixedOtp)
                return (true, null);
                
            return (false, "Invalid OTP. Please enter 1234 for demo.");
        }

        // --- Login / Register ke liye purana Database wala logic ---
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
            return (false, "Invalid OTP. Please try again.");
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