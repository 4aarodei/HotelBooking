using HotelBooking.Application.Media;
using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Application.Admin;

public sealed record UpdateRoomCommand(
    Guid RoomId,
    string Name,
    string? Description,
    string? Amenities,
    int Capacity,
    decimal PricePerNight,
    int Quantity,
    RoomFeatures Features,
    bool IsActive,
    IReadOnlyList<ImageUploadFile> Photos,
    IReadOnlyList<Guid> RemoveImageIds)
{
    public UpdateRoomCommand(
        Guid roomId,
        string name,
        string? description,
        string? amenities,
        int capacity,
        decimal pricePerNight,
        int quantity,
        bool includesBreakfast,
        bool hasPrivateBathroom,
        bool hasSaunaAccess,
        bool hasBalcony,
        bool hasWorkspace,
        bool hasAirConditioning,
        bool isActive,
        IReadOnlyList<ImageUploadFile> photos,
        IReadOnlyList<Guid> removeImageIds)
        : this(
            roomId,
            name,
            description,
            amenities,
            capacity,
            pricePerNight,
            quantity,
            new RoomFeatures(
                includesBreakfast,
                hasPrivateBathroom,
                hasSaunaAccess,
                hasBalcony,
                hasWorkspace,
                hasAirConditioning),
            isActive,
            photos,
            removeImageIds)
    {
    }
}
