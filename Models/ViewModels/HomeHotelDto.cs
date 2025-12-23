namespace HotelBooking.Models.ViewModels;

public class HomeViewModel
{
    public List<HotelCardVm> PopularHotels { get; set; } = new();
}

public class HotelCardVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string City { get; set; } = null!;
    public decimal MinPrice { get; set; }
}
