using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Web.ViewModels;

public class HotelDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ImageViewModel> Images { get; set; } = new();
    public List<RoomDetailsViewModel> Rooms { get; set; } = new();
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
            Images = hotel.Images
                .OrderByDescending(i => i.IsCover)
                .ThenBy(i => i.SortOrder)
                .Select(i => new ImageViewModel
                {
                    Url = i.Url,
                    AltText = i.AltText ?? hotel.Name
                })
                .ToList(),
            Rooms = hotel.Rooms
                .OrderBy(r => r.PricePerNight)
                .ThenBy(r => r.Capacity)
                .Select(r => new RoomDetailsViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    ImageUrl = GetRoomCoverUrl(r),
                    Images = r.Images
                        .OrderByDescending(i => i.IsCover)
                        .ThenBy(i => i.SortOrder)
                        .Select(i => new ImageViewModel
                        {
                            Url = i.Url,
                            AltText = i.AltText ?? r.Name
                        })
                        .ToList(),
                    Capacity = r.Capacity,
                    Quantity = r.Quantity,
                    PricePerNight = r.PricePerNight
                })
                .ToList(),
            CheckIn = checkIn,
            CheckOut = checkOut
        };
    }

    private static string? GetRoomCoverUrl(Room room)
    {
        return room.Images
                   .OrderByDescending(i => i.IsCover)
                   .ThenBy(i => i.SortOrder)
                   .Select(i => i.Url)
                   .FirstOrDefault();
    }
}
