using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Application.Hotels;

public sealed record HotelReadSnapshot(
    Guid Id,
    string Name,
    string City,
    string Address,
    string? Description,
    IReadOnlyList<HotelImageSnapshot> Images,
    IReadOnlyList<RoomReadSnapshot> Rooms)
{
    public static HotelReadSnapshot FromHotel(Hotel hotel)
    {
        return new HotelReadSnapshot(
            hotel.Id,
            hotel.Name,
            hotel.City,
            hotel.Address,
            hotel.Description,
            hotel.Images.Select(HotelImageSnapshot.FromImage).ToList(),
            hotel.Rooms.Select(RoomReadSnapshot.FromRoom).ToList());
    }

    public Hotel ToHotel()
    {
        return new Hotel
        {
            Id = Id,
            Name = Name,
            City = City,
            Address = Address,
            Description = Description,
            Images = Images.Select(i => i.ToImage(Id)).ToList(),
            Rooms = Rooms.Select(r => r.ToRoom(Id)).ToList()
        };
    }
}

public sealed record RoomReadSnapshot(
    Guid Id,
    string Name,
    string? Description,
    string? Amenities,
    int Capacity,
    decimal PricePerNight,
    int Quantity,
    bool IncludesBreakfast,
    bool HasPrivateBathroom,
    bool HasSaunaAccess,
    bool HasBalcony,
    bool HasWorkspace,
    bool HasAirConditioning,
    bool IsActive,
    IReadOnlyList<RoomImageSnapshot> Images)
{
    public static RoomReadSnapshot FromRoom(Room room)
    {
        return new RoomReadSnapshot(
            room.Id,
            room.Name,
            room.Description,
            room.Amenities,
            room.Capacity,
            room.PricePerNight,
            room.Quantity,
            room.IncludesBreakfast,
            room.HasPrivateBathroom,
            room.HasSaunaAccess,
            room.HasBalcony,
            room.HasWorkspace,
            room.HasAirConditioning,
            room.IsActive,
            room.Images.Select(RoomImageSnapshot.FromImage).ToList());
    }

    public Room ToRoom(Guid hotelId)
    {
        return new Room
        {
            Id = Id,
            HotelId = hotelId,
            Name = Name,
            Description = Description,
            Amenities = Amenities,
            Capacity = Capacity,
            PricePerNight = PricePerNight,
            Quantity = Quantity,
            IncludesBreakfast = IncludesBreakfast,
            HasPrivateBathroom = HasPrivateBathroom,
            HasSaunaAccess = HasSaunaAccess,
            HasBalcony = HasBalcony,
            HasWorkspace = HasWorkspace,
            HasAirConditioning = HasAirConditioning,
            IsActive = IsActive,
            Images = Images.Select(i => i.ToImage(Id)).ToList()
        };
    }
}

public sealed record HotelImageSnapshot(
    Guid Id,
    string StorageKey,
    string Url,
    string ContentType,
    long SizeBytes,
    int Width,
    int Height,
    string? AltText,
    bool IsCover,
    int SortOrder,
    DateTimeOffset CreatedAtUtc)
{
    public static HotelImageSnapshot FromImage(HotelImage image)
    {
        return new HotelImageSnapshot(
            image.Id,
            image.StorageKey,
            image.Url,
            image.ContentType,
            image.SizeBytes,
            image.Width,
            image.Height,
            image.AltText,
            image.IsCover,
            image.SortOrder,
            image.CreatedAtUtc);
    }

    public HotelImage ToImage(Guid hotelId)
    {
        return new HotelImage
        {
            Id = Id,
            HotelId = hotelId,
            StorageKey = StorageKey,
            Url = Url,
            ContentType = ContentType,
            SizeBytes = SizeBytes,
            Width = Width,
            Height = Height,
            AltText = AltText,
            IsCover = IsCover,
            SortOrder = SortOrder,
            CreatedAtUtc = CreatedAtUtc
        };
    }
}

public sealed record RoomImageSnapshot(
    Guid Id,
    string StorageKey,
    string Url,
    string ContentType,
    long SizeBytes,
    int Width,
    int Height,
    string? AltText,
    bool IsCover,
    int SortOrder,
    DateTimeOffset CreatedAtUtc)
{
    public static RoomImageSnapshot FromImage(RoomImage image)
    {
        return new RoomImageSnapshot(
            image.Id,
            image.StorageKey,
            image.Url,
            image.ContentType,
            image.SizeBytes,
            image.Width,
            image.Height,
            image.AltText,
            image.IsCover,
            image.SortOrder,
            image.CreatedAtUtc);
    }

    public RoomImage ToImage(Guid roomId)
    {
        return new RoomImage
        {
            Id = Id,
            RoomId = roomId,
            StorageKey = StorageKey,
            Url = Url,
            ContentType = ContentType,
            SizeBytes = SizeBytes,
            Width = Width,
            Height = Height,
            AltText = AltText,
            IsCover = IsCover,
            SortOrder = SortOrder,
            CreatedAtUtc = CreatedAtUtc
        };
    }
}
