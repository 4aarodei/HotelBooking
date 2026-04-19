using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure.Dapper;
using HotelBooking.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IHotelRepository, HotelRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        services.AddScoped<DapperConnectionFactory>();
        services.AddScoped<IBookingStatisticsQuery, BookingStatisticsQuery>();

        return services;
    }
}
