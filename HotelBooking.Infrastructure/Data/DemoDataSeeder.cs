using HotelBooking.Domain.Entities.Hotels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Infrastructure.Data;

public static class DemoDataSeeder
{
    private static readonly Guid DemoHotelId = Guid.Parse("5b2b6a8a-d2b4-4a17-b2b4-0a2ae69d1001");

    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(DemoDataSeeder));
        var context = services.GetRequiredService<ApplicationDbContext>();

        if (await context.Hotels.AnyAsync())
        {
            logger.LogInformation("Demo hotel seed skipped because hotels already exist.");
            return;
        }

        var hotel = new Hotel
        {
            Id = DemoHotelId,
            Name = "Riverfront Boutique Hotel",
            City = "Kyiv",
            Address = "Naberezhno-Khreshchatytska St, 12",
            Description = "A compact demo hotel profile with three room types for browsing, booking flows, and UI verification.",
            Images =
            [
                new HotelImage
                {
                    Id = Guid.Parse("f1f38f98-1f5e-4b63-9dd2-1918e8b81001"),
                    StorageKey = "demo/hotels/riverfront-boutique-cover.png",
                    Url = "/uploads/demo/hotels/riverfront-boutique-cover.png",
                    ContentType = "image/png",
                    SizeBytes = 0,
                    Width = 1600,
                    Height = 1000,
                    AltText = "Riverfront Boutique Hotel exterior",
                    IsCover = true,
                    SortOrder = 0
                }
            ],
            Rooms =
            [
                new Room
                {
                    Id = Guid.Parse("2ee99bb6-5d78-49df-9cac-23768c7df101"),
                    Name = "Standard Riverside",
                    Description = "A calm double room for short city stays with a compact seating area and a partial river view.",
                    Amenities = "Queen bed, Blackout curtains, Coffee station, Smart TV",
                    Capacity = 2,
                    PricePerNight = 3400m,
                    Quantity = 4,
                    IncludesBreakfast = true,
                    HasPrivateBathroom = true,
                    HasWorkspace = true,
                    HasAirConditioning = true,
                    IsActive = true,
                    Images =
                    [
                        new RoomImage
                        {
                            Id = Guid.Parse("b416c8c7-3e3a-4f17-b100-8f687f7c1101"),
                            StorageKey = "demo/rooms/standard-riverside.png",
                            Url = "/uploads/demo/rooms/standard-riverside.png",
                            ContentType = "image/png",
                            SizeBytes = 0,
                            Width = 1600,
                            Height = 1000,
                            AltText = "Standard Riverside room",
                            IsCover = true,
                            SortOrder = 0
                        }
                    ]
                },
                new Room
                {
                    Id = Guid.Parse("661ab7ea-cc01-4f71-8372-25ac63cf7102"),
                    Name = "Deluxe Panorama",
                    Description = "A larger room with open city views, a lounge corner, and upgraded bathroom amenities.",
                    Amenities = "King bed, Espresso machine, Premium toiletries, Reading lights",
                    Capacity = 3,
                    PricePerNight = 4900m,
                    Quantity = 2,
                    IncludesBreakfast = true,
                    HasPrivateBathroom = true,
                    HasBalcony = true,
                    HasWorkspace = true,
                    HasAirConditioning = true,
                    IsActive = true,
                    Images =
                    [
                        new RoomImage
                        {
                            Id = Guid.Parse("b416c8c7-3e3a-4f17-b100-8f687f7c1102"),
                            StorageKey = "demo/rooms/deluxe-panorama.png",
                            Url = "/uploads/demo/rooms/deluxe-panorama.png",
                            ContentType = "image/png",
                            SizeBytes = 0,
                            Width = 1600,
                            Height = 1000,
                            AltText = "Deluxe Panorama room",
                            IsCover = true,
                            SortOrder = 0
                        }
                    ]
                },
                new Room
                {
                    Id = Guid.Parse("28ff5645-a1e2-49d9-b3d5-c1f347738103"),
                    Name = "Family Courtyard Suite",
                    Description = "A family-friendly suite with separate sleeping zones, generous storage, and quiet courtyard-facing windows.",
                    Amenities = "Two sleeping areas, Sofa bed, Mini fridge, Extra towels",
                    Capacity = 4,
                    PricePerNight = 6200m,
                    Quantity = 2,
                    IncludesBreakfast = true,
                    HasPrivateBathroom = true,
                    HasSaunaAccess = true,
                    HasWorkspace = true,
                    HasAirConditioning = true,
                    IsActive = true,
                    Images =
                    [
                        new RoomImage
                        {
                            Id = Guid.Parse("b416c8c7-3e3a-4f17-b100-8f687f7c1103"),
                            StorageKey = "demo/rooms/family-courtyard-suite.png",
                            Url = "/uploads/demo/rooms/family-courtyard-suite.png",
                            ContentType = "image/png",
                            SizeBytes = 0,
                            Width = 1600,
                            Height = 1000,
                            AltText = "Family Courtyard Suite",
                            IsCover = true,
                            SortOrder = 0
                        }
                    ]
                }
            ]
        };

        context.Hotels.Add(hotel);
        await context.SaveChangesAsync();

        logger.LogInformation("Demo hotel data seeded.");
    }
}
