namespace HotelBooking.Application.Statistics;

public interface IBookingStatisticsQuery
{
    Task<List<BookingStatsDto>> GetByDateAsync(DateOnly from, DateOnly toDate);
}
