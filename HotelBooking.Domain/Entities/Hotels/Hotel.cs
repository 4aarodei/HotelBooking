using HotelBooking.Domain.Exceptions;

namespace HotelBooking.Domain.Entities.Hotels;

public class Hotel
{
    private Hotel()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public ICollection<Room> Rooms { get; private set; } = new List<Room>();
    public ICollection<HotelImage> Images { get; private set; } = new List<HotelImage>();

    public static Hotel Create(string name, string city, string address, string? description = null)
    {
        return Create(Guid.NewGuid(), name, city, address, description);
    }

    public static Hotel Create(Guid id, string name, string city, string address, string? description = null)
    {
        var hotel = new Hotel { Id = id };
        hotel.UpdateDetails(name, city, address, description);
        return hotel;
    }

    public void UpdateDetails(string name, string city, string address, string? description)
    {
        Name = RequireText(name, "Hotel name is required.");
        City = RequireText(city, "Hotel city is required.");
        Address = RequireText(address, "Hotel address is required.");
        Description = NormalizeOptionalText(description);
    }

    public void ReplaceRooms(IEnumerable<Room> rooms)
    {
        Rooms = rooms.ToList();
    }

    public void ReplaceImages(IEnumerable<HotelImage> images)
    {
        Images = images.ToList();
        NormalizeImages();
    }

    public void AddImage(HotelImage image)
    {
        image.HotelId = Id;
        Images.Add(image);
        NormalizeImages();
    }

    public IReadOnlyList<HotelImage> RemoveImages(IEnumerable<Guid> imageIds)
    {
        var ids = imageIds.ToHashSet();
        var removed = Images.Where(i => ids.Contains(i.Id)).ToList();
        foreach (var image in removed)
        {
            Images.Remove(image);
        }

        NormalizeImages();
        return removed;
    }

    public void NormalizeImages()
    {
        var ordered = Images.OrderByDescending(i => i.IsCover).ThenBy(i => i.SortOrder).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].IsCover = i == 0;
            ordered[i].SortOrder = i;
        }
    }

    private static string RequireText(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleViolationException(message);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
