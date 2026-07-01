namespace HotelBooking.Application.Hotels;

public sealed record HotelReadModel(
    Guid Id,
    string Name,
    string City,
    string Address,
    string? Description,
    IReadOnlyList<ImageReadModel> Images,
    IReadOnlyList<RoomReadModel> Rooms);

public sealed record RoomReadModel(
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
    IReadOnlyList<ImageReadModel> Images);

public sealed record ImageReadModel(
    string Url,
    string? AltText,
    int Width,
    int Height,
    bool IsCover,
    int SortOrder);
