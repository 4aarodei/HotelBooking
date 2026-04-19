using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Bookings;

namespace HotelBooking.Application.Services;

public class BookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;

    public BookingService(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
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

        var bookingsByRoom = await _bookingRepository.GetOverlappingActiveBookingsCountByRoomAsync(
            new[] { roomId },
            checkIn,
            checkOut,
            ct);

        if (bookingsByRoom.GetValueOrDefault(roomId, 0) >= room.Quantity)
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
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await _bookingRepository.AddAsync(booking, ct);

        return booking;
    }

    public Task<List<Booking>> GetBookingsByUserAsync(string userId, CancellationToken ct = default) =>
        _bookingRepository.GetByUserAsync(userId, ct);
}
