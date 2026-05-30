namespace HotelBooking.Application.Media;

public sealed class ImageUploadValidationException : Exception
{
    public ImageUploadValidationException(string message)
        : base(message)
    {
    }
}
