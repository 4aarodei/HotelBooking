using HotelBooking.Application.Admin;
using HotelBooking.Application.Media;
using HotelBooking.ViewModels.Admin;
using HotelBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Web.Areas.Admin.Controllers;

public class RoomsController : AdminControllerBase
{
    private readonly ICreateRoomUseCase _createRoom;
    private readonly IUpdateRoomUseCase _updateRoom;
    private readonly IGetCreateRoomDetailsQuery _getCreateRoomDetails;
    private readonly IGetEditRoomDetailsQuery _getEditRoomDetails;
    private readonly RoomDraftImageUploadService _roomDraftImageUploadService;

    public RoomsController(
        ICreateRoomUseCase createRoom,
        IUpdateRoomUseCase updateRoom,
        IGetCreateRoomDetailsQuery getCreateRoomDetails,
        IGetEditRoomDetailsQuery getEditRoomDetails,
        RoomDraftImageUploadService roomDraftImageUploadService)
    {
        _createRoom = createRoom;
        _updateRoom = updateRoom;
        _getCreateRoomDetails = getCreateRoomDetails;
        _getEditRoomDetails = getEditRoomDetails;
        _roomDraftImageUploadService = roomDraftImageUploadService;
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid hotelId, CancellationToken ct)
    {
        var details = await _getCreateRoomDetails.ExecuteForHotelAsync(hotelId, ct);
        if (details is null)
        {
            return NotFound();
        }

        var model = MapRoomForm(details);
        model.DraftUploadId = Guid.NewGuid().ToString("N");
        return View("~/Areas/Admin/Views/Hotels/RoomForm.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminRoomFormViewModel model, CancellationToken ct)
    {
        var details = await _getCreateRoomDetails.ExecuteForHotelAsync(model.HotelId, ct);
        if (details is null)
        {
            return NotFound();
        }

        model.HotelName = details.HotelName;
        EnsureDraftUploadId(model);

        if (!ModelState.IsValid)
        {
            return View("~/Areas/Admin/Views/Hotels/RoomForm.cshtml", model);
        }

        try
        {
            var files = HotelsController.ToImageUploadFiles(model.Photos);
            var draftFiles = await ReadDraftFilesAsync(model.DraftUploadId, ct);
            files.AddRange(draftFiles);

            await _createRoom.ExecuteAsync(
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
            return RedirectToAction("Edit", "Hotels", new { id = model.HotelId });
        }
        catch (ImageUploadValidationException ex)
        {
            ModelState.AddModelError(nameof(model.Photos), ex.Message);
            return View("~/Areas/Admin/Views/Hotels/RoomForm.cshtml", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var details = await _getEditRoomDetails.ExecuteForRoomAsync(id, ct);
        if (details is null)
        {
            return NotFound();
        }

        return View("~/Areas/Admin/Views/Hotels/RoomForm.cshtml", MapRoomForm(details));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminRoomFormViewModel model, CancellationToken ct)
    {
        if (model.Id is null)
        {
            return BadRequest();
        }

        var details = await _getEditRoomDetails.ExecuteForRoomAsync(model.Id.Value, ct);
        if (details is null)
        {
            return NotFound();
        }

        model.HotelId = details.HotelId;
        model.HotelName = details.HotelName;

        if (!ModelState.IsValid)
        {
            return View("~/Areas/Admin/Views/Hotels/RoomForm.cshtml", ApplyRoomInput(MapRoomForm(details), model));
        }

        try
        {
            await _updateRoom.ExecuteAsync(
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
                    HotelsController.ToImageUploadFiles(model.Photos),
                    model.RemoveImageIds),
                ct);
        }
        catch (ImageUploadValidationException ex)
        {
            ModelState.AddModelError(nameof(model.Photos), ex.Message);
            return View("~/Areas/Admin/Views/Hotels/RoomForm.cshtml", ApplyRoomInput(MapRoomForm(details), model));
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "Room data was changed by another operation. Reload and try again.");

            var latest = await _getEditRoomDetails.ExecuteForRoomAsync(model.Id.Value, ct);
            if (latest is null)
            {
                return NotFound();
            }

            return View("~/Areas/Admin/Views/Hotels/RoomForm.cshtml", ApplyRoomInput(MapRoomForm(latest), model));
        }

        TempData["SuccessMessage"] = "Room updated.";
        return RedirectToAction("Edit", "Hotels", new { id = details.HotelId });
    }

    internal static AdminRoomFormViewModel MapRoomForm(AdminRoomFormDetails room)
    {
        return new AdminRoomFormViewModel
        {
            Id = room.Id,
            HotelId = room.HotelId,
            HotelName = room.HotelName,
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
                .Select(i => new AdminImageViewModel
                {
                    Id = i.Id,
                    Url = i.Url,
                    AltText = i.AltText,
                    Width = i.Width,
                    Height = i.Height,
                    IsCover = i.IsCover
                })
                .ToList()
        };
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

    private async Task<List<ImageUploadFile>> ReadDraftFilesAsync(string? draftUploadId, CancellationToken ct)
    {
        if (!RoomDraftPhotosController.TryParseDraftId(draftUploadId, out var draftId))
        {
            return [];
        }

        var files = await _roomDraftImageUploadService.GetDraftAsUploadFilesAsync(draftId, ct);
        return files.ToList();
    }

    private async Task DeleteDraftFilesAsync(string? draftUploadId, CancellationToken ct)
    {
        if (!RoomDraftPhotosController.TryParseDraftId(draftUploadId, out var draftId))
        {
            return;
        }

        await _roomDraftImageUploadService.DeleteDraftAsync(draftId, ct);
    }
}
