using HotelBooking.Core.EntitiesModels.Hotels;
using HotelBooking.Data.ApplicationDbContext;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Core.Services;


public class RoomService 
{
    private readonly ApplicationDbContext _context;

    public RoomService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Room>> GetByHotelAsync(Guid hotelId)
    {
        return await _context.Rooms
            .Where(r => r.HotelId == hotelId)
            .ToListAsync();
    }

    public async Task<Room?> GetByIdAsync(int id)
    {
        return await _context.Rooms.FindAsync(id);
    }

    public async Task<List<Room>> SearchAsync(
        string city,
        DateTime checkIn,
        DateTime checkOut)
    {
        return await _context.Rooms
            .Include(r => r.Hotel)
            .Where(r =>
                r.Hotel.City == city &&
                !_context.Bookings.Any(b =>
                    b.RoomId == r.Id &&
                    checkIn < b.CheckOut &&
                    checkOut > b.CheckIn))
            .ToListAsync();
    }

    public async Task<Room> CreateAsync(Room room)
    {
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
        return room;
    }

    public async Task DeleteAsync(int id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room == null) return;

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();
    }
}
