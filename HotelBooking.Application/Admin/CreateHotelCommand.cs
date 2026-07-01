using HotelBooking.Application.Media;

namespace HotelBooking.Application.Admin;

public sealed record CreateHotelCommand(
    string Name,
    string City,
    string Address,
    string? Description,
    IReadOnlyList<ImageUploadFile> Photos);
