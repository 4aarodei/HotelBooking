using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Web.ViewModels;

public class HotelDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Room> Rooms { get; set; } = new();
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }

    public static HotelDetailsViewModel Create(Hotel hotel, DateOnly checkIn, DateOnly checkOut)
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
