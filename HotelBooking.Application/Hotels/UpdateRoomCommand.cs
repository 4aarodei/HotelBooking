using HotelBooking.Application.Media;

namespace HotelBooking.Application.Hotels;

public sealed record UpdateRoomCommand(
    Guid RoomId,
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
    IReadOnlyList<ImageUploadFile> Photos,
    IReadOnlyList<Guid> RemoveImageIds);
