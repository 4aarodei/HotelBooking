using HotelBooking.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Areas.Admin.Controllers;

public class DashboardController : AdminControllerBase
{
    [HttpGet]
    public IActionResult Index()
    {
        var model = new AdminDashboardViewModel
        {
            CanManageRoles = User.IsInRole("SuperAdmin")
        };

        return View(model);
    }
}
