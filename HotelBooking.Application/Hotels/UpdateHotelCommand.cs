using HotelBooking.Application.Media;

namespace HotelBooking.Application.Hotels;

public sealed record UpdateHotelCommand(
    Guid HotelId,
    string Name,
    string City,
    string Address,
    string? Description,
    IReadOnlyList<ImageUploadFile> Photos,
    IReadOnlyList<Guid> RemoveImageIds);
