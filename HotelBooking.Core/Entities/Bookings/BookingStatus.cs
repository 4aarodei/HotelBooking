namespace HotelBooking.Core.EntitiesModels.Bookings;

public class BookingStatus
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Код (для логіки)
    public Guid BookingStatusCode { get; set; } // CONFIRMED, CANCELLED

    // Назва (для UI)
    public string Name { get; set; } = null!;
}
