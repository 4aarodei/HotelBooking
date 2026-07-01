namespace HotelBooking.Application.Admin;

public sealed record AdminHotelListItem(
    Guid Id,
    string Name,
    string City,
    string Address,
    string? CoverImageUrl,
    int? CoverImageWidth,
    int? CoverImageHeight);

public sealed record AdminHotelEditDetails(
    Guid Id,
    string Name,
    string City,
    string Address,
    string? Description,
    IReadOnlyList<AdminImageItem> Images,
    IReadOnlyList<AdminRoomListItem> Rooms);

public sealed record AdminRoomFormDetails(
    Guid? Id,
    Guid HotelId,
    string HotelName,
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
    IReadOnlyList<AdminImageItem> Images);

public sealed record AdminRoomListItem(
    Guid Id,
    string Name,
    int Capacity,
    int Quantity,
    decimal PricePerNight,
    bool IsActive,
    string? CoverImageUrl,
    int? CoverImageWidth,
    int? CoverImageHeight);

public sealed record AdminImageItem(
    Guid Id,
    string Url,
    string AltText,
    int Width,
    int Height,
    bool IsCover);
