using HotelBooking.Application.Common;
using HotelBooking.Application.Hotels;
using HotelBooking.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Controllers;

[Route("rooms")]
public class RoomsController : Controller
{
    private readonly IHotelService _hotelService;
    private readonly IClock _clock;

    public RoomsController(IHotelService hotelService, IClock clock)
    {
        _hotelService = hotelService;
        _clock = clock;
    }

    [HttpGet("{roomId:guid}")]
    public async Task<IActionResult> Details(Guid roomId, DateOnly? checkIn, DateOnly? checkOut)
    {
        var (checkInDate, checkOutDate) = ResolveDateRange(checkIn, checkOut, _clock.Today);
        var roomDetails = await _hotelService.GetRoomByIdWithAvailabilityAsync(roomId, checkInDate, checkOutDate);

        if (roomDetails == null)
        {
            return NotFound();
        }

        var vm = RoomPageViewModel.Create(roomDetails, checkInDate, checkOutDate);

        return View("~/Views/Rooms/Details.cshtml", vm);
    }

    private static (DateOnly checkIn, DateOnly checkOut) ResolveDateRange(DateOnly? checkIn, DateOnly? checkOut, DateOnly today)
    {
        var resolvedCheckIn = checkIn ?? today.AddDays(1);
        var resolvedCheckOut = checkOut ?? resolvedCheckIn.AddDays(1);

        if (resolvedCheckOut <= resolvedCheckIn)
        {
            resolvedCheckOut = resolvedCheckIn.AddDays(1);
        }

        return (resolvedCheckIn, resolvedCheckOut);
    }
}
