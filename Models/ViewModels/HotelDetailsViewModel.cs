using HotelBooking.Models.Hotels;

namespace HotelBooking.Models.ViewModels;

public class HotelDetailsViewModel
{

    public Guid Id { get; set; }
    public string Name { get; set; }
    public string City { get; set; }
    public string Address { get; set; }
    public string Description { get; set; }
    public List<Room> Rooms { get; set; } = new List<Room>();
   
}
