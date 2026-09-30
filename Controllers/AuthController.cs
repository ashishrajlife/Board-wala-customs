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

public class AuthController : Controller
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IOtpService _otp;

    public AuthController(AppDbContext db, ITokenService tokenService, IOtpService otp)
    {
        _db = db;
        _tokenService = tokenService;
        _otp = otp;
    }

    // ============================================================
    // LOGIN (Password) — UNCHANGED
    // ============================================================
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null || !user.IsActive || user.Password != model.Password)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        await SignInUserAsync(user);

        return user.Role!.RoleName == "Admin"
            ? RedirectToAction("Dashboard", "Admin")
            : RedirectToAction("Index", "Home");
    }

    // ============================================================
    // REGISTER — Step 1: Form (with Send OTP button)
    // ============================================================
    [HttpGet]
    public IActionResult Register() => View();

    // Called by register form — sends OTP for phone verification
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendRegisterOtp(RegisterViewModel model)
    {
        // Validate name/email/phone/password all present
        if (string.IsNullOrWhiteSpace(model.FullName) ||
            string.IsNullOrWhiteSpace(model.Email) ||
            string.IsNullOrWhiteSpace(model.Phone) ||
            string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError("", "All fields are required.");
            return View("Register", model);
        }

        if (model.Password != model.ConfirmPassword)
        {
            ModelState.AddModelError("", "Passwords do not match.");
            return View("Register", model);
        }

        // Check duplicates
        if (await _db.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError("", "Email already registered.");
            return View("Register", model);
        }

        if (await _db.Users.AnyAsync(u => u.Phone == model.Phone))
        {
            ModelState.AddModelError("", "Phone number already registered.");
            return View("Register", model);
        }

        // Send OTP
        var (success, error) = await _otp.SendOtpAsync(model.Phone, "Register");

        if (!success)
        {
            ModelState.AddModelError("", error ?? "Failed to send OTP.");
            return View("Register", model);
        }

        // Save form data in TempData (so OTP page can use it)
        TempData["Reg_FullName"] = model.FullName;
        TempData["Reg_Email"] = model.Email;
        TempData["Reg_Phone"] = model.Phone;
        TempData["Reg_Password"] = model.Password;

        return RedirectToAction(nameof(VerifyRegisterOtp));
    }

    // ============================================================
    // REGISTER — Step 2: OTP verification
    // ============================================================
    [HttpGet]
    public IActionResult VerifyRegisterOtp()
    {
        var phone = TempData["Reg_Phone"] as string;
        if (string.IsNullOrEmpty(phone))
            return RedirectToAction(nameof(Register));

        TempData.Keep("Reg_FullName");
        TempData.Keep("Reg_Email");
        TempData.Keep("Reg_Phone");
        TempData.Keep("Reg_Password");

        ViewBag.Phone = phone;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyRegisterOtp(string otp)
    {
        var phone = TempData["Reg_Phone"] as string;
        var fullName = TempData["Reg_FullName"] as string;
        var email = TempData["Reg_Email"] as string;
        var password = TempData["Reg_Password"] as string;

        if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(email))
            return RedirectToAction(nameof(Register));

        TempData.Keep("Reg_FullName");
        TempData.Keep("Reg_Email");
        TempData.Keep("Reg_Phone");
        TempData.Keep("Reg_Password");

        var (success, error) = await _otp.VerifyOtpAsync(phone, otp, "Register");

        if (!success)
        {
            ModelState.AddModelError("", error ?? "Invalid OTP.");
            ViewBag.Phone = phone;
            return View();
        }

        // Create user
        var userRole = await _db.Roles.FirstAsync(r => r.RoleName == "User");

        var user = new User
        {
            FullName = fullName!,
            Email = email,
            Phone = phone,
            Password = password,     // plain text as per your choice
            RoleId = userRole.RoleId,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Reload with Role
        user = await _db.Users.Include(u => u.Role)
            .FirstAsync(u => u.UserId == user.UserId);

        // Auto-login
        await SignInUserAsync(user);

        TempData["Success"] = "Account created successfully!";
        return RedirectToAction("Index", "Home");
    }

    // ============================================================
    // LOGOUT
    // ============================================================
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token");
        return RedirectToAction("Login", "Auth");
    }

    // ============================================================
    // HELPER — Sign in user (cookie + JWT + refresh token)
    // ============================================================
    private async Task SignInUserAsync(User user)
    {
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

        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}