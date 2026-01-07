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
    public static HotelDetailsViewModel Create(Hotel hotel, DateTime checkIn, DateTime checkOut)
    {
        return new HotelDetailsViewModel
        {
            Id = hotel.Id,
            Name = hotel.Name,
            City = hotel.City,
            Address = hotel.Address,
            Description = hotel.Description ?? string.Empty,
            Rooms = hotel.Rooms.ToList(),
            CheckIn = checkIn,
            CheckOut = checkOut
        };
    }
}
