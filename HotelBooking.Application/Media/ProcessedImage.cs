namespace HotelBooking.Application.Media;

public sealed class ProcessedImage : IDisposable
{
    public ProcessedImage(
        Stream content,
        string originalFileName,
        string contentType,
        string fileExtension,
        long sizeBytes,
        int width,
        int height)
    {
        Content = content;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        FileExtension = fileExtension;
        SizeBytes = sizeBytes;
        Width = width;
        Height = height;
    }

    public Stream Content { get; }
    public string OriginalFileName { get; }
    public string ContentType { get; }
    public string FileExtension { get; }
    public long SizeBytes { get; }
    public int Width { get; }
    public int Height { get; }

    public void Dispose()
    {
        Content.Dispose();
    }
}
