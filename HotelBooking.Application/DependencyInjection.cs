using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Media;
using HotelBooking.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IHotelService, HotelService>();
        services.AddScoped<IAdminHotelManagementService, AdminHotelManagementService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IImageProcessor, ImageProcessor>();

        return services;
    }
}
