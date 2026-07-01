using HotelBooking.Application.Caching;
using HotelBooking.Application.Common;
using HotelBooking.Application.Persistence;
using HotelBooking.Domain.Entities.Bookings;
using HotelBooking.Domain.Entities.Hotels;
using HotelBooking.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Bookings;

public sealed class CreateBookingUseCase : ICreateBookingUseCase
{
    private static readonly TimeSpan AvailabilityVersionTtl = TimeSpan.FromDays(365);

    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IClock _clock;
    private readonly IAppCache _cache;
    private readonly ILogger<CreateBookingUseCase> _logger;

    public CreateBookingUseCase(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        IClock clock,
        IAppCache cache,
        ILogger<CreateBookingUseCase> logger)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _clock = clock;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Booking> ExecuteAsync(string userId, Guid roomId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default)
    {
        var room = await _roomRepository.GetByIdAsync(roomId, ct)
                   ?? throw new BookingRuleViolationException("Room was not found.");

        var booking = CreateBooking(userId, room, checkIn, checkOut);

        var created = await _bookingRepository.TryAddIfAvailableAsync(booking, room.Quantity, ct);
        if (!created)
        {
            throw new BookingRuleViolationException("Room is already fully booked for these dates.");
        }

        await BumpAvailabilityVersionAsync(ct);
        return booking;
    }

    private Booking CreateBooking(string userId, Room room, DateOnly checkIn, DateOnly checkOut)
    {
        try
        {
            var dateRange = DateRange.Create(checkIn, checkOut);
            return Booking.Create(userId, room, dateRange, _clock.UtcNow);
        }
        catch (DomainRuleViolationException ex)
        {
            throw new BookingRuleViolationException(ex.Message);
        }
    }

    private async Task BumpAvailabilityVersionAsync(CancellationToken ct)
    {
        try
        {
            await _cache.SetAsync(
                HotelCacheKeys.AvailabilityVersion,
                Guid.NewGuid().ToString("N"),
                AvailabilityVersionTtl,
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to bump hotel availability cache version.");
        }
    }
}
