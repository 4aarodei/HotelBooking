namespace HotelBooking.Models.Bookings;

public class BookingStatus
{
    public int Id { get; set; }

    // TECH код (для логіки)
    public string Code { get; set; } = null!; // CONFIRMED, CANCELLED

    // Людська назва (для UI)
    public string Name { get; set; } = null!;
}
