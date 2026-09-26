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

    public AuthController(AppDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null || !user.IsActive || user.Password != model.Password)   // ⚠️ PLAIN COMPARE
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // Generate JWT for API + issue cookie for MVC session
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

        // Sign in cookie principal for MVC views
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role!.RoleName)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return user.Role.RoleName == "Admin"
        ? RedirectToAction("Dashboard", "Admin")
        : RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _db.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError("", "Email already registered.");
            return View(model);
        }

        var userRole = await _db.Roles.FirstAsync(r => r.RoleName == "User");

        _db.Users.Add(new User
        {
            FullName = model.FullName,
            Email = model.Email,
            Phone = model.Phone,
            Password = model.Password,
            RoleId = userRole.RoleId
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Registration successful. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token");
        return RedirectToAction("Login", "Auth");
    }
}