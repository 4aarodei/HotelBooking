using System.Globalization;
using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Web.ViewModels;

public class HotelIndexViewModel
{
    public string City { get; set; } = string.Empty;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public List<HotelCardViewModel> Hotels { get; set; } = new();
    public IReadOnlyList<string> Cities { get; init; } = Array.Empty<string>();

    public string CheckInFormatted => CheckIn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public string CheckOutFormatted => CheckOut.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public string MinCheckInDate { get; init; } = string.Empty;
    public string MinCheckOutDate => CheckIn.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static HotelIndexViewModel Create(IEnumerable<Hotel> hotels, string? city, DateOnly checkIn, DateOnly checkOut, IReadOnlyList<string> cities, DateOnly today)
    {
        var vm = new HotelIndexViewModel
        {
            City = string.IsNullOrWhiteSpace(city) ? "All cities" : city,
            CheckIn = checkIn,
            CheckOut = checkOut,
            Cities = cities,
            MinCheckInDate = today.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };

        foreach (var hotel in hotels)
        {
            vm.Hotels.Add(new HotelCardViewModel
            {
                Id = hotel.Id,
                Name = hotel.Name,
                Summary = hotel.Description ?? "Description is not available yet.",
                PriceText = $"from {GetMinActivePrice(hotel.Rooms):0} UAH",
                ActionText = "View details",
                ImageUrl = GetHotelCoverUrl(hotel),
                CheckIn = checkIn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                CheckOut = checkOut.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
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

        return activePrices.Count > 0 ? activePrices.Min() : 0;
    }

    private static string? GetHotelCoverUrl(Hotel hotel)
    {
        return hotel.Images
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.SortOrder)
            .Select(i => i.Url)
            .FirstOrDefault();
    }
}
