using HotelBooking.Application.Services;
using HotelBooking.Domain.Entities.Hotels;
using HotelBooking.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Areas.Admin.Controllers;

public class HotelsController : AdminControllerBase
{
    private readonly HotelService _hotelService;

    public HotelsController(HotelService hotelService)
    {
        _hotelService = hotelService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var hotels = await _hotelService.GetAllAsync(ct);

        var viewModel = hotels
            .Select(h => new AdminHotelListItemViewModel
            {
                Id = h.Id,
                Name = h.Name,
                City = h.City,
                Address = h.Address
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

        var hotel = new Hotel
        {
            Name = model.Name!,
            City = model.City!,
            Address = model.Address!,
            Description = model.Description
        };

        await _hotelService.AddAsync(hotel, ct);

        TempData["SuccessMessage"] = "Готель успішно додано";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var hotel = await _hotelService.GetByIdAsync(id, ct);
        if (hotel == null)
        {
            return NotFound();
        }

        var model = new AdminHotelFormViewModel
        {
            Id = hotel.Id,
            Name = hotel.Name,
            City = hotel.City,
            Address = hotel.Address,
            Description = hotel.Description
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminHotelFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.Id is null)
        {
            return BadRequest();
        }

        var hotel = await _hotelService.GetByIdAsync(model.Id.Value, ct);
        if (hotel == null)
        {
            return NotFound();
        }

        hotel.Name = model.Name!;
        hotel.City = model.City!;
        hotel.Address = model.Address!;
        hotel.Description = model.Description;

        await _hotelService.UpdateAsync(hotel, ct);

        TempData["SuccessMessage"] = "Дані готелю оновлено";
        return RedirectToAction(nameof(Index));
    }
}
