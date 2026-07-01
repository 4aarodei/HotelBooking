using HotelBooking.Application.Hotels;

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

    public static HotelDetailsViewModel Create(HotelReadModel hotel, DateOnly checkIn, DateOnly checkOut)
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
                    AltText = i.AltText ?? hotel.Name,
                    Width = i.Width,
                    Height = i.Height
                })
                .ToList(),
            Rooms = hotel.Rooms
                .OrderBy(r => r.PricePerNight)
                .ThenBy(r => r.Capacity)
                .Select(r =>
                {
                    var coverImage = GetRoomCoverImage(r);
                    return new RoomDetailsViewModel
                    {
                        Id = r.Id,
                        Name = r.Name,
                        ImageUrl = coverImage?.Url,
                        ImageWidth = coverImage?.Width,
                        ImageHeight = coverImage?.Height,
                        Images = r.Images
                            .OrderByDescending(i => i.IsCover)
                            .ThenBy(i => i.SortOrder)
                            .Select(i => new ImageViewModel
                            {
                                Url = i.Url,
                                AltText = i.AltText ?? r.Name,
                                Width = i.Width,
                                Height = i.Height
                            })
                            .ToList(),
                        Capacity = r.Capacity,
                        Quantity = r.Quantity,
                        Description = r.Description ?? string.Empty,
                        IncludesBreakfast = r.IncludesBreakfast,
                        HasPrivateBathroom = r.HasPrivateBathroom,
                        HasSaunaAccess = r.HasSaunaAccess,
                        HasBalcony = r.HasBalcony,
                        HasWorkspace = r.HasWorkspace,
                        HasAirConditioning = r.HasAirConditioning,
                        PricePerNight = r.PricePerNight
                    };
                })
                .ToList(),
            CheckIn = checkIn,
            CheckOut = checkOut
        };
    }

    private static ImageReadModel? GetRoomCoverImage(RoomReadModel room)
    {
        return room.Images
                   .OrderByDescending(i => i.IsCover)
                   .ThenBy(i => i.SortOrder)
                   .FirstOrDefault();
    }
}
