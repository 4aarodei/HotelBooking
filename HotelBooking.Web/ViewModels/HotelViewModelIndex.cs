using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.ViewModels;

public class HotelViewModelIndex
{
    public List<HomeHotelDto> Hotels { get; set; } = new();

    public HotelViewModelIndex CreateVM(IEnumerable<Hotel> hotels)
    {
        Hotels = hotels.Select(h => new HomeHotelDto
        {
            Id = h.Id,
            Name = h.Name,
            City = h.City,
            Description = h.Description,
            ActiveRoomsCount = h.Rooms.Count(r => r.IsActive)
        }).ToList();

        return this;
    }
}
