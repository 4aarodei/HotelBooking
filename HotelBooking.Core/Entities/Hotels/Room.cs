namespace HotelBooking.Domain.Entities.Hotels;

public class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid HotelId { get; set; }
    public Hotel? Hotel { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Amenities { get; set; }
    public int Capacity { get; set; }
    public decimal PricePerNight { get; set; }

    public int Quantity { get; set; } = 1;

    public bool IncludesBreakfast { get; set; }
    public bool HasPrivateBathroom { get; set; } = true;
    public bool HasSaunaAccess { get; set; }
    public bool HasBalcony { get; set; }
    public bool HasWorkspace { get; set; }
    public bool HasAirConditioning { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<RoomImage> Images { get; set; } = new List<RoomImage>();
}
