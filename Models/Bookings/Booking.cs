using HotelBooking.Models.Bookings;
using HotelBooking.Models.Hotels;
using HotelBooking.Models.Identity;

namespace HotelBooking.Models.Bookings;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public int StatusId { get; set; }
    public BookingStatus Status { get; set; } = null!;

    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }

    // Фіксація ціни на момент бронювання
    public decimal PricePerNightSnapshot { get; set; }
    public int Nights { get; set; }
    public decimal TotalPrice { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
