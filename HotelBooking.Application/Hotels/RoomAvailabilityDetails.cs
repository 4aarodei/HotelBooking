namespace HotelBooking.Application.Hotels;

public sealed record RoomAvailabilityDetails(
    Guid Id,
    Guid HotelId,
    string HotelName,
    string HotelCity,
    string HotelAddress,
    string Name,
    string? Description,
    string? Amenities,
    IReadOnlyList<ImageReadModel> Images,
    int Capacity,
    int Quantity,
    int AvailableQuantity,
    decimal PricePerNight,
    bool IncludesBreakfast,
    bool HasPrivateBathroom,
    bool HasSaunaAccess,
    bool HasBalcony,
    bool HasWorkspace,
    bool HasAirConditioning);
