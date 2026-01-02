using HotelBooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using HotelBooking.Data;
using HotelBooking.ViewModels;
using HotelBooking.Data.ApplicationDbContext;
using HotelBooking.Services;

namespace HotelBooking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HotelService _hotelService;
        public HomeController(ILogger<HomeController> logger, HotelService hotelService)
        {
            _logger = logger;
            _hotelService = hotelService;
        }
        public async Task<IActionResult> Index()
        {

            // Створити список "популярних, або готелів які "на рекламі" і передвати їх в VM
            var hotels = await _context.Hotels
                .Where(h => h.Rooms.Any(r => r.IsActive))
                .Select(h => new HotelCardVm
                {
                    Id = h.Id,
                    Name = h.Name,
                    City = h.City,
                    MinPrice = h.Rooms
                        .Where(r => r.IsActive)
                        .Min(r => r.PricePerNight)
                })
                .Take(6)
                .ToListAsync();

            var vm = new HomeViewModel
            {
                PopularHotels = hotels
            };

            return View(vm);
        }
    }
}
