using HotelBooking.Core.EntitiesModels.Identity;
using HotelBooking.Core.Services;
using HotelBooking.ViewModels.UserProfileViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBooking.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        private readonly BookingService _bookingService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;


        public UserProfileController(BookingService bookingService, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _bookingService = bookingService;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var vm = new UserProfileIndexModel
            {
                Email = user.Email,
                FirstName = user.FirstName ?? "Імʼя не вказано",
                LastName = user.LastName ?? "Прізвище не вказано"
            };

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePublicProfile(UserProfileIndexModel newUserData)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Будь ласка, виправте помилки у формі.";
                return View("Index", newUserData);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = newUserData.FirstName;
            user.LastName = newUserData.LastName;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Профіль успішно оновлено.";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            TempData["Error"] = "Не вдалося оновити профіль.";
            return View("Index", newUserData);
        }



        public IActionResult Bookings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // тут потім буде БД
            // var bookings = _bookingService.GetByUser(userId);

            return View(/* bookings */);
        }

        public IActionResult Security()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

    }
}
