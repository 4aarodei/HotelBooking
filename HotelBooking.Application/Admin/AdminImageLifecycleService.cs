using HotelBooking.Application.Media;
using HotelBooking.Domain.Entities.Hotels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Application.Admin;

public sealed class AdminImageLifecycleService
{
    private readonly IImageProcessor _imageProcessor;
    private readonly IImageStorage _imageStorage;
    private readonly ImageUploadOptions _imageOptions;
    private readonly ILogger<AdminImageLifecycleService> _logger;

    public AdminImageLifecycleService(
        IImageProcessor imageProcessor,
        IImageStorage imageStorage,
        IOptions<ImageUploadOptions> imageOptions,
        ILogger<AdminImageLifecycleService> logger)
    {
        _imageProcessor = imageProcessor;
        _imageStorage = imageStorage;
        _imageOptions = imageOptions.Value;
        _logger = logger;
    }

    public void EnsureFileCount(IReadOnlyList<ImageUploadFile> files)
    {
        if (files.Count > _imageOptions.MaxFilesPerUpload)
        {
            throw new ImageUploadValidationException($"Upload at most {_imageOptions.MaxFilesPerUpload} images at a time.");
        }
    }

    public async Task AddHotelImagesAsync(Hotel hotel, IReadOnlyList<ImageUploadFile> files, List<HotelImage> addedImages, CancellationToken ct)
    {
        if (files.Count == 0)
        {
            return;
        }

        var sortOrder = hotel.Images.Count == 0 ? 0 : hotel.Images.Max(i => i.SortOrder) + 1;
        foreach (var file in files)
        {
            using var processed = await _imageProcessor.ProcessAsync(file, ct);
            var stored = await _imageStorage.SaveHotelImageAsync(hotel.Id, processed, ct);
            var image = HotelImage.Create(
                stored.StorageKey,
                stored.PublicUrl,
                stored.ContentType,
                stored.SizeBytes,
                stored.Width,
                stored.Height,
                hotel.Name);
            image.SortOrder = sortOrder++;

            hotel.AddImage(image);
            addedImages.Add(image);
        }
    }

    public async Task AddRoomImagesAsync(Room room, IReadOnlyList<ImageUploadFile> files, List<RoomImage> addedImages, CancellationToken ct)
    {
        if (files.Count == 0)
        {
            return;
        }

        var sortOrder = room.Images.Count == 0 ? 0 : room.Images.Max(i => i.SortOrder) + 1;
        foreach (var file in files)
        {
            using var processed = await _imageProcessor.ProcessAsync(file, ct);
            var stored = await _imageStorage.SaveRoomImageAsync(room.Id, processed, ct);
            var image = RoomImage.Create(
                stored.StorageKey,
                stored.PublicUrl,
                stored.ContentType,
                stored.SizeBytes,
                stored.Width,
                stored.Height,
                room.Name);
            image.SortOrder = sortOrder++;

            room.AddImage(image);
            addedImages.Add(image);
        }
    }

    public List<HotelImage> RemoveHotelImages(Hotel hotel, IReadOnlyList<Guid> imageIds)
    {
        var ids = imageIds.ToHashSet();
        var removed = hotel.RemoveImages(ids).ToList();
        foreach (var image in removed)
        {
            _logger.LogInformation(
                "Admin removed hotel image metadata. HotelId: {HotelId}, ImageId: {ImageId}, StorageKey: {StorageKey}",
                hotel.Id,
                image.Id,
                image.StorageKey);
        }

        return removed;
    }

    public List<RoomImage> RemoveRoomImages(Room room, IReadOnlyList<Guid> imageIds)
    {
        var ids = imageIds.ToHashSet();
        var removed = room.RemoveImages(ids).ToList();
        foreach (var image in removed)
        {
            _logger.LogInformation(
                "Admin removed room image metadata. RoomId: {RoomId}, ImageId: {ImageId}, StorageKey: {StorageKey}",
                room.Id,
                image.Id,
                image.StorageKey);
        }

        return removed;
    }

    public async Task CleanupHotelImagesAsync(IEnumerable<HotelImage> images, CancellationToken ct)
    {
        foreach (var image in images)
        {
            await DeleteStoredImageAsync(image.StorageKey, image.Url, "hotel-upload-cleanup", image.Id, ct);
        }
    }

    public async Task CleanupRoomImagesAsync(IEnumerable<RoomImage> images, CancellationToken ct)
    {
        foreach (var image in images)
        {
            await DeleteStoredImageAsync(image.StorageKey, image.Url, "room-upload-cleanup", image.Id, ct);
        }
    }

    public async Task DeleteHotelImagesAsync(IEnumerable<HotelImage> images, Guid hotelId, CancellationToken ct)
    {
        foreach (var image in images)
        {
            await DeleteStoredImageAsync(image.StorageKey, image.Url, $"hotel:{hotelId:N}", image.Id, ct);
        }
    }

    public async Task DeleteRoomImagesAsync(IEnumerable<RoomImage> images, Guid roomId, CancellationToken ct)
    {
        foreach (var image in images)
        {
            await DeleteStoredImageAsync(image.StorageKey, image.Url, $"room:{roomId:N}", image.Id, ct);
        }
    }

    private async Task DeleteStoredImageAsync(string storageKey, string publicUrl, string scope, Guid imageId, CancellationToken ct)
    {
        try
        {
            await _imageStorage.DeleteAsync(storageKey, publicUrl, ct);
            _logger.LogInformation(
                "Deleted image blob. Scope: {Scope}, ImageId: {ImageId}, StorageKey: {StorageKey}",
                scope,
                imageId,
                storageKey);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Image blob delete failed and should be retried by a future cleanup worker. Scope: {Scope}, ImageId: {ImageId}, StorageKey: {StorageKey}",
                scope,
                imageId,
                storageKey);
        }
    }
}
