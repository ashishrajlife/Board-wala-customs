using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValousWorld.Web.Services;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "User")]
[Route("checkout/voucher")]
public class VoucherController : Controller
{
    private readonly IVoucherService _vouchers;

    public VoucherController(IVoucherService vouchers) => _vouchers = vouchers;

    [HttpPost("validate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Validate(string? code)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var result = await _vouchers.ValidateAsync(userId, code);
        return Json(new
        {
            result.IsValid,
            result.Message,
            result.DiscountAmount
        });
    }
}