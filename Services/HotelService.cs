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


    public async Task<List<Hotel>> GetAvailableHotelsAsync(DateTime checkIn, DateTime checkOut, string? city = null)
    {
        checkIn = checkIn.Date;
        checkOut = checkOut.Date;

        var hotelsQuery = _context.Hotels.AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
        {
            var normalizedCity = city.ToLower();
            hotelsQuery = hotelsQuery.Where(h => h.City.ToLower() == normalizedCity);
        }

        var hotels = await hotelsQuery
            .Include(h => h.Rooms.Where(r => r.IsActive))
            .AsNoTracking()
            .ToListAsync();

        var roomIds = hotels
            .SelectMany(h => h.Rooms)
            .Select(r => r.Id)
            .ToList();

        if (!roomIds.Any())
        {
            return new List<Hotel>();
        }

        var bookingsByRoom = await _context.Bookings
            .Where(b =>
                roomIds.Contains(b.RoomId) &&
                b.Status.BookingStatusCode != BookingStatusCodes.Cancelled &&
                b.CheckIn < checkOut &&
                b.CheckOut > checkIn)
            .GroupBy(b => b.RoomId)
            .Select(g => new { RoomId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoomId, x => x.Count);

        foreach (var hotel in hotels)
        {
            hotel.Rooms = hotel.Rooms
                .Where(room =>
                {
                    var bookedCount = bookingsByRoom.GetValueOrDefault(room.Id, 0);
                    return bookedCount < room.Quantity;
                })
                .ToList();
        }

        return hotels.Where(h => h.Rooms.Any()).ToList();
    }


    public async Task<Hotel?> GetByIdWithAvailabilityAsync(Guid id, DateTime checkIn, DateTime checkOut)
    {
        checkIn = checkIn.Date;
        checkOut = checkOut.Date;

        var hotel = await _context.Hotels
            .Include(h => h.Rooms.Where(r => r.IsActive))
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hotel is null)
        {
            return null;
        }

        var roomIds = hotel.Rooms.Select(r => r.Id).ToList();

        var bookingsByRoom = await _context.Bookings
            .Where(b =>
                roomIds.Contains(b.RoomId) &&
                b.Status.BookingStatusCode != BookingStatusCodes.Cancelled &&
                b.CheckIn < checkOut &&
                b.CheckOut > checkIn)
            .GroupBy(b => b.RoomId)
            .Select(g => new { RoomId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoomId, x => x.Count);

        hotel.Rooms = hotel.Rooms
            .Where(room => bookingsByRoom.GetValueOrDefault(room.Id, 0) < room.Quantity)
            .ToList();

        return hotel;
    }
}
