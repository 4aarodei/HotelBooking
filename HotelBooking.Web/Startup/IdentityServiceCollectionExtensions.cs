using HotelBooking.Domain.Entities.Identity;
using HotelBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace HotelBooking.Web.Startup;

internal static class IdentityServiceCollectionExtensions
{
    // Identity defaults for sign-in, unique email, and lockout.
    public static void AddHotelBookingIdentity(this IServiceCollection services, bool requireConfirmedAccount)
    {
        services
            .AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = requireConfirmedAccount;
                options.User.RequireUniqueEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
    }
}
