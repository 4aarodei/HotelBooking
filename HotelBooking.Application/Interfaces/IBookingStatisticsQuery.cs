using HotelBooking.Application.Statistics;

namespace HotelBooking.Application.Interfaces;

public interface IBookingStatisticsQuery
{
    Task<List<BookingStatsDto>> GetByDateAsync(DateOnly from, DateOnly to);
}
