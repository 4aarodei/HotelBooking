namespace HotelBooking.Web.ViewModels;

public class RoomDetailsViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public int? ImageWidth { get; init; }
    public int? ImageHeight { get; init; }
    public List<ImageViewModel> Images { get; init; } = new();
    public int Capacity { get; init; }
    public int Quantity { get; init; }
    public string Description { get; init; } = string.Empty;
    public bool IncludesBreakfast { get; init; }
    public bool HasPrivateBathroom { get; init; }
    public bool HasSaunaAccess { get; init; }
    public bool HasBalcony { get; init; }
    public bool HasWorkspace { get; init; }
    public bool HasAirConditioning { get; init; }
    public decimal PricePerNight { get; init; }
}
