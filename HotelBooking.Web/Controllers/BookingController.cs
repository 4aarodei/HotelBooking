using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Services;
using HotelBooking.Domain.Entities.Identity;
using HotelBooking.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBooking.Web.Controllers;

[Authorize]
[Authorize(Roles = AppRoles.User)]
public class BookingController : Controller
{
    private readonly BookingService _bookingService;

    public BookingController(BookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet]
    public IActionResult Create(Guid roomId, DateOnly? checkIn, DateOnly? checkOut)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var resolvedCheckIn = checkIn ?? today.AddDays(1);
        var resolvedCheckOut = checkOut ?? resolvedCheckIn.AddDays(1);

        if (resolvedCheckOut <= resolvedCheckIn)
        {
            resolvedCheckOut = resolvedCheckIn.AddDays(1);
        }

        var model = new CreateBookingRequest
        {
            RoomId = roomId,
            CheckIn = resolvedCheckIn,
            CheckOut = resolvedCheckOut
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            await _bookingService.CreateBookingAsync(
                userId,
                request.RoomId,
                request.CheckIn,
                request.CheckOut,
                ct);
        }
        catch (BookingRuleViolationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(request);
        }

        return RedirectToAction("Index", "Home");
    }
}
