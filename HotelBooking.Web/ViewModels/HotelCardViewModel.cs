namespace HotelBooking.Web.ViewModels;

public class HotelCardViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public string ActionText { get; set; } = string.Empty;
    public string? CheckIn { get; set; }
    public string? CheckOut { get; set; }
}
