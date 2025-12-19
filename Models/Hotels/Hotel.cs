namespace HotelBooking.Models.Hotels;

public class Hotel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string? Description { get; set; }

    // Готель створюється одразу з номерами
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
