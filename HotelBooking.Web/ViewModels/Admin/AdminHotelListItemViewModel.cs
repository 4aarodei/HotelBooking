namespace HotelBooking.ViewModels.Admin;

public class AdminHotelListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public int? CoverImageWidth { get; set; }
    public int? CoverImageHeight { get; set; }
}
