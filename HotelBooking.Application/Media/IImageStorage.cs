namespace HotelBooking.Application.Media;

public interface IImageStorage
{
    Task<StoredImage> SaveHotelImageAsync(Guid hotelId, ProcessedImage image, CancellationToken ct);
    Task<StoredImage> SaveRoomImageAsync(Guid roomId, ProcessedImage image, CancellationToken ct);
    Task DeleteAsync(string storageKey, string publicUrl, CancellationToken ct);
}
