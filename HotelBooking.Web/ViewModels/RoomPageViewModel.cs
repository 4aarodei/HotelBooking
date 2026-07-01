using HotelBooking.Application.Hotels;

namespace HotelBooking.Web.ViewModels;

public class RoomPageViewModel
{
    public Guid Id { get; init; }
    public Guid HotelId { get; init; }
    public string HotelName { get; init; } = string.Empty;
    public string HotelCity { get; init; } = string.Empty;
    public string HotelAddress { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Amenities { get; init; } = [];
    public List<ImageViewModel> Images { get; init; } = [];
    public int Capacity { get; init; }
    public int Quantity { get; init; }
    public int AvailableQuantity { get; init; }
    public decimal PricePerNight { get; init; }
    public bool IncludesBreakfast { get; init; }
    public bool HasPrivateBathroom { get; init; }
    public bool HasSaunaAccess { get; init; }
    public bool HasBalcony { get; init; }
    public bool HasWorkspace { get; init; }
    public bool HasAirConditioning { get; init; }
    public DateOnly CheckIn { get; init; }
    public DateOnly CheckOut { get; init; }

    public static RoomPageViewModel Create(RoomAvailabilityDetails details, DateOnly checkIn, DateOnly checkOut)
    {
        return new RoomPageViewModel
        {
            Id = details.Id,
            HotelId = details.HotelId,
            HotelName = details.HotelName,
            HotelCity = details.HotelCity,
            HotelAddress = details.HotelAddress,
            Name = details.Name,
            Description = details.Description ?? string.Empty,
            Amenities = SplitAmenities(details.Amenities),
            Images = details.Images
                .OrderByDescending(i => i.IsCover)
                .ThenBy(i => i.SortOrder)
                .Select(i => new ImageViewModel
                {
                    Url = i.Url,
                    AltText = i.AltText ?? details.Name,
                    Width = i.Width,
                    Height = i.Height
                })
                .ToList(),
            Capacity = details.Capacity,
            Quantity = details.Quantity,
            AvailableQuantity = details.AvailableQuantity,
            PricePerNight = details.PricePerNight,
            IncludesBreakfast = details.IncludesBreakfast,
            HasPrivateBathroom = details.HasPrivateBathroom,
            HasSaunaAccess = details.HasSaunaAccess,
            HasBalcony = details.HasBalcony,
            HasWorkspace = details.HasWorkspace,
            HasAirConditioning = details.HasAirConditioning,
            CheckIn = checkIn,
            CheckOut = checkOut
        };
    }

    private static List<string> SplitAmenities(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split([',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
