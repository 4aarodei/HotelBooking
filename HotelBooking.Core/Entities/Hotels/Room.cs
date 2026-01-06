namespace HotelBooking.Domain.Entities.Hotels;

public class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid HotelId { get; set; }
    public Hotel Hotel { get; set; } = null!;

    public string Name { get; set; } = null!;
    public int Capacity { get; set; }
    public decimal PricePerNight { get; set; }

    public int Quantity { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}
