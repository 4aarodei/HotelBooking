using HotelBooking.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<HotelService>();
        services.AddScoped<BookingService>();

        return services;
    }
}
