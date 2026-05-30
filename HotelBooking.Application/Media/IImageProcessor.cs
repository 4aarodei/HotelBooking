namespace HotelBooking.Application.Media;

public interface IImageProcessor
{
    Task<ProcessedImage> ProcessAsync(ImageUploadFile file, CancellationToken ct);
}
