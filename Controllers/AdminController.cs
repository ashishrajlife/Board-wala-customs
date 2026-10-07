using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValousWorld.Web.Data;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Dashboard()
    {
        ViewBag.UserCount = await _db.Users.CountAsync();
        ViewBag.ProductCount = await _db.Products.CountAsync();
        ViewBag.CategoryCount = await _db.Categories.CountAsync();
        ViewBag.NewArrivals = await _db.Products.CountAsync(p => p.IsNewArrival);
         ViewBag.AnnouncementCount = await _db.Announcements.CountAsync(a => a.IsActive);
                 // Order counters
        ViewBag.OrderCount = await _db.Orders.CountAsync();
        ViewBag.OrderConfirmed = await _db.Orders.CountAsync(o => o.Status == "Confirmed");
        ViewBag.OrderShipped = await _db.Orders.CountAsync(o => o.Status == "Shipped");
        ViewBag.OrderDelivered = await _db.Orders.CountAsync(o => o.Status == "Delivered");
        ViewBag.OrderCancelled = await _db.Orders.CountAsync(o => o.Status == "Cancelled");
        ViewBag.CodPending = await _db.Orders.CountAsync(o =>
            o.PaymentMethod == "COD" &&
            o.PaymentStatus == "CodPending" &&
            o.Status != "Cancelled" &&
            o.Status != "Delivered");
          return View("~/Views/Admin/Dashboard.cshtml");
    }
}