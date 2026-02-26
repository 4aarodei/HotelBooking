using HotelBooking.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Infrastructure.Data;

public static class IdentitySeeder
{
    private const string SuperAdminEmail = "admin@hotelbooking.local";
    private const string SuperAdminPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(IdentitySeeder));
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

        var superAdmin = await userManager.FindByEmailAsync(SuperAdminEmail);
        if (superAdmin is null)
        {
            superAdmin = new ApplicationUser
            {
                UserName = SuperAdminEmail,
                Email = SuperAdminEmail,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin"
            };

            var createResult = await userManager.CreateAsync(superAdmin, SuperAdminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, AppRoles.SuperAdmin);
                logger.LogInformation("SuperAdmin seeded: {Email}", SuperAdminEmail);
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
