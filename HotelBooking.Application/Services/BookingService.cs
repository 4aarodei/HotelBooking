using HotelBooking.Application.Caching;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Bookings;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Services;

public class BookingService : IBookingService
{
    private static readonly TimeSpan AvailabilityVersionTtl = TimeSpan.FromDays(365);

    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IClock _clock;
    private readonly IAppCache _cache;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        IClock clock,
        IAppCache cache,
        ILogger<BookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _clock = clock;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Booking> CreateBookingAsync(string userId, Guid roomId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default)
    {
        if (checkOut <= checkIn)
        {
            throw new BookingRuleViolationException("Check-out date must be later than check-in date.");
        }

        var room = await _roomRepository.GetByIdAsync(roomId, ct)
                   ?? throw new BookingRuleViolationException("Room was not found.");

        if (!room.IsActive)
        {
            throw new BookingRuleViolationException("Room is not available for booking.");
        }

        if (room.Quantity <= 0)
        {
            throw new BookingRuleViolationException("Room is already fully booked for these dates.");
        }

        var nights = checkOut.DayNumber - checkIn.DayNumber;

        var booking = new Booking
        {
            UserId = userId,
            RoomId = roomId,
            Status = BookingStatus.Pending,
            CheckIn = checkIn,
            CheckOut = checkOut,
            PricePerNightSnapshot = room.PricePerNight,
            Nights = nights,
            TotalPrice = nights * room.PricePerNight,
            CreatedAtUtc = _clock.UtcNow
        };

        var created = await _bookingRepository.TryAddIfAvailableAsync(booking, room.Quantity, ct);
        if (!created)
        {
            throw new BookingRuleViolationException("Room is already fully booked for these dates.");
        }

        await BumpAvailabilityVersionAsync(ct);
        return booking;
    }

    public Task<List<Booking>> GetBookingsByUserAsync(string userId, CancellationToken ct = default) =>
        _bookingRepository.GetByUserAsync(userId, ct);

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
