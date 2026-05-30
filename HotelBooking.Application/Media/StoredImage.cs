namespace HotelBooking.Application.Media;

public sealed record StoredImage(
    string StorageKey,
    string PublicUrl,
    string ContentType,
    long SizeBytes,
    int Width,
    int Height);
