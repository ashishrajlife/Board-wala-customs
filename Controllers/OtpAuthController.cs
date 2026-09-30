using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;
using ValousWorld.Web.Models.Entities;
using ValousWorld.Web.Models.ViewModels;
using ValousWorld.Web.Services;

namespace ValousWorld.Web.Controllers;

[Route("auth/otp-login")]
public class OtpAuthController : Controller
{
    private readonly AppDbContext _db;
    private readonly IOtpService _otp;
    private readonly ITokenService _tokenService;
    private readonly ILogger<OtpAuthController> _logger;

    public OtpAuthController(
        AppDbContext db,
        IOtpService otp,
        ITokenService tokenService,
        ILogger<OtpAuthController> logger)
    {
        _db = db;
        _otp = otp;
        _tokenService = tokenService;
        _logger = logger;
    }

    // GET /auth/otp-login
    [HttpGet("")]
    public IActionResult Request(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View("~/Views/Auth/OtpRequest.cshtml", new OtpLoginViewModel());
    }

    // POST /auth/otp-login/send
    [HttpPost("send")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(OtpLoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Auth/OtpRequest.cshtml", model);

        var (success, error) = await _otp.SendOtpAsync(model.Phone);

        if (!success)
        {
            ModelState.AddModelError("", error ?? "Failed to send OTP.");
            return View("~/Views/Auth/OtpRequest.cshtml", model);
        }

        TempData["Phone"] = model.Phone;
        TempData["ReturnUrl"] = model.ReturnUrl;

        return RedirectToAction(nameof(Verify));
    }

    // GET /auth/otp-login/verify
    [HttpGet("verify")]
    public IActionResult Verify()
    {
        var phone = TempData["Phone"] as string;
        if (string.IsNullOrEmpty(phone))
            return RedirectToAction(nameof(Request));

        TempData.Keep("Phone");
        TempData.Keep("ReturnUrl");

        return View("~/Views/Auth/OtpVerify.cshtml", new OtpVerifyViewModel
        {
            Phone = phone,
            ReturnUrl = TempData["ReturnUrl"] as string
        });
    }

    // POST /auth/otp-login/verify
    [HttpPost("verify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(OtpVerifyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData.Keep("Phone");
            TempData.Keep("ReturnUrl");
            return View("~/Views/Auth/OtpVerify.cshtml", model);
        }

        var (success, error) = await _otp.VerifyOtpAsync(model.Phone, model.Otp);

        if (!success)
        {
            ModelState.AddModelError("", error ?? "Invalid OTP.");
            TempData.Keep("Phone");
            TempData.Keep("ReturnUrl");
            return View("~/Views/Auth/OtpVerify.cshtml", model);
        }

        // Find or create user
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Phone == model.Phone);

        if (user == null)
        {
            // Auto-register
            var userRole = await _db.Roles.FirstAsync(r => r.RoleName == "User");

            user = new User
            {
                FullName = $"User {model.Phone.Substring(6)}",   // e.g. "User 3210"
                Phone = model.Phone,
                Email = null,           // No email for OTP users
                Password = null,    // No password for OTP users
                RoleId = userRole.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Reload with role
            user = await _db.Users.Include(u => u.Role)
                .FirstAsync(u => u.UserId == user.UserId);

            _logger.LogInformation("[OTP] Auto-registered user: {Phone}", model.Phone);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError("", "Your account is inactive. Please contact support.");
            return View("~/Views/Auth/OtpVerify.cshtml", model);
        }

        // Issue JWT + cookie (same as password login)
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.UserId,
            Token = refreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(7)
        });
        await _db.SaveChangesAsync();

        Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddHours(1)
        });
        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        });

      var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName ?? ""),
            new(ClaimTypes.Role, user.Role?.RoleName ?? "User")
        };

        // Email sirf tab add karo jab ho
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        // Phone add karo (OTP users ke liye)
        if (!string.IsNullOrWhiteSpace(user.Phone))
        {
            claims.Add(new Claim("phone", user.Phone));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        _logger.LogInformation("[OTP] Login success: {Phone}", model.Phone);

        // Redirect based on role
        if (user.Role?.RoleName == "Admin")
            return RedirectToAction("Dashboard", "Admin");

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }
}