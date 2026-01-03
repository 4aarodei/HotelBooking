using HotelBooking.Models.Hotels;
using HotelBooking.ViewModels;
using HotelBooking.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers;

public class HotelController : Controller
{
    public readonly HotelService _hotelService;
    public HotelController(HotelService hotelService)
    {
        _hotelService = hotelService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? city, DateTime? checkIn, DateTime? checkOut)
    {
        var (checkInDate, checkOutDate) = ResolveDateRange(checkIn, checkOut);

        var hotels = await _hotelService.GetAvailableHotelsAsync(checkInDate, checkOutDate, city);

        ViewBag.City = string.IsNullOrWhiteSpace(city) ? "Усі міста" : city;
        ViewBag.CheckIn = checkInDate;
        ViewBag.CheckOut = checkOutDate;

        var vm = new HotelViewModelIndex().CreateVM(hotels);

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid hotelId, DateTime? checkIn, DateTime? checkOut)
    {
        var (checkInDate, checkOutDate) = ResolveDateRange(checkIn, checkOut);
        var hotel = await _hotelService.GetByIdWithAvailabilityAsync(hotelId, checkInDate, checkOutDate);

        if (hotel == null)
        {
            return NotFound();
        }

        var vm = new HotelDetailsViewModel
        {
            Id = hotel.Id,
            Name = hotel.Name,
            City = hotel.City,
            Address = hotel.Address,
            Description = hotel.Description ?? string.Empty,
            Rooms = hotel.Rooms.ToList(),
            CheckIn = checkInDate,
            CheckOut = checkOutDate
        };
        return View(vm);
    }

    private static (DateTime checkIn, DateTime checkOut) ResolveDateRange(DateTime? checkIn, DateTime? checkOut)
    {
        var resolvedCheckIn = (checkIn ?? DateTime.Today.AddDays(1)).Date;
        var resolvedCheckOut = (checkOut ?? resolvedCheckIn.AddDays(1)).Date;

        if (resolvedCheckOut <= resolvedCheckIn)
        {
            resolvedCheckOut = resolvedCheckIn.AddDays(1);
        }

        return (resolvedCheckIn, resolvedCheckOut);
    }

}

