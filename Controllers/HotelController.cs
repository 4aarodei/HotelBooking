using HotelBooking.Models.Hotels;
using HotelBooking.Models.ViewModels;
using HotelBooking.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers;

public class HotelController : Controller
{
    public readonly IHotelService _hotelService;
    public HotelController(IHotelService hotelService)
    {
        _hotelService = hotelService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? city)
    {
        var hotels = new List<Hotel>();

        if (string.IsNullOrEmpty(city))
        {
            // в цей блок переходимо якщо параметр city не переданий
            hotels = await _hotelService.GetAllAsync();
        }
        else
        {
            hotels = await _hotelService.GetByCityAsync(city);
        }
        var VM = new HotelViewModelIndex().CreateVM(hotels);

        return View(VM);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid hotelId)
    {
        var Hotel = await _hotelService.GetByIdAsync(hotelId);
        
        if (Hotel == null)
        {
            return NotFound();
        }
        
        var VM = new HotelDetailsViewModel
        {
            Id = Hotel.Id,
            Name = Hotel.Name,
            City = Hotel.City,
            Address = Hotel.Address,
            Description = Hotel.Description ?? string.Empty,
            Rooms = Hotel.Rooms.ToList()
        };
        return View(VM);
    }

}

