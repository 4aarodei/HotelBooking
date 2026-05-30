namespace HotelBooking.Application.Media;

public class ImageUploadOptions
{
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
    public int MaxFilesPerUpload { get; set; } = 8;
    public int MaxWidth { get; set; } = 4096;
    public int MaxHeight { get; set; } = 4096;
    public long MaxPixelCount { get; set; } = 12_000_000;
    public int ReencodeQuality { get; set; } = 82;

    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public string[] AllowedExtensions { get; set; } =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    ];
}
