namespace HotelBooking.ViewModels.Admin;

public class AdminImageViewModel
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public string AltText { get; init; } = string.Empty;
    public bool IsCover { get; init; }
}
