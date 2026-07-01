using HotelBooking.Domain.Exceptions;

namespace HotelBooking.Domain.Entities.Hotels;

public class Room
{
    private Room()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid HotelId { get; private set; }
    public Hotel? Hotel { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Amenities { get; private set; }
    public int Capacity { get; private set; }
    public decimal PricePerNight { get; private set; }

    public int Quantity { get; private set; } = 1;

    public bool IncludesBreakfast { get; private set; }
    public bool HasPrivateBathroom { get; private set; } = true;
    public bool HasSaunaAccess { get; private set; }
    public bool HasBalcony { get; private set; }
    public bool HasWorkspace { get; private set; }
    public bool HasAirConditioning { get; private set; }

    public bool IsActive { get; private set; } = true;

    public ICollection<RoomImage> Images { get; private set; } = new List<RoomImage>();

    public static Room Create(
        Guid hotelId,
        string name,
        string? description,
        string? amenities,
        int capacity,
        decimal pricePerNight,
        int quantity,
        RoomFeatures features,
        bool isActive)
    {
        return Create(
            Guid.NewGuid(),
            hotelId,
            name,
            description,
            amenities,
            capacity,
            pricePerNight,
            quantity,
            features,
            isActive);
    }

    public static Room Create(
        Guid hotelId,
        string name,
        string? description,
        string? amenities,
        int capacity,
        decimal pricePerNight,
        int quantity,
        bool includesBreakfast,
        bool hasPrivateBathroom,
        bool hasSaunaAccess,
        bool hasBalcony,
        bool hasWorkspace,
        bool hasAirConditioning,
        bool isActive)
    {
        return Create(
            hotelId,
            name,
            description,
            amenities,
            capacity,
            pricePerNight,
            quantity,
            new RoomFeatures(
                includesBreakfast,
                hasPrivateBathroom,
                hasSaunaAccess,
                hasBalcony,
                hasWorkspace,
                hasAirConditioning),
            isActive);
    }

    public static Room Create(
        Guid id,
        Guid hotelId,
        string name,
        string? description,
        string? amenities,
        int capacity,
        decimal pricePerNight,
        int quantity,
        RoomFeatures features,
        bool isActive)
    {
        if (hotelId == Guid.Empty)
        {
            throw new DomainRuleViolationException("Hotel is required.");
        }

        var room = new Room
        {
            Id = id,
            HotelId = hotelId
        };

        room.UpdateDetails(
            name,
            description,
            amenities,
            capacity,
            pricePerNight,
            quantity,
            features,
            isActive);

        return room;
    }

    public static Room Create(
        Guid id,
        Guid hotelId,
        string name,
        string? description,
        string? amenities,
        int capacity,
        decimal pricePerNight,
        int quantity,
        bool includesBreakfast,
        bool hasPrivateBathroom,
        bool hasSaunaAccess,
        bool hasBalcony,
        bool hasWorkspace,
        bool hasAirConditioning,
        bool isActive)
    {
        return Create(
            id,
            hotelId,
            name,
            description,
            amenities,
            capacity,
            pricePerNight,
            quantity,
            new RoomFeatures(
                includesBreakfast,
                hasPrivateBathroom,
                hasSaunaAccess,
                hasBalcony,
                hasWorkspace,
                hasAirConditioning),
            isActive);
    }

    public void UpdateDetails(
        string name,
        string? description,
        string? amenities,
        int capacity,
        decimal pricePerNight,
        int quantity,
        RoomFeatures features,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleViolationException("Room name is required.");
        }

        if (capacity <= 0)
        {
            throw new DomainRuleViolationException("Room capacity must be greater than zero.");
        }

        if (pricePerNight <= 0)
        {
            throw new DomainRuleViolationException("Room price must be greater than zero.");
        }

        if (quantity <= 0)
        {
            throw new DomainRuleViolationException("Room quantity must be greater than zero.");
        }

        Name = name.Trim();
        Description = NormalizeOptionalText(description);
        Amenities = NormalizeOptionalText(amenities);
        Capacity = capacity;
        PricePerNight = pricePerNight;
        Quantity = quantity;
        IncludesBreakfast = features.IncludesBreakfast;
        HasPrivateBathroom = features.HasPrivateBathroom;
        HasSaunaAccess = features.HasSaunaAccess;
        HasBalcony = features.HasBalcony;
        HasWorkspace = features.HasWorkspace;
        HasAirConditioning = features.HasAirConditioning;
        IsActive = isActive;
    }

    public void ReplaceImages(IEnumerable<RoomImage> images)
    {
        Images = images.ToList();
        NormalizeImages();
    }

    public void AddImage(RoomImage image)
    {
        image.RoomId = Id;
        Images.Add(image);
        NormalizeImages();
    }

    public IReadOnlyList<RoomImage> RemoveImages(IEnumerable<Guid> imageIds)
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

    public void EnsureCanBeBooked()
    {
        if (!IsActive)
        {
            throw new DomainRuleViolationException("Room is not available for booking.");
        }

        if (Quantity <= 0)
        {
            throw new DomainRuleViolationException("Room is already fully booked for these dates.");
        }
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
