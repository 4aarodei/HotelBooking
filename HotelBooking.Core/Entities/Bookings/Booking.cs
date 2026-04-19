using HotelBooking.Domain.Entities.Hotels;
using HotelBooking.Domain.Entities.Identity;

namespace HotelBooking.Domain.Entities.Bookings;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public Guid RoomId { get; set; }
    public Room? Room { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }

    public decimal PricePerNightSnapshot { get; set; }
    public int Nights { get; set; }
    public decimal TotalPrice { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
