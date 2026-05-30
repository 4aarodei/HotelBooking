namespace HotelBooking.ViewModels.Admin;

public class AdminRoomListItemViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Capacity { get; init; }
    public int Quantity { get; init; }
    public decimal PricePerNight { get; init; }
    public bool IsActive { get; init; }
    public string? CoverImageUrl { get; init; }
}
