using Dapper;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Statistics;

namespace HotelBooking.Infrastructure.Dapper;

public class BookingStatisticsQuery : IBookingStatisticsQuery
{
    private readonly DapperConnectionFactory _connectionFactory;

    public BookingStatisticsQuery(DapperConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<BookingStatsDto>> GetByDateAsync(DateOnly from, DateOnly to)
    {
        const string sql = @"
    SELECT
        b.CheckIn AS [Date],
        COUNT(*)  AS [BookingsCount]
    FROM Bookings b
    WHERE b.CheckIn >= @From
      AND b.CheckIn <= @To
    GROUP BY b.CheckIn
    ORDER BY [Date];";

        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<BookingStatsDto>(sql, new { From = from, To = to });
        return rows.ToList();
    }
}
