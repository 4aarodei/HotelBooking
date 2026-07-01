using HotelBooking.Application.Common;
using HotelBooking.Application.Statistics;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Areas.Admin.Controllers;

public class StatisticsController : AdminControllerBase
{
    private readonly IBookingStatisticsQuery _bookingStatisticsQuery;
    private readonly IClock _clock;

    public StatisticsController(IBookingStatisticsQuery bookingStatisticsQuery, IClock clock)
    {
        _bookingStatisticsQuery = bookingStatisticsQuery;
        _clock = clock;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        var today = _clock.Today;

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
