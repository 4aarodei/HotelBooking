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
                DATE(check_in) AS Date,
                COUNT(*) AS BookingsCount
            FROM bookings
            WHERE check_in BETWEEN @From AND @To
            GROUP BY DATE(check_in)
            ORDER BY Date;
            """;

        var connection = _connectionFactory.CreateConnection();

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
