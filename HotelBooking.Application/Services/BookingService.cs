using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Bookings;

namespace HotelBooking.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IClock _clock;

    public BookingService(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        IClock clock)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _clock = clock;
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

        return booking;
    }

    public Task<List<Booking>> GetBookingsByUserAsync(string userId, CancellationToken ct = default) =>
        _bookingRepository.GetByUserAsync(userId, ct);
}
