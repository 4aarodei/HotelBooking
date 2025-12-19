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
        services.AddScoped<IBookingService, BookingService>();

        // Dapper
        services.AddScoped<DapperConnectionFactory>();
        services.AddScoped<IBookingStatisticsQuery, BookingStatisticsQuery>();

        return services;
    }
}
