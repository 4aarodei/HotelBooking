using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Web.ViewModels;

public class HotelIndexViewModel
{
    public string CityName { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public List<HotelIndexItemViewModel> Hotels { get; set; } = new();

    public static HotelIndexViewModel Create(IEnumerable<Hotel> hotels, string? city, DateTime checkIn, DateTime checkOut)
    {
        var vm = new HotelIndexViewModel
        {
            CityName = string.IsNullOrWhiteSpace(city) ? "”с≥ м≥ста" : city!,
            CheckIn = checkIn,
            CheckOut = checkOut
        };

        foreach (var hotel in hotels)
        {
            vm.Hotels.Add(new HotelIndexItemViewModel
            {
                Id = hotel.Id,
                Name = hotel.Name,
                City = hotel.City,
                Description = hotel.Description ?? "ќпис в≥дсутн≥й",
                LowestPrice = GetMinActivePrice(hotel.Rooms)
            });
        }

        return vm;
    }

    private static decimal GetMinActivePrice(IEnumerable<Room> rooms)
    {
        var activePrices = rooms
            .Where(r => r.IsActive)
            .Select(r => r.PricePerNight)
            .ToList();

        return activePrices.Min();
    }
}

public class HotelIndexItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal LowestPrice { get; set; }
}