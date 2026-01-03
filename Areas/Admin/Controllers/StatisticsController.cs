using HotelBooking.Application.Statistics;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Areas.Admin.Controllers;

public class StatisticsController : AdminControllerBase
{
    private readonly IBookingStatisticsQuery _bookingStatisticsQuery;

    public StatisticsController(IBookingStatisticsQuery bookingStatisticsQuery)
    {
        _bookingStatisticsQuery = bookingStatisticsQuery;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        var today = DateTime.Today;

        var fromDate = (from ?? today.AddDays(-7)).Date;
        var toDate = (to ?? today).Date;

        if (toDate < fromDate)
        {
            (fromDate, toDate) = (toDate, fromDate);
        }

        var stats = await _bookingStatisticsQuery
            .GetByDateAsync(fromDate, toDate);

        return View(stats);
    }
}
