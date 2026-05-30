using HotelBooking.Application.Media;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace HotelBooking.Web.Services;

public sealed class RoomDraftImageUploadService
{
    private const string TempContentType = "image/webp";

    private readonly IWebHostEnvironment _environment;
    private readonly LocalImageStorageOptions _storageOptions;
    private readonly IImageProcessor _imageProcessor;

    public RoomDraftImageUploadService(
        IWebHostEnvironment environment,
        IOptions<LocalImageStorageOptions> storageOptions,
        IImageProcessor imageProcessor)
    {
        _environment = environment;
        _storageOptions = storageOptions.Value;
        _imageProcessor = imageProcessor;
    }

    public async Task<IReadOnlyList<DraftUploadItem>> SaveDraftFilesAsync(Guid draftId, IEnumerable<IFormFile> files, CancellationToken ct)
    {
        var result = new List<DraftUploadItem>();
        var directory = GetDraftDirectory(draftId);
        Directory.CreateDirectory(directory);

        foreach (var file in files.Where(f => f.Length > 0))
        {
            var uploadFile = new ImageUploadFile(
                file.FileName,
                file.ContentType,
                file.Length,
                file.OpenReadStream);

            using var processed = await _imageProcessor.ProcessAsync(uploadFile, ct);
            var fileName = $"{Guid.NewGuid():N}{processed.FileExtension}";
            var fullPath = Path.Combine(directory, fileName);

            processed.Content.Position = 0;
            await using (var output = File.Create(fullPath))
            {
                await processed.Content.CopyToAsync(output, ct);
            }

            result.Add(new DraftUploadItem(fileName, BuildDraftUrl(draftId, fileName)));
        }

        return result;
    }

    public Task<IReadOnlyList<ImageUploadFile>> GetDraftAsUploadFilesAsync(Guid draftId, CancellationToken ct)
    {
        var directory = GetDraftDirectory(draftId);
        if (!Directory.Exists(directory))
        {
            return Task.FromResult<IReadOnlyList<ImageUploadFile>>([]);
        }

        var files = Directory.GetFiles(directory)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path =>
            {
                var info = new FileInfo(path);
                return new ImageUploadFile(
                    info.Name,
                    TempContentType,
                    info.Length,
                    () => File.OpenRead(path));
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<ImageUploadFile>>(files);
    }

    public Task DeleteDraftAsync(Guid draftId, CancellationToken ct)
    {
        var directory = GetDraftDirectory(draftId);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        return Task.CompletedTask;
    }

    private string GetDraftDirectory(Guid draftId)
    {
        var webRoot = _environment.WebRootPath
                      ?? throw new InvalidOperationException("Web root path is not configured.");
        var baseRoot = Path.Combine(webRoot, _storageOptions.RootDirectory, "temp", "rooms", draftId.ToString("N"));
        return Path.GetFullPath(baseRoot);
    }

    private string BuildDraftUrl(Guid draftId, string fileName)
    {
        return $"/{_storageOptions.RootDirectory.Trim('/')}/temp/rooms/{draftId:N}/{fileName}";
    }
}

public sealed record DraftUploadItem(string FileName, string Url);
