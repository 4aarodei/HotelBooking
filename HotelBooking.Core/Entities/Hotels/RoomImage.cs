namespace HotelBooking.Domain.Entities.Hotels;

public class RoomImage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RoomId { get; set; }
    public Room? Room { get; set; }

    public required string StorageKey { get; set; }
    public required string Url { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
