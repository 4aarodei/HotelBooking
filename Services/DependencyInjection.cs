using HotelBooking.Application.Statistics;
using HotelBooking.Infrastructure.Dapper.Base;
using HotelBooking.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IHotelService, HotelService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<BookingService, BookingService>();
        services.AddScoped<BookingStatusService, BookingStatusService>();

        // Dapper
        services.AddScoped<DapperConnectionFactory>();
        services.AddScoped<IBookingStatisticsQuery, BookingStatisticsQuery>();

        return services;
    }
}
