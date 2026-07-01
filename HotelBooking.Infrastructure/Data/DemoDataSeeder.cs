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

        var hotel = Hotel.Create(
            DemoHotelId,
            "Riverfront Boutique Hotel",
            "Kyiv",
            "Naberezhno-Khreshchatytska St, 12",
            "A compact demo hotel profile with three room types for browsing, booking flows, and UI verification.");

        hotel.Images.Add(new HotelImage
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
        });

        var standardRoom = Room.Create(
            Guid.Parse("2ee99bb6-5d78-49df-9cac-23768c7df101"),
            DemoHotelId,
            "Standard Riverside",
            "A calm double room for short city stays with a compact seating area and a partial river view.",
            "Queen bed, Blackout curtains, Coffee station, Smart TV",
            2,
            3400m,
            4,
            includesBreakfast: true,
            hasPrivateBathroom: true,
            hasSaunaAccess: false,
            hasBalcony: false,
            hasWorkspace: true,
            hasAirConditioning: true,
            isActive: true);
        standardRoom.Images.Add(CreateRoomImage("b416c8c7-3e3a-4f17-b100-8f687f7c1101", "demo/rooms/standard-riverside.png", "Standard Riverside room"));

        var deluxeRoom = Room.Create(
            Guid.Parse("661ab7ea-cc01-4f71-8372-25ac63cf7102"),
            DemoHotelId,
            "Deluxe Panorama",
            "A larger room with open city views, a lounge corner, and upgraded bathroom amenities.",
            "King bed, Espresso machine, Premium toiletries, Reading lights",
            3,
            4900m,
            2,
            includesBreakfast: true,
            hasPrivateBathroom: true,
            hasSaunaAccess: false,
            hasBalcony: true,
            hasWorkspace: true,
            hasAirConditioning: true,
            isActive: true);
        deluxeRoom.Images.Add(CreateRoomImage("b416c8c7-3e3a-4f17-b100-8f687f7c1102", "demo/rooms/deluxe-panorama.png", "Deluxe Panorama room"));

        var familyRoom = Room.Create(
            Guid.Parse("28ff5645-a1e2-49d9-b3d5-c1f347738103"),
            DemoHotelId,
            "Family Courtyard Suite",
            "A family-friendly suite with separate sleeping zones, generous storage, and quiet courtyard-facing windows.",
            "Two sleeping areas, Sofa bed, Mini fridge, Extra towels",
            4,
            6200m,
            2,
            includesBreakfast: true,
            hasPrivateBathroom: true,
            hasSaunaAccess: true,
            hasBalcony: false,
            hasWorkspace: true,
            hasAirConditioning: true,
            isActive: true);
        familyRoom.Images.Add(CreateRoomImage("b416c8c7-3e3a-4f17-b100-8f687f7c1103", "demo/rooms/family-courtyard-suite.png", "Family Courtyard Suite"));

        hotel.Rooms.Add(standardRoom);
        hotel.Rooms.Add(deluxeRoom);
        hotel.Rooms.Add(familyRoom);

        context.Hotels.Add(hotel);
        await context.SaveChangesAsync();

        logger.LogInformation("Demo hotel data seeded.");
    }

    private static RoomImage CreateRoomImage(string id, string storageKey, string altText)
    {
        return new RoomImage
        {
            Id = Guid.Parse(id),
            StorageKey = storageKey,
            Url = $"/uploads/{storageKey}",
            ContentType = "image/png",
            SizeBytes = 0,
            Width = 1600,
            Height = 1000,
            AltText = altText,
            IsCover = true,
            SortOrder = 0
        };
    }
}
