namespace HotelBooking.Web.ViewModels;

public class RoomDetailsViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public List<ImageViewModel> Images { get; init; } = new();
    public int Capacity { get; init; }
    public int Quantity { get; init; }
    public decimal PricePerNight { get; init; }
}
