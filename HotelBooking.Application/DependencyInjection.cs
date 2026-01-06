using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<HotelService>();
        services.AddScoped<RoomService>();
        services.AddScoped<BookingService>();
        services.AddScoped<BookingStatusService>();

        return services;
    }
}
