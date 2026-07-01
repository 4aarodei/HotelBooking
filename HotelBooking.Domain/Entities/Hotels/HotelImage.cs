namespace HotelBooking.Domain.Entities.Hotels;

public class HotelImage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid HotelId { get; set; }
    public Hotel? Hotel { get; set; }

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

    public static HotelImage Create(
        string storageKey,
        string url,
        string contentType,
        long sizeBytes,
        int width,
        int height,
        string? altText)
    {
        return new HotelImage
        {
            StorageKey = storageKey,
            Url = url,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            Width = width,
            Height = height,
            AltText = altText
        };
    }
}
