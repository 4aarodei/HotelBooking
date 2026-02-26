using HotelBooking.Domain.Entities.Bookings;
using HotelBooking.Domain.Entities.Hotels;
using HotelBooking.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Infrastructure.Data;

public static class IdentitySeeder
{
    public const string SuperAdminEmail = "superadmin@hotelbooking.demo";
    public const string AdminEmail = "admin@hotelbooking.demo";
    public const string UserEmail = "user@hotelbooking.demo";

    public const string DemoPassword = "Demo123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager);
        await SeedBookingStatusesAsync(dbContext);
        await SeedHotelsAsync(dbContext);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = ["SuperAdmin", "Admin", "User"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        await EnsureUserInRoleAsync(userManager, SuperAdminEmail, "SuperAdmin");
        await EnsureUserInRoleAsync(userManager, AdminEmail, "Admin");
        await EnsureUserInRoleAsync(userManager, UserEmail, "User");
    }

    private static async Task EnsureUserInRoleAsync(UserManager<ApplicationUser> userManager, string email, string role)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Не вдалося створити demo-користувача {email}: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }

    private static async Task SeedBookingStatusesAsync(ApplicationDbContext dbContext)
    {
        var statuses = new[]
        {
            new BookingStatus { Name = "Очікує підтвердження", BookingStatusCode = BookingStatusCodes.Pending },
            new BookingStatus { Name = "Підтверджено", BookingStatusCode = BookingStatusCodes.Confirmed },
            new BookingStatus { Name = "Скасовано", BookingStatusCode = BookingStatusCodes.Cancelled }
        };

        foreach (var status in statuses)
        {
            var exists = await dbContext.BookingStatuses
                .AnyAsync(x => x.BookingStatusCode == status.BookingStatusCode);

            if (!exists)
            {
                dbContext.BookingStatuses.Add(status);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedHotelsAsync(ApplicationDbContext dbContext)
    {
        if (await dbContext.Hotels.AnyAsync())
        {
            return;
        }

        var hotels = new List<Hotel>
        {
            new()
            {
                Name = "Kyiv River View Hotel",
                City = "Київ",
                Address = "Набережне шосе, 12",
                Description = "Сучасний готель у центрі Києва з видом на Дніпро.",
                Rooms = new List<Room>
                {
                    new() { Name = "Standard Double", Capacity = 2, PricePerNight = 2400, Quantity = 8, IsActive = true },
                    new() { Name = "Business Suite", Capacity = 3, PricePerNight = 3900, Quantity = 3, IsActive = true }
                }
            },
            new()
            {
                Name = "Lviv Old Town Residence",
                City = "Львів",
                Address = "вул. Вірменська, 8",
                Description = "Затишний бутик-готель поруч із площею Ринок.",
                Rooms = new List<Room>
                {
                    new() { Name = "Classic Room", Capacity = 2, PricePerNight = 2100, Quantity = 6, IsActive = true },
                    new() { Name = "Family Room", Capacity = 4, PricePerNight = 3300, Quantity = 2, IsActive = true }
                }
            },
            new()
            {
                Name = "Odesa Coastline Hotel",
                City = "Одеса",
                Address = "Французький бульвар, 29",
                Description = "Готель біля узбережжя для відпочинку та бізнес-подорожей.",
                Rooms = new List<Room>
                {
                    new() { Name = "Sea View Room", Capacity = 2, PricePerNight = 2800, Quantity = 5, IsActive = true },
                    new() { Name = "Deluxe Apartment", Capacity = 4, PricePerNight = 4500, Quantity = 2, IsActive = true }
                }
            }
        };

        await dbContext.Hotels.AddRangeAsync(hotels);
        await dbContext.SaveChangesAsync();
    }
}
