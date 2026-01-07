namespace HotelBooking.Web.ViewModels.UserProfileViewModels;

public class UserBookingItemViewModel
{
    public string HotelName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Nights { get; set; }
    public decimal TotalPrice { get; set; }
    public string StatusName { get; set; } = string.Empty;
}