using HotelBooking.Application.Hotels;
using HotelBooking.Domain.Entities.Hotels;

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
        var room = details.Room;
        var hotel = room.Hotel;

        return new RoomPageViewModel
        {
            Id = room.Id,
            HotelId = room.HotelId,
            HotelName = hotel?.Name ?? string.Empty,
            HotelCity = hotel?.City ?? string.Empty,
            HotelAddress = hotel?.Address ?? string.Empty,
            Name = room.Name,
            Description = room.Description ?? string.Empty,
            Amenities = SplitAmenities(room.Amenities),
            Images = room.Images
                .OrderByDescending(i => i.IsCover)
                .ThenBy(i => i.SortOrder)
                .Select(i => new ImageViewModel
                {
                    Url = i.Url,
                    AltText = i.AltText ?? room.Name,
                    Width = i.Width,
                    Height = i.Height
                })
                .ToList(),
            Capacity = room.Capacity,
            Quantity = room.Quantity,
            AvailableQuantity = details.AvailableQuantity,
            PricePerNight = room.PricePerNight,
            IncludesBreakfast = room.IncludesBreakfast,
            HasPrivateBathroom = room.HasPrivateBathroom,
            HasSaunaAccess = room.HasSaunaAccess,
            HasBalcony = room.HasBalcony,
            HasWorkspace = room.HasWorkspace,
            HasAirConditioning = room.HasAirConditioning,
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
