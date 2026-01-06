using HotelBooking.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace HotelBooking.Infrastructure.Data;

public static class IdentitySeeder
{
    private const string SuperAdminEmail = "Admin@testAkk";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = { "SuperAdmin", "Admin", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var superAdmin = await userManager.FindByEmailAsync(SuperAdminEmail);
        if (superAdmin != null && !await userManager.IsInRoleAsync(superAdmin, "SuperAdmin"))
        {
            await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
        }
    }
}
