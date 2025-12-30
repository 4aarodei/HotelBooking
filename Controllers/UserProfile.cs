using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers
{
    public class UserProfileController : Controller
    {

        public async Task<IActionResult> Index(string UserId, string tab = "public")
        {
            return View();
        }


    }
}
