using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Media;
using HotelBooking.Domain.Entities.Hotels;
using HotelBooking.ViewModels.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HotelBooking.Web.Areas.Admin.Controllers;

public class HotelsController : AdminControllerBase
{
    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IImageProcessor _imageProcessor;
    private readonly IImageStorage _imageStorage;
    private readonly ImageUploadOptions _imageOptions;
    private readonly ILogger<HotelsController> _logger;

    public HotelsController(
        IHotelRepository hotelRepository,
        IRoomRepository roomRepository,
        IImageProcessor imageProcessor,
        IImageStorage imageStorage,
        IOptions<ImageUploadOptions> imageOptions,
        ILogger<HotelsController> logger)
    {
        _hotelRepository = hotelRepository;
        _roomRepository = roomRepository;
        _imageProcessor = imageProcessor;
        _imageStorage = imageStorage;
        _imageOptions = imageOptions.Value;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var hotels = await _hotelRepository.GetAllAsync(ct);

        var viewModel = hotels
            .Select(h => new AdminHotelListItemViewModel
            {
                Id = h.Id,
                Name = h.Name,
                City = h.City,
                Address = h.Address,
                CoverImageUrl = GetCoverImageUrl(h.Images)
            })
            .ToList();

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new AdminHotelFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminHotelFormViewModel model, CancellationToken ct)
    {
        ValidateFileCount(model.Photos, nameof(model.Photos));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var hotel = new Hotel
        {
            Id = Guid.NewGuid(),
            Name = model.Name?.Trim() ?? string.Empty,
            City = model.City?.Trim() ?? string.Empty,
            Address = model.Address?.Trim() ?? string.Empty,
            Description = model.Description?.Trim()
        };

        var newImages = new List<HotelImage>();
        try
        {
            await AddHotelImagesAsync(hotel, model.Photos, newImages, ct);
        }
        catch (ImageUploadValidationException ex)
        {
            await CleanupHotelImagesAsync(newImages, ct);
            ModelState.AddModelError(nameof(model.Photos), ex.Message);
            return View(model);
        }

        try
        {
            await _hotelRepository.AddAsync(hotel, ct);
        }
        catch
        {
            await CleanupHotelImagesAsync(newImages, ct);
            throw;
        }

        TempData["SuccessMessage"] = "Hotel created.";
        return RedirectToAction(nameof(Edit), new { id = hotel.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var hotel = await _hotelRepository.GetByIdWithImagesAsync(id, ct);
        if (hotel == null)
        {
            return NotFound();
        }

        return View(MapHotelForm(hotel));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminHotelFormViewModel model, CancellationToken ct)
    {
        ValidateFileCount(model.Photos, nameof(model.Photos));

        if (model.Id is null)
        {
            return BadRequest();
        }

        var hotel = await _hotelRepository.GetByIdWithImagesAsync(model.Id.Value, ct);
        if (hotel == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(ApplyHotelInput(MapHotelForm(hotel), model));
        }

        hotel.Name = model.Name?.Trim() ?? string.Empty;
        hotel.City = model.City?.Trim() ?? string.Empty;
        hotel.Address = model.Address?.Trim() ?? string.Empty;
        hotel.Description = model.Description?.Trim();

        var newImages = new List<HotelImage>();
        try
        {
            await AddHotelImagesAsync(hotel, model.Photos, newImages, ct);
        }
        catch (ImageUploadValidationException ex)
        {
            await CleanupHotelImagesAsync(newImages, ct);
            ModelState.AddModelError(nameof(model.Photos), ex.Message);
            return View(ApplyHotelInput(MapHotelForm(hotel), model));
        }

        var removedImages = RemoveHotelImages(hotel, model.RemoveImageIds);
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

        TempData["SuccessMessage"] = "Hotel updated.";
        return RedirectToAction(nameof(Edit), new { id = hotel.Id });
    }

    [HttpGet]
    public async Task<IActionResult> CreateRoom(Guid hotelId, CancellationToken ct)
    {
        var hotel = await _hotelRepository.GetByIdAsync(hotelId, ct);
        if (hotel is null)
        {
            return NotFound();
        }

        return View("RoomForm", new AdminRoomFormViewModel
        {
            HotelId = hotel.Id,
            HotelName = hotel.Name,
            PricePerNight = 1000m,
            Quantity = 1,
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoom(AdminRoomFormViewModel model, CancellationToken ct)
    {
        ValidateFileCount(model.Photos, nameof(model.Photos));

        var hotel = await _hotelRepository.GetByIdAsync(model.HotelId, ct);
        if (hotel is null)
        {
            return NotFound();
        }

        model.HotelName = hotel.Name;

        if (!ModelState.IsValid)
        {
            return View("RoomForm", model);
        }

        var room = new Room
        {
            Id = Guid.NewGuid(),
            HotelId = model.HotelId,
            Name = model.Name?.Trim() ?? string.Empty,
            Capacity = model.Capacity,
            PricePerNight = model.PricePerNight,
            Quantity = model.Quantity,
            IsActive = model.IsActive
        };

        var newImages = new List<RoomImage>();
        try
        {
            await AddRoomImagesAsync(room, model.Photos, newImages, ct);
            NormalizeRoomCovers(room.Images);
        }
        catch (ImageUploadValidationException ex)
        {
            await CleanupRoomImagesAsync(newImages, ct);
            ModelState.AddModelError(nameof(model.Photos), ex.Message);
            return View("RoomForm", model);
        }

        try
        {
            await _roomRepository.AddAsync(room, ct);
        }
        catch
        {
            await CleanupRoomImagesAsync(newImages, ct);
            throw;
        }

        TempData["SuccessMessage"] = "Room created.";
        return RedirectToAction(nameof(Edit), new { id = model.HotelId });
    }

    [HttpGet]
    public async Task<IActionResult> EditRoom(Guid id, CancellationToken ct)
    {
        var room = await _roomRepository.GetByIdWithImagesAsync(id, ct);
        if (room is null)
        {
            return NotFound();
        }

        var hotel = await _hotelRepository.GetByIdAsync(room.HotelId, ct);
        if (hotel is null)
        {
            return NotFound();
        }

        return View("RoomForm", MapRoomForm(room, hotel.Name));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRoom(AdminRoomFormViewModel model, CancellationToken ct)
    {
        ValidateFileCount(model.Photos, nameof(model.Photos));

        if (model.Id is null)
        {
            return BadRequest();
        }

        var room = await _roomRepository.GetByIdWithImagesAsync(model.Id.Value, ct);
        if (room is null)
        {
            return NotFound();
        }

        var hotel = await _hotelRepository.GetByIdAsync(room.HotelId, ct);
        if (hotel is null)
        {
            return NotFound();
        }

        model.HotelId = room.HotelId;
        model.HotelName = hotel.Name;

        if (!ModelState.IsValid)
        {
            return View("RoomForm", ApplyRoomInput(MapRoomForm(room, hotel.Name), model));
        }

        room.Name = model.Name?.Trim() ?? string.Empty;
        room.Capacity = model.Capacity;
        room.PricePerNight = model.PricePerNight;
        room.Quantity = model.Quantity;
        room.IsActive = model.IsActive;

        var newImages = new List<RoomImage>();
        try
        {
            await AddRoomImagesAsync(room, model.Photos, newImages, ct);
        }
        catch (ImageUploadValidationException ex)
        {
            await CleanupRoomImagesAsync(newImages, ct);
            ModelState.AddModelError(nameof(model.Photos), ex.Message);
            return View("RoomForm", ApplyRoomInput(MapRoomForm(room, hotel.Name), model));
        }

        var removedImages = RemoveRoomImages(room, model.RemoveImageIds);
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

        TempData["SuccessMessage"] = "Room updated.";
        return RedirectToAction(nameof(Edit), new { id = room.HotelId });
    }

    private static AdminHotelFormViewModel MapHotelForm(Hotel hotel)
    {
        return new AdminHotelFormViewModel
        {
            Id = hotel.Id,
            Name = hotel.Name,
            City = hotel.City,
            Address = hotel.Address,
            Description = hotel.Description,
            ExistingImages = hotel.Images
                .OrderByDescending(i => i.IsCover)
                .ThenBy(i => i.SortOrder)
                .Select(i => new AdminImageViewModel
                {
                    Id = i.Id,
                    Url = i.Url,
                    AltText = i.AltText ?? hotel.Name,
                    IsCover = i.IsCover
                })
                .ToList(),
            Rooms = hotel.Rooms
                .OrderBy(r => r.PricePerNight)
                .Select(r => new AdminRoomListItemViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Capacity = r.Capacity,
                    Quantity = r.Quantity,
                    PricePerNight = r.PricePerNight,
                    IsActive = r.IsActive,
                    CoverImageUrl = GetCoverImageUrl(r.Images)
                })
                .ToList()
        };
    }

    private static AdminRoomFormViewModel MapRoomForm(Room room, string hotelName)
    {
        return new AdminRoomFormViewModel
        {
            Id = room.Id,
            HotelId = room.HotelId,
            HotelName = hotelName,
            Name = room.Name,
            Capacity = room.Capacity,
            PricePerNight = room.PricePerNight,
            Quantity = room.Quantity,
            IsActive = room.IsActive,
            ExistingImages = room.Images
                .OrderByDescending(i => i.IsCover)
                .ThenBy(i => i.SortOrder)
                .Select(i => new AdminImageViewModel
                {
                    Id = i.Id,
                    Url = i.Url,
                    AltText = i.AltText ?? room.Name,
                    IsCover = i.IsCover
                })
                .ToList()
        };
    }

    private static AdminHotelFormViewModel ApplyHotelInput(AdminHotelFormViewModel target, AdminHotelFormViewModel source)
    {
        target.Name = source.Name;
        target.City = source.City;
        target.Address = source.Address;
        target.Description = source.Description;
        return target;
    }

    private static AdminRoomFormViewModel ApplyRoomInput(AdminRoomFormViewModel target, AdminRoomFormViewModel source)
    {
        target.Name = source.Name;
        target.Capacity = source.Capacity;
        target.PricePerNight = source.PricePerNight;
        target.Quantity = source.Quantity;
        target.IsActive = source.IsActive;
        return target;
    }

    private async Task AddHotelImagesAsync(Hotel hotel, IFormFileCollection? files, List<HotelImage> addedImages, CancellationToken ct)
    {
        if (files is null || files.Count == 0)
        {
            return;
        }

        var sortOrder = hotel.Images.Count == 0 ? 0 : hotel.Images.Max(i => i.SortOrder) + 1;
        foreach (var file in files.Where(f => f.Length > 0))
        {
            using var processed = await _imageProcessor.ProcessAsync(ToImageUploadFile(file), ct);
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

    private async Task AddRoomImagesAsync(Room room, IFormFileCollection? files, List<RoomImage> addedImages, CancellationToken ct)
    {
        if (files is null || files.Count == 0)
        {
            return;
        }

        var sortOrder = room.Images.Count == 0 ? 0 : room.Images.Max(i => i.SortOrder) + 1;
        foreach (var file in files.Where(f => f.Length > 0))
        {
            using var processed = await _imageProcessor.ProcessAsync(ToImageUploadFile(file), ct);
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

    private List<HotelImage> RemoveHotelImages(Hotel hotel, IEnumerable<Guid> imageIds)
    {
        var ids = imageIds.ToHashSet();
        var removed = hotel.Images.Where(i => ids.Contains(i.Id)).ToList();
        foreach (var image in removed)
        {
            hotel.Images.Remove(image);
            _logger.LogInformation("Admin removed hotel image metadata. HotelId: {HotelId}, ImageId: {ImageId}, StorageKey: {StorageKey}",
                hotel.Id,
                image.Id,
                image.StorageKey);
        }

        return removed;
    }

    private List<RoomImage> RemoveRoomImages(Room room, IEnumerable<Guid> imageIds)
    {
        var ids = imageIds.ToHashSet();
        var removed = room.Images.Where(i => ids.Contains(i.Id)).ToList();
        foreach (var image in removed)
        {
            room.Images.Remove(image);
            _logger.LogInformation("Admin removed room image metadata. RoomId: {RoomId}, ImageId: {ImageId}, StorageKey: {StorageKey}",
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
            _logger.LogInformation("Deleted image blob. Scope: {Scope}, ImageId: {ImageId}, StorageKey: {StorageKey}",
                scope,
                imageId,
                storageKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Image blob delete failed and should be retried. Scope: {Scope}, ImageId: {ImageId}, StorageKey: {StorageKey}",
                scope,
                imageId,
                storageKey);
        }
    }

    private void ValidateFileCount(IFormFileCollection? files, string modelKey)
    {
        var count = files?.Count(f => f.Length > 0) ?? 0;
        if (count > _imageOptions.MaxFilesPerUpload)
        {
            ModelState.AddModelError(modelKey, $"Upload at most {_imageOptions.MaxFilesPerUpload} images at a time.");
        }
    }

    private static ImageUploadFile ToImageUploadFile(IFormFile file)
    {
        return new ImageUploadFile(
            file.FileName,
            file.ContentType,
            file.Length,
            file.OpenReadStream);
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

    private static string? GetCoverImageUrl(IEnumerable<HotelImage> images)
    {
        return images
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.SortOrder)
            .Select(i => i.Url)
            .FirstOrDefault();
    }

    private static string? GetCoverImageUrl(IEnumerable<RoomImage> images)
    {
        return images
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.SortOrder)
            .Select(i => i.Url)
            .FirstOrDefault();
    }
}
