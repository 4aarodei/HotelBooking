using HotelBooking.Application.Hotels;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Media;
using HotelBooking.Domain.Entities.Hotels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Application.Services;

public sealed class AdminHotelManagementService : IAdminHotelManagementService
{
    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IImageProcessor _imageProcessor;
    private readonly IImageStorage _imageStorage;
    private readonly ImageUploadOptions _imageOptions;
    private readonly ILogger<AdminHotelManagementService> _logger;

    public AdminHotelManagementService(
        IHotelRepository hotelRepository,
        IRoomRepository roomRepository,
        IImageProcessor imageProcessor,
        IImageStorage imageStorage,
        IOptions<ImageUploadOptions> imageOptions,
        ILogger<AdminHotelManagementService> logger)
    {
        _hotelRepository = hotelRepository;
        _roomRepository = roomRepository;
        _imageProcessor = imageProcessor;
        _imageStorage = imageStorage;
        _imageOptions = imageOptions.Value;
        _logger = logger;
    }

    public async Task<Guid> CreateHotelAsync(CreateHotelCommand command, CancellationToken ct = default)
    {
        EnsureFileCount(command.Photos);

        var hotel = new Hotel
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            City = command.City.Trim(),
            Address = command.Address.Trim(),
            Description = command.Description?.Trim()
        };

        var newImages = new List<HotelImage>();
        await AddHotelImagesAsync(hotel, command.Photos, newImages, ct);

        try
        {
            NormalizeHotelCovers(hotel.Images);
            await _hotelRepository.AddAsync(hotel, ct);
            return hotel.Id;
        }
        catch
        {
            await CleanupHotelImagesAsync(newImages, ct);
            throw;
        }
    }

    public async Task UpdateHotelAsync(UpdateHotelCommand command, CancellationToken ct = default)
    {
        EnsureFileCount(command.Photos);

        var hotel = await _hotelRepository.GetByIdWithImagesAsync(command.HotelId, ct)
            ?? throw new InvalidOperationException($"Hotel {command.HotelId} was not found.");

        hotel.Name = command.Name.Trim();
        hotel.City = command.City.Trim();
        hotel.Address = command.Address.Trim();
        hotel.Description = command.Description?.Trim();

        var newImages = new List<HotelImage>();
        await AddHotelImagesAsync(hotel, command.Photos, newImages, ct);

        var removedImages = RemoveHotelImages(hotel, command.RemoveImageIds);
        NormalizeHotelCovers(hotel.Images);

        try
        {
            await _hotelRepository.UpdateAsync(hotel, ct);
        }
        catch
        {
            await CleanupHotelImagesAsync(newImages, ct);
            throw;
        }

        await DeleteHotelImagesAsync(removedImages, hotel.Id, ct);
    }

    public async Task<Guid> CreateRoomAsync(CreateRoomCommand command, CancellationToken ct = default)
    {
        EnsureFileCount(command.Photos);

        var hotel = await _hotelRepository.GetByIdAsync(command.HotelId, ct)
            ?? throw new InvalidOperationException($"Hotel {command.HotelId} was not found.");

        var room = new Room
        {
            Id = Guid.NewGuid(),
            HotelId = hotel.Id,
            Name = command.Name.Trim(),
            Description = NormalizeOptionalText(command.Description),
            Amenities = NormalizeOptionalText(command.Amenities),
            Capacity = command.Capacity,
            PricePerNight = command.PricePerNight,
            Quantity = command.Quantity,
            IncludesBreakfast = command.IncludesBreakfast,
            HasPrivateBathroom = command.HasPrivateBathroom,
            HasSaunaAccess = command.HasSaunaAccess,
            HasBalcony = command.HasBalcony,
            HasWorkspace = command.HasWorkspace,
            HasAirConditioning = command.HasAirConditioning,
            IsActive = command.IsActive
        };

        var newImages = new List<RoomImage>();
        await AddRoomImagesAsync(room, command.Photos, newImages, ct);

        try
        {
            NormalizeRoomCovers(room.Images);
            await _roomRepository.AddAsync(room, ct);
            return room.Id;
        }
        catch
        {
            await CleanupRoomImagesAsync(newImages, ct);
            throw;
        }
    }

    public async Task UpdateRoomAsync(UpdateRoomCommand command, CancellationToken ct = default)
    {
        EnsureFileCount(command.Photos);

        var room = await _roomRepository.GetByIdWithImagesAsync(command.RoomId, ct)
            ?? throw new InvalidOperationException($"Room {command.RoomId} was not found.");

        room.Name = command.Name.Trim();
        room.Description = NormalizeOptionalText(command.Description);
        room.Amenities = NormalizeOptionalText(command.Amenities);
        room.Capacity = command.Capacity;
        room.PricePerNight = command.PricePerNight;
        room.Quantity = command.Quantity;
        room.IncludesBreakfast = command.IncludesBreakfast;
        room.HasPrivateBathroom = command.HasPrivateBathroom;
        room.HasSaunaAccess = command.HasSaunaAccess;
        room.HasBalcony = command.HasBalcony;
        room.HasWorkspace = command.HasWorkspace;
        room.HasAirConditioning = command.HasAirConditioning;
        room.IsActive = command.IsActive;

        var newImages = new List<RoomImage>();
        await AddRoomImagesAsync(room, command.Photos, newImages, ct);

        var removedImages = RemoveRoomImages(room, command.RemoveImageIds);
        NormalizeRoomCovers(room.Images);

        try
        {
            await _roomRepository.UpdateAsync(room, ct);
        }
        catch
        {
            await CleanupRoomImagesAsync(newImages, ct);
            throw;
        }

        await DeleteRoomImagesAsync(removedImages, room.Id, ct);
    }

    private void EnsureFileCount(IReadOnlyList<ImageUploadFile> files)
    {
        if (files.Count > _imageOptions.MaxFilesPerUpload)
        {
            throw new ImageUploadValidationException($"Upload at most {_imageOptions.MaxFilesPerUpload} images at a time.");
        }
    }

    private async Task AddHotelImagesAsync(Hotel hotel, IReadOnlyList<ImageUploadFile> files, List<HotelImage> addedImages, CancellationToken ct)
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
            var image = new HotelImage
            {
                StorageKey = stored.StorageKey,
                Url = stored.PublicUrl,
                ContentType = stored.ContentType,
                SizeBytes = stored.SizeBytes,
                Width = stored.Width,
                Height = stored.Height,
                AltText = hotel.Name,
                IsCover = hotel.Images.Count == 0,
                SortOrder = sortOrder++
            };

            hotel.Images.Add(image);
            addedImages.Add(image);
        }
    }

    private async Task AddRoomImagesAsync(Room room, IReadOnlyList<ImageUploadFile> files, List<RoomImage> addedImages, CancellationToken ct)
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
            var image = new RoomImage
            {
                StorageKey = stored.StorageKey,
                Url = stored.PublicUrl,
                ContentType = stored.ContentType,
                SizeBytes = stored.SizeBytes,
                Width = stored.Width,
                Height = stored.Height,
                AltText = room.Name,
                IsCover = room.Images.Count == 0,
                SortOrder = sortOrder++
            };

            room.Images.Add(image);
            addedImages.Add(image);
        }
    }

    private List<HotelImage> RemoveHotelImages(Hotel hotel, IReadOnlyList<Guid> imageIds)
    {
        var ids = imageIds.ToHashSet();
        var removed = hotel.Images.Where(i => ids.Contains(i.Id)).ToList();
        foreach (var image in removed)
        {
            hotel.Images.Remove(image);
            _logger.LogInformation(
                "Admin removed hotel image metadata. HotelId: {HotelId}, ImageId: {ImageId}, StorageKey: {StorageKey}",
                hotel.Id,
                image.Id,
                image.StorageKey);
        }

        return removed;
    }

    private List<RoomImage> RemoveRoomImages(Room room, IReadOnlyList<Guid> imageIds)
    {
        var ids = imageIds.ToHashSet();
        var removed = room.Images.Where(i => ids.Contains(i.Id)).ToList();
        foreach (var image in removed)
        {
            room.Images.Remove(image);
            _logger.LogInformation(
                "Admin removed room image metadata. RoomId: {RoomId}, ImageId: {ImageId}, StorageKey: {StorageKey}",
                room.Id,
                image.Id,
                image.StorageKey);
        }

        return removed;
    }

    private async Task CleanupHotelImagesAsync(IEnumerable<HotelImage> images, CancellationToken ct)
    {
        foreach (var image in images)
        {
            await DeleteStoredImageAsync(image.StorageKey, image.Url, "hotel-upload-cleanup", image.Id, ct);
        }
    }

    private async Task CleanupRoomImagesAsync(IEnumerable<RoomImage> images, CancellationToken ct)
    {
        foreach (var image in images)
        {
            await DeleteStoredImageAsync(image.StorageKey, image.Url, "room-upload-cleanup", image.Id, ct);
        }
    }

    private async Task DeleteHotelImagesAsync(IEnumerable<HotelImage> images, Guid hotelId, CancellationToken ct)
    {
        foreach (var image in images)
        {
            await DeleteStoredImageAsync(image.StorageKey, image.Url, $"hotel:{hotelId:N}", image.Id, ct);
        }
    }

    private async Task DeleteRoomImagesAsync(IEnumerable<RoomImage> images, Guid roomId, CancellationToken ct)
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
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Image blob delete failed and should be retried by a future cleanup worker. Scope: {Scope}, ImageId: {ImageId}, StorageKey: {StorageKey}",
                scope,
                imageId,
                storageKey);
        }
    }

    private static void NormalizeHotelCovers(ICollection<HotelImage> images)
    {
        var ordered = images.OrderBy(i => i.SortOrder).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].IsCover = i == 0;
            ordered[i].SortOrder = i;
        }
    }

    private static void NormalizeRoomCovers(ICollection<RoomImage> images)
    {
        var ordered = images.OrderBy(i => i.SortOrder).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].IsCover = i == 0;
            ordered[i].SortOrder = i;
        }
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
