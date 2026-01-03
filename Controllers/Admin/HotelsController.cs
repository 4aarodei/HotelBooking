using HotelBooking.Models.Hotels;
using HotelBooking.Services;
using HotelBooking.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Hotels")]
public class HotelsController : Controller
{
    private readonly HotelService _hotelService;

    public HotelsController(HotelService hotelService)
    {
        _hotelService = hotelService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var hotels = await _hotelService.GetAllAsync();

        var viewModel = hotels
            .Select(h => new AdminHotelListItemViewModel
            {
                Id = h.Id,
                Name = h.Name,
                City = h.City,
                Address = h.Address
            })
            .ToList();

        return View("~/Views/Admin/Hotels/Index.cshtml", viewModel);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View("~/Views/Admin/Hotels/Create.cshtml", new AdminHotelFormViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminHotelFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Hotels/Create.cshtml", model);
        }

        var hotel = new Hotel
        {
            Name = model.Name!,
            City = model.City!,
            Address = model.Address!,
            Description = model.Description
        };

        await _hotelService.AddAsync(hotel);

        TempData["SuccessMessage"] = "Готель успішно додано";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}/Edit")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var hotel = await _hotelService.GetByIdAsync(id);
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

        return View("~/Views/Admin/Hotels/Edit.cshtml", model);
    }

    [HttpPost("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminHotelFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Hotels/Edit.cshtml", model);
        }

        if (model.Id is null)
        {
            return BadRequest();
        }

        var hotel = await _hotelService.GetByIdAsync(model.Id.Value);
        if (hotel == null)
        {
            return NotFound();
        }

        hotel.Name = model.Name!;
        hotel.City = model.City!;
        hotel.Address = model.Address!;
        hotel.Description = model.Description;

        await _hotelService.UpdateAsync(hotel);

        TempData["SuccessMessage"] = "Дані готелю оновлено";
        return RedirectToAction(nameof(Index));
    }
}
