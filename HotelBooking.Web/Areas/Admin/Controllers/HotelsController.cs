using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Hotels;
using HotelBooking.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Areas.Admin.Controllers;

public class HotelsController : AdminControllerBase
{
    private readonly IHotelRepository _hotelRepository;

    public HotelsController(IHotelRepository hotelRepository)
    {
        _hotelRepository = hotelRepository;
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
            Name = model.Name?.Trim() ?? string.Empty,
            City = model.City?.Trim() ?? string.Empty,
            Address = model.Address?.Trim() ?? string.Empty,
            Description = model.Description?.Trim()
        };

        await _hotelRepository.AddAsync(hotel, ct);

        TempData["SuccessMessage"] = "Готель успішно додано";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var hotel = await _hotelRepository.GetByIdAsync(id, ct);
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

        var hotel = await _hotelRepository.GetByIdAsync(model.Id.Value, ct);
        if (hotel == null)
        {
            return NotFound();
        }

        hotel.Name = model.Name?.Trim() ?? string.Empty;
        hotel.City = model.City?.Trim() ?? string.Empty;
        hotel.Address = model.Address?.Trim() ?? string.Empty;
        hotel.Description = model.Description?.Trim();

        await _hotelRepository.UpdateAsync(hotel, ct);

        TempData["SuccessMessage"] = "Дані готелю оновлено";
        return RedirectToAction(nameof(Index));
    }
}
