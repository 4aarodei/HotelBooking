using Dapper;
using HotelBooking.Infrastructure.Dapper.Base;

namespace HotelBooking.Application.Statistics;

public interface IBookingStatisticsQuery
{
    Task<List<BookingStatsDto>> GetByDateAsync(
        DateTime from,
        DateTime to);
}

public class BookingStatisticsQuery : IBookingStatisticsQuery
{
    private readonly DapperConnectionFactory _connectionFactory;

    public BookingStatisticsQuery(DapperConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<BookingStatsDto>> GetByDateAsync(
        DateTime from,
        DateTime to)
    {
        const string sql = """
    SELECT
        CAST(b.CheckIn AS date) AS [Date],
        COUNT(*) AS BookingsCount
    FROM Bookings b
    WHERE b.CheckIn >= @From
      AND b.CheckIn < DATEADD(day, 1, @To)
    GROUP BY CAST(b.CheckIn AS date)
    ORDER BY [Date];
    """;


        using var connection = _connectionFactory.CreateConnection();

        var result = await connection.QueryAsync<BookingStatsDto>(
            sql,
            new
            {
                From = from,
                To = to
            });

        return result.ToList();
    }

}
