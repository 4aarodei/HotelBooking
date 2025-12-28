using HotelBooking.Models.Hotels;
using HotelBooking.Models.ViewModels;
using HotelBooking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBooking.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly BookingService _bookingService;

    public BookingController(BookingService bookingService)
    {
        _bookingService = bookingService;
    }

    // GET: /Booking/Create?roomId=...
    [HttpGet]
    public IActionResult Create(Guid roomId)
    {
        var model = new CreateBookingRequest
        {
            RoomId = roomId,
            CheckIn = DateTime.Today,
            CheckOut = DateTime.Today.AddDays(1)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(request);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        try
        {
            await _bookingService.CreateAsync(
                userId,
                request.RoomId,
                request.CheckIn,
                request.CheckOut,
                ct);
        }
        catch (InvalidOperationException ex)
        {
            // Ось це і є "гарна помилка" для UI
            ModelState.AddModelError(string.Empty, ex.Message);

            // Повертаємо ту саму сторінку, без падіння
            return View(request);
        }

        return RedirectToAction("Index", "Home");
    }
}
