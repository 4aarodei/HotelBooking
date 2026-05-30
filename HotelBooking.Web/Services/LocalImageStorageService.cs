using HotelBooking.Application.Media;
using Microsoft.Extensions.Options;

namespace HotelBooking.Web.Services;

public class LocalImageStorageService : IImageStorage
{
    private readonly IWebHostEnvironment _environment;
    private readonly LocalImageStorageOptions _options;

    public LocalImageStorageService(IWebHostEnvironment environment, IOptions<LocalImageStorageOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public Task<StoredImage> SaveHotelImageAsync(Guid hotelId, ProcessedImage image, CancellationToken ct)
    {
        return SaveAsync(["hotels", hotelId.ToString("N")], image, ct);
    }

    public Task<StoredImage> SaveRoomImageAsync(Guid roomId, ProcessedImage image, CancellationToken ct)
    {
        return SaveAsync(["rooms", roomId.ToString("N")], image, ct);
    }

    public Task DeleteAsync(string storageKey, string publicUrl, CancellationToken ct)
    {
        var url = string.IsNullOrWhiteSpace(publicUrl) ? storageKey : publicUrl;
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith('/'))
        {
            return Task.CompletedTask;
        }

        var relative = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var root = GetStorageRoot();
        var fullPath = Path.GetFullPath(Path.Combine(GetWebRoot(), relative));

        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            return Task.CompletedTask;
        }

        File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private async Task<StoredImage> SaveAsync(string[] segments, ProcessedImage image, CancellationToken ct)
    {
        var fileName = $"{Guid.NewGuid():N}{image.FileExtension}";
        var directory = Path.Combine([GetStorageRoot(), .. segments]);
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, fileName);
        await using (var stream = File.Create(fullPath))
        {
            image.Content.Position = 0;
            await image.Content.CopyToAsync(stream, ct);
        }

        var urlSegments = new[] { _options.RootDirectory }.Concat(segments).Append(fileName);
        var url = "/" + string.Join("/", urlSegments.Select(s => s.Trim('/')));
        var storageKey = string.Join("/", segments.Append(fileName));

        return new StoredImage(storageKey, url, image.ContentType, image.SizeBytes, image.Width, image.Height);
    }

    private string GetStorageRoot()
    {
        return Path.GetFullPath(Path.Combine(GetWebRoot(), _options.RootDirectory));
    }

    private string GetWebRoot()
    {
        return _environment.WebRootPath
               ?? throw new InvalidOperationException("Web root path is not configured.");
    }
}
