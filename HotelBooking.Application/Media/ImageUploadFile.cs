namespace HotelBooking.Application.Media;

public sealed record ImageUploadFile(
    string FileName,
    string ContentType,
    long Length,
    Func<Stream> OpenReadStream);
