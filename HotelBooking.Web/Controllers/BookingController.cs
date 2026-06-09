using System.Security.Claims;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Identity;
using HotelBooking.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Controllers;

[Authorize]
[Authorize(Roles = AppRoles.User)]
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly IFixedWindowRateLimiter _rateLimiter;
    private readonly IClock _clock;

    public BookingController(
        IBookingService bookingService,
        IFixedWindowRateLimiter rateLimiter,
        IClock clock)
    {
        _bookingService = bookingService;
        _rateLimiter = rateLimiter;
        _clock = clock;
    }

    [HttpGet]
    public IActionResult Create(Guid roomId, DateOnly? checkIn, DateOnly? checkOut)
    {
        var today = _clock.Today;
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

        var rateLimit = await _rateLimiter.CheckAsync(
            $"rate-limit:booking-create:user:{userId}",
            permitLimit: 5,
            window: TimeSpan.FromMinutes(10),
            ct);

        if (!rateLimit.IsAllowed)
        {
            ModelState.AddModelError(
                string.Empty,
                "Too many booking attempts. Please wait a few minutes and try again.");
            return View(request);
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
