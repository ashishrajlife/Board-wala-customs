using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ValousWorld.Web.Data;
using ValousWorld.Web.Helpers;
using ValousWorld.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ValousWorld.Web.Helpers.IntegrationSettings>(
    builder.Configuration.GetSection("Integrations"));

// Read integrations config
var integrations = builder.Configuration.GetSection("Integrations").Get<ValousWorld.Web.Helpers.IntegrationSettings>() ?? new();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));

// JWT settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(opt =>
{
    opt.MultipartBodyLengthLimit = 150 * 1024 * 1024;
});

// Auth: both Cookie (for MVC) and JWT (for API)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, opt =>
{
    opt.LoginPath = "/Auth/Login";
    opt.AccessDeniedPath = "/Auth/Login";
    opt.ExpireTimeSpan = TimeSpan.FromHours(1);
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
    };
});

// Razorpay
if (integrations.Razorpay.Mode == "Live")
    builder.Services.AddScoped<IRazorpayService, RealRazorpayService>();
else
    builder.Services.AddScoped<IRazorpayService, MockRazorpayService>();

// WhatsApp
if (integrations.WhatsApp.Mode == "Live")
    builder.Services.AddScoped<IWhatsAppService, MockWhatsAppService>();  // replace with real
else
    builder.Services.AddScoped<IWhatsAppService, MockWhatsAppService>();

// Delivery
if (integrations.Delivery.Mode == "Live")
    builder.Services.AddScoped<IDeliveryService, MockDeliveryService>();  // replace with real
else
    builder.Services.AddScoped<IDeliveryService, MockDeliveryService>();

// OTP Service
if (integrations.Otp.Mode == "Live")
    builder.Services.AddScoped<IOtpService, RealOtpService>();
else
    builder.Services.AddScoped<IOtpService, MockOtpService>();

builder.Services.AddAuthorization();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddHostedService<OrderExpiryBackgroundService>();
builder.Services.AddScoped<IPaymentFinalizer, PaymentFinalizer>();
builder.Services.AddHostedService<PaymentReconciliationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();