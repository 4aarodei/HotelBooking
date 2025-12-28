using HotelBooking.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace HotelBooking.Data.IdentitySeeder;

public static class IdentitySeeder
{
    private const string SuperAdminEmail = "Admin@testAkk"; // ← ТУТ СВІЙ EMAIL

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Roles
        string[] roles = { "SuperAdmin", "Admin", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. SuperAdmin
        var superAdmin = await userManager.FindByEmailAsync(SuperAdminEmail);
        if (superAdmin != null)
        {
            if (!await userManager.IsInRoleAsync(superAdmin, "SuperAdmin"))
            {
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
            }
        }
    }
}
