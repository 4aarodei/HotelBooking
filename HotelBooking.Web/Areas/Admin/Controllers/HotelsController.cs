using HotelBooking.Application.Admin;
using HotelBooking.Application.Media;
using HotelBooking.ViewModels.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Web.Areas.Admin.Controllers;

public class HotelsController : AdminControllerBase
{
    private readonly ICreateHotelUseCase _createHotel;
    private readonly IUpdateHotelUseCase _updateHotel;
    private readonly IGetAdminHotelListQuery _getHotelList;
    private readonly IGetAdminHotelEditDetailsQuery _getHotelEditDetails;

    public HotelsController(
        ICreateHotelUseCase createHotel,
        IUpdateHotelUseCase updateHotel,
        IGetAdminHotelListQuery getHotelList,
        IGetAdminHotelEditDetailsQuery getHotelEditDetails)
    {
        _createHotel = createHotel;
        _updateHotel = updateHotel;
        _getHotelList = getHotelList;
        _getHotelEditDetails = getHotelEditDetails;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var hotels = await _getHotelList.ExecuteAsync(ct);

        var viewModel = hotels
            .Select(h => new AdminHotelListItemViewModel
            {
                Id = h.Id,
                Name = h.Name,
                City = h.City,
                Address = h.Address,
                CoverImageUrl = h.CoverImageUrl,
                CoverImageWidth = h.CoverImageWidth,
                CoverImageHeight = h.CoverImageHeight
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
            var hotelId = await _createHotel.ExecuteAsync(
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
        var hotel = await _getHotelEditDetails.ExecuteForHotelAsync(id, ct);
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

        var hotel = await _getHotelEditDetails.ExecuteForHotelAsync(model.Id.Value, ct);
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
            await _updateHotel.ExecuteAsync(
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
            var latest = await _getHotelEditDetails.ExecuteForHotelAsync(model.Id.Value, ct);
            if (latest is null)
            {
                return NotFound();
            }

            return View(ApplyHotelInput(MapHotelForm(latest), model));
        }

        TempData["SuccessMessage"] = "Hotel updated.";
        return RedirectToAction(nameof(Edit), new { id = hotel.Id });
    }

    internal static AdminHotelFormViewModel MapHotelForm(AdminHotelEditDetails hotel)
    {
        return new AdminHotelFormViewModel
        {
            Id = hotel.Id,
            Name = hotel.Name,
            City = hotel.City,
            Address = hotel.Address,
            Description = hotel.Description,
            ExistingImages = hotel.Images
                .Select(i => new AdminImageViewModel
                {
                    Id = i.Id,
                    Url = i.Url,
                    AltText = i.AltText,
                    Width = i.Width,
                    Height = i.Height,
                    IsCover = i.IsCover
                })
                .ToList(),
            Rooms = hotel.Rooms
                .Select(r => new AdminRoomListItemViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Capacity = r.Capacity,
                    Quantity = r.Quantity,
                    PricePerNight = r.PricePerNight,
                    IsActive = r.IsActive,
                    CoverImageUrl = r.CoverImageUrl,
                    CoverImageWidth = r.CoverImageWidth,
                    CoverImageHeight = r.CoverImageHeight
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

    internal static List<ImageUploadFile> ToImageUploadFiles(IEnumerable<IFormFile>? files)
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
}
