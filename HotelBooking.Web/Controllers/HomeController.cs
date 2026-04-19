using System.Diagnostics;
using HotelBooking.Application.Services;
using HotelBooking.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Controllers
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

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var hotels = await _hotelService.GetFeaturedAsync(6, ct);

            var vm = new HomeViewModel
            {
                PopularHotels = hotels
                    .Where(h => h.Rooms.Any(r => r.IsActive))
                    .Select(h =>
                    {
                        var minPrice = h.Rooms.Where(r => r.IsActive).Min(r => r.PricePerNight);

                        return new HotelCardViewModel
                        {
                            Id = h.Id,
                            Name = h.Name,
                            Summary = h.City,
                            PriceText = $"від {minPrice:0} ₴",
                            ActionText = "Дивитися деталі"
                        };
                    })
                    .ToList()
            };

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
