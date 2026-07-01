using HotelBooking.Application.Admin;
using HotelBooking.Application.Bookings;
using HotelBooking.Application.Common;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Media;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<HotelQueryCache>();
        services.AddScoped<IGetAvailableCitiesQuery, GetAvailableCitiesQuery>();
        services.AddScoped<ISearchAvailableHotelsQuery, SearchAvailableHotelsQuery>();
        services.AddScoped<IGetHotelDetailsWithAvailabilityQuery, GetHotelDetailsWithAvailabilityQuery>();
        services.AddScoped<IGetRoomDetailsWithAvailabilityQuery, GetRoomDetailsWithAvailabilityQuery>();
        services.AddScoped<IGetFeaturedHotelsQuery, GetFeaturedHotelsQuery>();
        services.AddScoped<IHotelService, HotelService>();
        services.AddScoped<AdminImageLifecycleService>();
        services.AddScoped<IAdminHotelCommandService, AdminHotelCommandService>();
        services.AddScoped<ICreateHotelUseCase, AdminHotelCommandService>();
        services.AddScoped<IUpdateHotelUseCase, AdminHotelCommandService>();
        services.AddScoped<IAdminRoomCommandService, AdminRoomCommandService>();
        services.AddScoped<ICreateRoomUseCase, AdminRoomCommandService>();
        services.AddScoped<IUpdateRoomUseCase, AdminRoomCommandService>();
        services.AddScoped<IAdminHotelQueryService, AdminHotelQueryService>();
        services.AddScoped<IGetAdminHotelListQuery, AdminHotelQueryService>();
        services.AddScoped<IGetAdminHotelEditDetailsQuery, AdminHotelQueryService>();
        services.AddScoped<IGetCreateRoomDetailsQuery, AdminHotelQueryService>();
        services.AddScoped<IGetEditRoomDetailsQuery, AdminHotelQueryService>();
        services.AddScoped<IAdminHotelExistsQuery, AdminHotelQueryService>();
        services.AddScoped<ICreateBookingUseCase, CreateBookingUseCase>();
        services.AddScoped<IGetUserBookingsUseCase, GetUserBookingsUseCase>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IImageProcessor, ImageProcessor>();

        return services;
    }
}
