using System.Security.Claims;
using HotelBooking.Application.Services;
using HotelBooking.Domain.Entities.Identity;
using HotelBooking.ViewModels.UserProfileViewModels;
using HotelBooking.Web.ViewModels.UserProfileViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Controllers
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

        public async Task<IActionResult> Bookings(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var bookings = await _bookingService.GetBookingsByUserAsync(userId, ct);

            var viewModel = bookings
                .Select(booking => new UserBookingItemViewModel
                {
                    HotelName = booking.Room?.Hotel?.Name ?? string.Empty,
                    RoomName = booking.Room?.Name ?? string.Empty,
                    CheckIn = booking.CheckIn,
                    CheckOut = booking.CheckOut,
                    Nights = booking.Nights,
                    TotalPrice = booking.TotalPrice,
                    StatusName = booking.Status.ToString()
                })
                .ToList();

            return View(viewModel);
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
