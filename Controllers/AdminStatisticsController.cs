using HotelBooking.Application.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminStatisticsController : Controller
{
    private readonly IBookingStatisticsQuery _bookingStatisticsQuery;

    public AdminStatisticsController(
        IBookingStatisticsQuery bookingStatisticsQuery)
    {
        _bookingStatisticsQuery = bookingStatisticsQuery;
    }

    // GET: /admin/statistics
    // GET: /admin/statistics?from=2025-01-01&to=2025-01-31
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        // значення за замовчуванням
        var today = DateTime.Today;

        var fromDate = (from ?? today.AddDays(-14)).Date;
        var toDate = (to ?? today).Date;

        // захист від некоректного діапазону
        if (toDate < fromDate)
        {
            (fromDate, toDate) = (toDate, fromDate);
        }


        var stats = await _bookingStatisticsQuery
            .GetByDateAsync(fromDate, toDate);

        // передаємо дані напряму у View
        return View(stats);
    }
}
