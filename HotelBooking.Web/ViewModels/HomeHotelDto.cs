namespace HotelBooking.Web.ViewModels;

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

public class HomeHotelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string City { get; set; } = null!;
    public string? Description { get; set; }
    public int ActiveRoomsCount { get; set; }
}