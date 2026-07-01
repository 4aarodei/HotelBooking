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

    public static HotelReadSnapshot FromReadModel(HotelReadModel hotel)
    {
        return new HotelReadSnapshot(
            hotel.Id,
            hotel.Name,
            hotel.City,
            hotel.Address,
            hotel.Description,
            hotel.Images.Select(HotelImageSnapshot.FromReadModel).ToList(),
            hotel.Rooms.Select(RoomReadSnapshot.FromReadModel).ToList());
    }

    public Hotel ToHotel()
    {
        var hotel = Hotel.Create(Id, Name, City, Address, Description);
        hotel.ReplaceImages(Images.Select(i => i.ToImage(Id)));
        hotel.ReplaceRooms(Rooms.Select(r => r.ToRoom(Id)));
        return hotel;
    }

    public HotelReadModel ToReadModel(IEnumerable<RoomReadSnapshot>? rooms = null)
    {
        return new HotelReadModel(
            Id,
            Name,
            City,
            Address,
            Description,
            Images.Select(i => i.ToReadModel()).ToList(),
            (rooms ?? Rooms).Select(r => r.ToReadModel()).ToList());
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

    public static RoomReadSnapshot FromReadModel(RoomReadModel room)
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
            room.Images.Select(RoomImageSnapshot.FromReadModel).ToList());
    }

    public Room ToRoom(Guid hotelId)
    {
        var room = Room.Create(
            Id,
            hotelId,
            Name,
            Description,
            Amenities,
            Capacity,
            PricePerNight,
            Quantity,
            new RoomFeatures(
                IncludesBreakfast,
                HasPrivateBathroom,
                HasSaunaAccess,
                HasBalcony,
                HasWorkspace,
                HasAirConditioning),
            IsActive);

        room.ReplaceImages(Images.Select(i => i.ToImage(Id)));
        return room;
    }

    public RoomReadModel ToReadModel()
    {
        return new RoomReadModel(
            Id,
            Name,
            Description,
            Amenities,
            Capacity,
            PricePerNight,
            Quantity,
            IncludesBreakfast,
            HasPrivateBathroom,
            HasSaunaAccess,
            HasBalcony,
            HasWorkspace,
            HasAirConditioning,
            IsActive,
            Images.Select(i => i.ToReadModel()).ToList());
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

    public static HotelImageSnapshot FromReadModel(ImageReadModel image)
    {
        return new HotelImageSnapshot(
            Guid.NewGuid(),
            string.Empty,
            image.Url,
            string.Empty,
            0,
            image.Width,
            image.Height,
            image.AltText,
            image.IsCover,
            image.SortOrder,
            DateTimeOffset.UtcNow);
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

    public ImageReadModel ToReadModel()
    {
        return new ImageReadModel(Url, AltText, Width, Height, IsCover, SortOrder);
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

    public static RoomImageSnapshot FromReadModel(ImageReadModel image)
    {
        return new RoomImageSnapshot(
            Guid.NewGuid(),
            string.Empty,
            image.Url,
            string.Empty,
            0,
            image.Width,
            image.Height,
            image.AltText,
            image.IsCover,
            image.SortOrder,
            DateTimeOffset.UtcNow);
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

    public ImageReadModel ToReadModel()
    {
        return new ImageReadModel(Url, AltText, Width, Height, IsCover, SortOrder);
    }
}
