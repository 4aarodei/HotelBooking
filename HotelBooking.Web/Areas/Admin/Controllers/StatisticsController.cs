using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Areas.Admin.Controllers;

public class StatisticsController : AdminControllerBase
{
    private readonly IBookingStatisticsQuery _bookingStatisticsQuery;

    public StatisticsController(IBookingStatisticsQuery bookingStatisticsQuery)
    {
        _bookingStatisticsQuery = bookingStatisticsQuery;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var fromDate = from ?? today.AddDays(-7);
        var toDate = to ?? today;

        if (toDate < fromDate)
        {
            (fromDate, toDate) = (toDate, fromDate);
        }

        var stats = await _bookingStatisticsQuery.GetByDateAsync(fromDate, toDate);

        return View(stats);
    }
}
