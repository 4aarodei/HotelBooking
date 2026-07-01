using HotelBooking.Application.Admin;
using HotelBooking.Application.Media;
using HotelBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Areas.Admin.Controllers;

public class RoomDraftPhotosController : AdminControllerBase
{
    private readonly IAdminHotelExistsQuery _hotelExists;
    private readonly RoomDraftImageUploadService _roomDraftImageUploadService;

    public RoomDraftPhotosController(
        IAdminHotelExistsQuery hotelExists,
        RoomDraftImageUploadService roomDraftImageUploadService)
    {
        _hotelExists = hotelExists;
        _roomDraftImageUploadService = roomDraftImageUploadService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(Guid hotelId, string? draftUploadId, List<IFormFile>? photos, CancellationToken ct)
    {
        var exists = await _hotelExists.ExecuteForHotelAsync(hotelId, ct);
        if (!exists)
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
    public async Task<IActionResult> Discard(string? draftUploadId, CancellationToken ct)
    {
        if (!TryParseDraftId(draftUploadId, out var draftId))
        {
            return Ok();
        }

        await _roomDraftImageUploadService.DeleteDraftAsync(draftId, ct);
        return Ok();
    }

    internal static bool TryParseDraftId(string? value, out Guid draftId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            draftId = Guid.Empty;
            return false;
        }

        return Guid.TryParse(value, out draftId);
    }
}
