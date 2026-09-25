using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ValousWorld.Web.Controllers;

[Authorize(Roles = "User,Admin")]
public class UserController : Controller
{
    public IActionResult Dashboard() => View();
}