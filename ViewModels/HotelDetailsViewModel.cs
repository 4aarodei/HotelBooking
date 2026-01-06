using HotelBooking.Core.EntitiesModels.Hotels;

namespace HotelBooking.ViewModels;

public class HotelDetailsViewModel
{

    public Guid Id { get; set; }
    public string Name { get; set; }
    public string City { get; set; }
    public string Address { get; set; }
    public string Description { get; set; }
    public List<Room> Rooms { get; set; } = new List<Room>();
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }

}
