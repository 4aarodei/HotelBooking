using HotelBooking.Constants;
using HotelBooking.Data.ApplicationDbContext;
using HotelBooking.Models.Hotels;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Services;

public class HotelService
{
    private readonly ApplicationDbContext _context;

    public HotelService(ApplicationDbContext context)
    {
        _context = context;
    }


    public async Task<List<Hotel>> GetByCityAsync(string city)
    {
        return await _context.Hotels
            .Where(h => h.City.ToLower() == city.ToLower())
            .Include(h => h.Rooms)
            .ToListAsync();
    }

    public async Task<List<Hotel>> GetAvailableHotelsAsync(string city, DateTime checkIn, DateTime checkOut)
    {
        var hotels = await _context.Hotels
            .Include(h => h.Rooms)
            .Where(h => h.City == city)
            .ToListAsync();

        var availableHotels = new List<Hotel>();

        foreach (var hotel in hotels)
        {
            var availableRooms = new List<Room>();

            foreach (var room in hotel.Rooms.Where(r => r.IsActive))
            {
                // кількість бронювань, які перетинаються з датами
                var bookedCount = await _context.Bookings
                    .CountAsync(b =>
                        b.RoomId == room.Id &&
                        b.Status.BookingStatusCode != BookingStatusCodes.Cancelled &&
                        b.CheckIn < checkOut &&
                        b.CheckOut > checkIn
                    );

                if (bookedCount < room.Quantity)
                {
                    availableRooms.Add(room);
                }
            }

            if (availableRooms.Any())
            {
                hotel.Rooms = availableRooms;
                availableHotels.Add(hotel);
            }
        }

        return availableHotels;
    }


    public async Task<List<Hotel>> GetAllAsync()
    {
        return await _context.Hotels
            .Include(h => h.Rooms)
            .ToListAsync();
    }

    public async Task<Hotel?> GetByIdAsync(Guid id)
    {
        return await _context.Hotels
            .Include(h => h.Rooms)
            .FirstOrDefaultAsync(h => h.Id == id);
    }
}
