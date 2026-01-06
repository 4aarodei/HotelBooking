namespace HotelBooking.Domain.Entities.Bookings;

public class BookingStatus
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = null!;
    public Guid BookingStatusCode { get; set; }
}
