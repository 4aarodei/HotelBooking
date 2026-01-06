using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.ViewModels;

public class HotelDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public List<Room> Rooms { get; set; } = new();
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
}
