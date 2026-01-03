using HotelBooking.Models.Hotels;
namespace HotelBooking.ViewModels;

public class HotelViewModelIndex
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string City { get; set; }
    public decimal Price { get; set; }
    public string ShortDescription { get; set; }

    public List<HotelViewModelIndex> CreateVM(List<Hotel> hotels)
    {
        var result = new List<HotelViewModelIndex>();

        foreach (var hotel in hotels)
        {
            var vm = new HotelViewModelIndex
            {
                Id = hotel.Id,
                Name = hotel.Name,
                City = hotel.City,
                ShortDescription = (hotel.Description ?? string.Empty).Length > 100
                    ? (hotel.Description ?? string.Empty).Substring(0, 100) + "..."
                    : hotel.Description ?? string.Empty,
                Price = hotel.Rooms.Min(r => r.PricePerNight),
            };
            result.Add(vm);
        }

        return result;
    }
}


//public Guid Id { get; set; } = Guid.NewGuid();

//   public string Name { get; set; } = null!;
//   public string City { get; set; } = null!;
//   public string Address { get; set; } = null!;
//   public string? Description { get; set; }

//   // Готель створюється одразу з номерами
//   public ICollection<Room> Rooms { get; set; } = new List<Room>();