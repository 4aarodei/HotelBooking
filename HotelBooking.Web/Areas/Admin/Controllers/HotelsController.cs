using HotelBooking.Application.Hotels;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Media;
using HotelBooking.Domain.Entities.Hotels;
using HotelBooking.ViewModels.Admin;
using HotelBooking.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Web.Areas.Admin.Controllers;

public class HotelsController : AdminControllerBase
{
    private readonly IAdminHotelManagementService _adminHotelManagementService;
    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly RoomDraftImageUploadService _roomDraftImageUploadService;

    public HotelsController(
        IAdminHotelManagementService adminHotelManagementService,
        IHotelRepository hotelRepository,
        IRoomRepository roomRepository,
        RoomDraftImageUploadService roomDraftImageUploadService)
    {
        _adminHotelManagementService = adminHotelManagementService;
        _hotelRepository = hotelRepository;
        _roomRepository = roomRepository;
        _roomDraftImageUploadService = roomDraftImageUploadService;
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
                CoverImageUrl = GetCoverImage(h.Images)?.Url,
                CoverImageWidth = GetCoverImage(h.Images)?.Width,
                CoverImageHeight = GetCoverImage(h.Images)?.Height
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
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var hotelId = await _adminHotelManagementService.CreateHotelAsync(
                new CreateHotelCommand(
                    model.Name ?? string.Empty,
                    model.City ?? string.Empty,
                    model.Address ?? string.Empty,
                    model.Description,
                    ToImageUploadFiles(model.Photos)),
                ct);

            TempData["SuccessMessage"] = "Hotel created.";
            return RedirectToAction(nameof(Edit), new { id = hotelId });
        }
        catch (ImageUploadValidationException ex)
        {
            ModelState.AddModelError(nameof(model.Photos), ex.Message);
            return View(model);
        }
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

        try
        {
            await _adminHotelManagementService.UpdateHotelAsync(
                new UpdateHotelCommand(
                    model.Id.Value,
                    model.Name ?? string.Empty,
                    model.City ?? string.Empty,
                    model.Address ?? string.Empty,
                    model.Description,
                    ToImageUploadFiles(model.Photos),
                    model.RemoveImageIds),
                ct);
        }
        catch (ImageUploadValidationException ex)
        {
            ModelState.AddModelError(nameof(model.Photos), ex.Message);
            return View(ApplyHotelInput(MapHotelForm(hotel), model));
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "Hotel data was changed by another operation. Reload and try again.");
            var latest = await _hotelRepository.GetByIdWithImagesAsync(model.Id.Value, ct);
            if (latest is null)
            {
                return NotFound();
            }

            return View(ApplyHotelInput(MapHotelForm(latest), model));
        }

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
            DraftUploadId = Guid.NewGuid().ToString("N"),
            PricePerNight = 1000m,
            Quantity = 1,
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoom(AdminRoomFormViewModel model, CancellationToken ct)
    {
        var hotel = await _hotelRepository.GetByIdAsync(model.HotelId, ct);
        if (hotel is null)
        {
            return NotFound();
        }

        model.HotelName = hotel.Name;
        EnsureDraftUploadId(model);

        if (!ModelState.IsValid)
        {
            return View("RoomForm", model);
        }

        try
        {
            var files = ToImageUploadFiles(model.Photos);
            var draftFiles = await ReadDraftFilesAsync(model.DraftUploadId, ct);
            files.AddRange(draftFiles);

            await _adminHotelManagementService.CreateRoomAsync(
                new CreateRoomCommand(
                    model.HotelId,
                    model.Name ?? string.Empty,
                    model.Description,
                    model.Amenities,
                    model.Capacity,
                    model.PricePerNight,
                    model.Quantity,
                    model.IncludesBreakfast,
                    model.HasPrivateBathroom,
                    model.HasSaunaAccess,
                    model.HasBalcony,
                    model.HasWorkspace,
                    model.HasAirConditioning,
                    model.IsActive,
                    files),
                ct);

            await DeleteDraftFilesAsync(model.DraftUploadId, ct);
            TempData["SuccessMessage"] = "Room created.";
            return RedirectToAction(nameof(Edit), new { id = model.HotelId });
        }
        catch (ImageUploadValidationException ex)
        {
            ModelState.AddModelError(nameof(model.Photos), ex.Message);
            return View("RoomForm", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadRoomDraftPhotos(Guid hotelId, string? draftUploadId, List<IFormFile>? photos, CancellationToken ct)
    {
        var hotelExists = await _hotelRepository.GetByIdAsync(hotelId, ct);
        if (hotelExists is null)
        {
            return NotFound();
        }

        if (!TryParseDraftId(draftUploadId, out var draftId))
        {
            return BadRequest("Invalid draft id.");
        }

        var files = photos ?? [];
        if (files.Count == 0)
        {
            return Ok(new { uploaded = Array.Empty<object>(), count = 0 });
        }

        try
        {
            var uploaded = await _roomDraftImageUploadService.SaveDraftFilesAsync(draftId, files, ct);
            return Ok(new
            {
                uploaded = uploaded.Select(x => new { fileName = x.FileName, url = x.Url }),
                count = uploaded.Count
            });
        }
        catch (ImageUploadValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DiscardRoomDraftPhotos(string? draftUploadId, CancellationToken ct)
    {
        if (!TryParseDraftId(draftUploadId, out var draftId))
        {
            return Ok();
        }

        await _roomDraftImageUploadService.DeleteDraftAsync(draftId, ct);
        return Ok();
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

        try
        {
            await _adminHotelManagementService.UpdateRoomAsync(
                new UpdateRoomCommand(
                    model.Id.Value,
                    model.Name ?? string.Empty,
                    model.Description,
                    model.Amenities,
                    model.Capacity,
                    model.PricePerNight,
                    model.Quantity,
                    model.IncludesBreakfast,
                    model.HasPrivateBathroom,
                    model.HasSaunaAccess,
                    model.HasBalcony,
                    model.HasWorkspace,
                    model.HasAirConditioning,
                    model.IsActive,
                    ToImageUploadFiles(model.Photos),
                    model.RemoveImageIds),
                ct);
        }
        catch (ImageUploadValidationException ex)
        {
            ModelState.AddModelError(nameof(model.Photos), ex.Message);
            return View("RoomForm", ApplyRoomInput(MapRoomForm(room, hotel.Name), model));
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "Room data was changed by another operation. Reload and try again.");

            var latestRoom = await _roomRepository.GetByIdWithImagesAsync(model.Id.Value, ct);
            if (latestRoom is null)
            {
                return NotFound();
            }

            var latestHotel = await _hotelRepository.GetByIdAsync(latestRoom.HotelId, ct);
            if (latestHotel is null)
            {
                return NotFound();
            }

            return View("RoomForm", ApplyRoomInput(MapRoomForm(latestRoom, latestHotel.Name), model));
        }

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
                    Width = i.Width,
                    Height = i.Height,
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
                    CoverImageUrl = GetCoverImage(r.Images)?.Url,
                    CoverImageWidth = GetCoverImage(r.Images)?.Width,
                    CoverImageHeight = GetCoverImage(r.Images)?.Height
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
            DraftUploadId = string.Empty,
            Name = room.Name,
            Description = room.Description,
            Amenities = room.Amenities,
            Capacity = room.Capacity,
            PricePerNight = room.PricePerNight,
            Quantity = room.Quantity,
            IncludesBreakfast = room.IncludesBreakfast,
            HasPrivateBathroom = room.HasPrivateBathroom,
            HasSaunaAccess = room.HasSaunaAccess,
            HasBalcony = room.HasBalcony,
            HasWorkspace = room.HasWorkspace,
            HasAirConditioning = room.HasAirConditioning,
            IsActive = room.IsActive,
            ExistingImages = room.Images
                .OrderByDescending(i => i.IsCover)
                .ThenBy(i => i.SortOrder)
                .Select(i => new AdminImageViewModel
                {
                    Id = i.Id,
                    Url = i.Url,
                    AltText = i.AltText ?? room.Name,
                    Width = i.Width,
                    Height = i.Height,
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
        target.DraftUploadId = source.DraftUploadId;
        target.Description = source.Description;
        target.Amenities = source.Amenities;
        target.Capacity = source.Capacity;
        target.PricePerNight = source.PricePerNight;
        target.Quantity = source.Quantity;
        target.IncludesBreakfast = source.IncludesBreakfast;
        target.HasPrivateBathroom = source.HasPrivateBathroom;
        target.HasSaunaAccess = source.HasSaunaAccess;
        target.HasBalcony = source.HasBalcony;
        target.HasWorkspace = source.HasWorkspace;
        target.HasAirConditioning = source.HasAirConditioning;
        target.IsActive = source.IsActive;
        return target;
    }

    private static void EnsureDraftUploadId(AdminRoomFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.DraftUploadId) || !Guid.TryParse(model.DraftUploadId, out _))
        {
            model.DraftUploadId = Guid.NewGuid().ToString("N");
        }
    }

    private static bool TryParseDraftId(string? value, out Guid draftId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            draftId = Guid.Empty;
            return false;
        }

        return Guid.TryParse(value, out draftId);
    }

    private async Task<List<ImageUploadFile>> ReadDraftFilesAsync(string? draftUploadId, CancellationToken ct)
    {
        if (!TryParseDraftId(draftUploadId, out var draftId))
        {
            return [];
        }

        var files = await _roomDraftImageUploadService.GetDraftAsUploadFilesAsync(draftId, ct);
        return files.ToList();
    }

    private async Task DeleteDraftFilesAsync(string? draftUploadId, CancellationToken ct)
    {
        if (!TryParseDraftId(draftUploadId, out var draftId))
        {
            return;
        }

        await _roomDraftImageUploadService.DeleteDraftAsync(draftId, ct);
    }

    private static List<ImageUploadFile> ToImageUploadFiles(IEnumerable<IFormFile>? files)
    {
        return files?
            .Where(file => file.Length > 0)
            .Select(file => new ImageUploadFile(
                file.FileName,
                file.ContentType,
                file.Length,
                file.OpenReadStream))
            .ToList()
            ?? [];
    }

    private static HotelImage? GetCoverImage(IEnumerable<HotelImage> images)
    {
        return images
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.SortOrder)
            .FirstOrDefault();
    }

    private static RoomImage? GetCoverImage(IEnumerable<RoomImage> images)
    {
        return images
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.SortOrder)
            .FirstOrDefault();
    }
}
