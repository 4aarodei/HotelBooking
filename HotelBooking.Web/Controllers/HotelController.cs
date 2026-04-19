using HotelBooking.Application.Services;
using HotelBooking.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Controllers;

public class HotelController : Controller
{
    private readonly HotelService _hotelService;

    public HotelController(HotelService hotelService)
    {
        _hotelService = hotelService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? city, DateOnly? checkIn, DateOnly? checkOut)
    {
        var (checkInDate, checkOutDate) = ResolveDateRange(checkIn, checkOut);

        var hotels = await _hotelService.GetAvailableHotelsAsync(checkInDate, checkOutDate, city);
        var cities = await _hotelService.GetAvailableCitiesAsync();

        var vm = HotelIndexViewModel.Create(hotels, city, checkInDate, checkOutDate, cities);

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid hotelId, DateOnly? checkIn, DateOnly? checkOut)
    {
        var (checkInDate, checkOutDate) = ResolveDateRange(checkIn, checkOut);
        var hotel = await _hotelService.GetByIdWithAvailabilityAsync(hotelId, checkInDate, checkOutDate);

        if (hotel == null)
        {
            return NotFound();
        }

        var vm = HotelDetailsViewModel.Create(hotel, checkInDate, checkOutDate);

        return View(vm);
    }

    private static (DateOnly checkIn, DateOnly checkOut) ResolveDateRange(DateOnly? checkIn, DateOnly? checkOut)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var resolvedCheckIn = checkIn ?? today.AddDays(1);
        var resolvedCheckOut = checkOut ?? resolvedCheckIn.AddDays(1);

        if (resolvedCheckOut <= resolvedCheckIn)
        {
            resolvedCheckOut = resolvedCheckIn.AddDays(1);
        }

        return (resolvedCheckIn, resolvedCheckOut);
    }
}
