using System.Net;
using System.Security.Claims;
using HotelBooking.Application.Bookings;
using HotelBooking.Application.Common;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.RateLimiting;
using HotelBooking.Domain.Entities.Bookings;
using HotelBooking.Domain.Entities.Hotels;
using HotelBooking.Web.Controllers;
using HotelBooking.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HotelBooking.Tests.Controllers;

public class RateLimitedControllerTests
{
    [Fact]
    public async Task HotelSearch_Returns429_WhenRateLimitExceeded()
    {
        var controller = new HotelController(
            new FakeHotelService(),
            new FakeRateLimiter(new RateLimitResult(false, 121, TimeSpan.FromSeconds(17))),
            new FakeClock());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Connection =
                {
                    RemoteIpAddress = IPAddress.Parse("127.0.0.1")
                }
            }
        };

        var result = await controller.Index(null, null, null, CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status429TooManyRequests, status.StatusCode);
        Assert.Equal("17", controller.Response.Headers.RetryAfter);
    }

    [Fact]
    public async Task BookingPost_AddsModelError_WhenRateLimitExceeded()
    {
        var bookingService = new FakeBookingService();
        var controller = CreateBookingController(
            bookingService,
            new FakeRateLimiter(new RateLimitResult(false, 6, TimeSpan.FromMinutes(10))));
        var request = new CreateBookingRequest
        {
            RoomId = Guid.NewGuid(),
            CheckIn = new DateOnly(2026, 5, 1),
            CheckOut = new DateOnly(2026, 5, 2)
        };

        var result = await controller.Create(request, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(0, bookingService.CreateCalls);
    }

    [Fact]
    public async Task BookingPost_StillUsesBookingService_WhenRateLimiterAllows()
    {
        var bookingService = new FakeBookingService();
        var controller = CreateBookingController(
            bookingService,
            new FakeRateLimiter(RateLimitResult.Allowed(1)));
        var request = new CreateBookingRequest
        {
            RoomId = Guid.NewGuid(),
            CheckIn = new DateOnly(2026, 5, 1),
            CheckOut = new DateOnly(2026, 5, 2)
        };

        var result = await controller.Create(request, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(1, bookingService.CreateCalls);
    }

    private static BookingController CreateBookingController(
        FakeBookingService bookingService,
        IFixedWindowRateLimiter rateLimiter)
    {
        var controller = new BookingController(bookingService, rateLimiter, new FakeClock());
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user-1"), new Claim(ClaimTypes.Role, "User")],
            "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user
            }
        };

        return controller;
    }

    private sealed class FakeRateLimiter : IFixedWindowRateLimiter
    {
        private readonly RateLimitResult _result;

        public FakeRateLimiter(RateLimitResult result)
        {
            _result = result;
        }

        public Task<RateLimitResult> CheckAsync(
            string keyPrefix,
            int permitLimit,
            TimeSpan window,
            CancellationToken ct = default)
        {
            return Task.FromResult(_result);
        }
    }

    private sealed class FakeHotelService : IHotelService
    {
        public Task<List<string>> GetAvailableCitiesAsync(CancellationToken ct = default)
            => Task.FromResult(new List<string>());

        public Task<List<HotelReadModel>> GetAvailableHotelsAsync(
            DateOnly checkIn,
            DateOnly checkOut,
            string? city,
            CancellationToken ct = default)
            => Task.FromResult(new List<HotelReadModel>());

        public Task<HotelReadModel?> GetByIdWithAvailabilityAsync(
            Guid id,
            DateOnly checkIn,
            DateOnly checkOut,
            CancellationToken ct = default)
            => Task.FromResult<HotelReadModel?>(null);

        public Task<RoomAvailabilityDetails?> GetRoomByIdWithAvailabilityAsync(
            Guid roomId,
            DateOnly checkIn,
            DateOnly checkOut,
            CancellationToken ct = default)
            => Task.FromResult<RoomAvailabilityDetails?>(null);

        public Task<List<HotelReadModel>> GetFeaturedAsync(int count, CancellationToken ct = default)
            => Task.FromResult(new List<HotelReadModel>());
    }

    private sealed class FakeBookingService : IBookingService
    {
        public int CreateCalls { get; private set; }

        public Task<Booking> CreateBookingAsync(
            string userId,
            Guid roomId,
            DateOnly checkIn,
            DateOnly checkOut,
            CancellationToken ct = default)
        {
            CreateCalls++;
            var room = Room.Create(
                roomId,
                Guid.NewGuid(),
                "Standard",
                null,
                null,
                2,
                100m,
                1,
                includesBreakfast: false,
                hasPrivateBathroom: true,
                hasSaunaAccess: false,
                hasBalcony: false,
                hasWorkspace: false,
                hasAirConditioning: false,
                isActive: true);

            return Task.FromResult(Booking.CreatePending(userId, room, checkIn, checkOut, DateTimeOffset.UtcNow));
        }

        public Task<List<Booking>> GetBookingsByUserAsync(string userId, CancellationToken ct = default)
            => Task.FromResult(new List<Booking>());
    }

    private sealed class FakeClock : IClock
    {
        public DateOnly Today { get; } = new(2026, 4, 30);
        public DateTimeOffset UtcNow { get; } = new(2026, 4, 30, 12, 0, 0, TimeSpan.Zero);
    }
}
