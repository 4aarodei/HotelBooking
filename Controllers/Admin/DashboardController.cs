using HotelBooking.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin")]
public class DashboardController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        var model = new AdminDashboardViewModel
        {
            CanManageRoles = User.IsInRole("SuperAdmin")
        };

        return View("~/Views/Admin/Dashboard/Index.cshtml", model);
    }
}
