using HotelBooking.Domain.Entities.Hotels;
using HotelBooking.Domain.Exceptions;

namespace HotelBooking.Domain.Entities.Bookings;

public class Booking
{
    private Booking()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string UserId { get; private set; } = string.Empty;

    public Guid RoomId { get; private set; }
    public Room? Room { get; private set; }

    public BookingStatus Status { get; private set; } = BookingStatus.Pending;

    public DateOnly CheckIn { get; private set; }
    public DateOnly CheckOut { get; private set; }

    public decimal PricePerNightSnapshot { get; private set; }
    public int Nights { get; private set; }
    public decimal TotalPrice { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public static Booking Create(
        string userId,
        Room room,
        DateRange dateRange,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainRuleViolationException("User is required to create a booking.");
        }

        room.EnsureCanBeBooked();

        if (room.PricePerNight <= 0)
        {
            throw new DomainRuleViolationException("Room price must be greater than zero.");
        }

        return new Booking
        {
            UserId = userId,
            RoomId = room.Id,
            Status = BookingStatus.Pending,
            CheckIn = dateRange.CheckIn,
            CheckOut = dateRange.CheckOut,
            PricePerNightSnapshot = room.PricePerNight,
            Nights = dateRange.Nights,
            TotalPrice = dateRange.Nights * room.PricePerNight,
            CreatedAtUtc = createdAtUtc
        };
    }

    public static Booking CreatePending(
        string userId,
        Room room,
        DateOnly checkIn,
        DateOnly checkOut,
        DateTimeOffset createdAtUtc)
    {
        return Create(userId, room, DateRange.Create(checkIn, checkOut), createdAtUtc);
    }
}
