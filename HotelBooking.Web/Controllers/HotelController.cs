using System.Globalization;
using HotelBooking.Application.Interfaces;
using HotelBooking.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Controllers;

public class HotelController : Controller
{
    private readonly IHotelService _hotelService;
    private readonly IFixedWindowRateLimiter _rateLimiter;
    private readonly IClock _clock;

    public HotelController(
        IHotelService hotelService,
        IFixedWindowRateLimiter rateLimiter,
        IClock clock)
    {
        _hotelService = hotelService;
        _rateLimiter = rateLimiter;
        _clock = clock;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? city, DateOnly? checkIn, DateOnly? checkOut, CancellationToken ct)
    {
        var rateLimit = await _rateLimiter.CheckAsync(
            $"rate-limit:hotel-search:ip:{GetClientIp()}",
            permitLimit: 120,
            window: TimeSpan.FromMinutes(1),
            ct);

        if (!rateLimit.IsAllowed)
        {
            Response.Headers["Retry-After"] = Math.Ceiling(rateLimit.RetryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            return StatusCode(StatusCodes.Status429TooManyRequests);
        }

        var (checkInDate, checkOutDate) = ResolveDateRange(checkIn, checkOut, _clock.Today);

        var hotels = await _hotelService.GetAvailableHotelsAsync(checkInDate, checkOutDate, city, ct);
        var cities = await _hotelService.GetAvailableCitiesAsync(ct);

        var vm = HotelIndexViewModel.Create(hotels, city, checkInDate, checkOutDate, cities, _clock.Today);

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid hotelId, DateOnly? checkIn, DateOnly? checkOut)
    {
        var (checkInDate, checkOutDate) = ResolveDateRange(checkIn, checkOut, _clock.Today);
        var hotel = await _hotelService.GetByIdWithAvailabilityAsync(hotelId, checkInDate, checkOutDate);

        if (hotel == null)
        {
            return NotFound();
        }

        var vm = HotelDetailsViewModel.Create(hotel, checkInDate, checkOutDate);

        return View(vm);
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

    private string GetClientIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
