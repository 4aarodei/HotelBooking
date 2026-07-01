using HotelBooking.Application.Security;
using HotelBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Infrastructure.Data;

public static class IdentitySeeder
{
    private const string DefaultSuperAdminEmail = "admin@hotelbooking.local";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(IdentitySeeder));
        var configuration = services.GetRequiredService<IConfiguration>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = [AppRoles.SuperAdmin, AppRoles.Admin, AppRoles.User];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (result.Succeeded)
                    logger.LogInformation("Role '{Role}' created", role);
                else
                    logger.LogWarning("Failed to create role '{Role}': {Errors}", role,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        var superAdminEmail = configuration["AdminSeed:Email"] ?? DefaultSuperAdminEmail;
        var superAdminPassword = configuration["AdminSeed:Password"];

        var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
        if (superAdmin is null)
        {
            if (string.IsNullOrWhiteSpace(superAdminPassword))
            {
                logger.LogWarning("SuperAdmin user was not seeded because AdminSeed:Password is not configured.");
                return;
            }

            superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin"
            };

            var createResult = await userManager.CreateAsync(superAdmin, superAdminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, AppRoles.SuperAdmin);
                logger.LogInformation("SuperAdmin seeded: {Email}", superAdminEmail);
            }
            else
            {
                logger.LogWarning("Failed to seed SuperAdmin: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }
        else if (!await userManager.IsInRoleAsync(superAdmin, AppRoles.SuperAdmin))
        {
            await userManager.AddToRoleAsync(superAdmin, AppRoles.SuperAdmin);
        }
    }
}
