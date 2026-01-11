using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Web.ViewModels;

public class HotelIndexViewModel
{

    public string City { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public List<HotelCardViewModel> Hotels { get; set; } = new();
    public IReadOnlyList<string> Cities { get; init; } = Array.Empty<string>();

    public string CheckInFormatted => CheckIn.ToString("yyyy-MM-dd");
    public string CheckOutFormatted => CheckOut.ToString("yyyy-MM-dd");
    public string MinCheckInDate => DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
    public string MinCheckOutDate => CheckIn.AddDays(1).ToString("yyyy-MM-dd");

    public static HotelIndexViewModel Create(IEnumerable<Hotel> hotels, string? city, DateTime checkIn, DateTime checkOut, IReadOnlyList<string> cities)
    {
        var vm = new HotelIndexViewModel
        {
            City = string.IsNullOrWhiteSpace(city) ? "Усі міста" : city!,
            CheckIn = checkIn,
            CheckOut = checkOut,
            Cities = cities
        };

        foreach (var hotel in hotels)
        {
            vm.Hotels.Add(new HotelCardViewModel
            {
                Id = hotel.Id,
                Name = hotel.Name,
                Summary = hotel.Description ?? "Опис відсутній",
                PriceText = $"від {GetMinActivePrice(hotel.Rooms)} ₴",
                ActionText = "Переглянути",
                CheckIn = checkIn.ToString("yyyy-MM-dd"),
                CheckOut = checkOut.ToString("yyyy-MM-dd")
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

        return activePrices.Any() ? activePrices.Min() : 0;
    }
}