using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Hotels;
using HotelBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly ApplicationDbContext _context;

    public RoomRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<Room>> GetByHotelAsync(Guid hotelId, CancellationToken ct)
    {
        return _context.Rooms
            .Where(r => r.HotelId == hotelId)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task<Room?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return _context.Rooms.FindAsync([id], ct).AsTask();
    }

    public Task<List<Room>> SearchAsync(string city, DateTime checkIn, DateTime checkOut, CancellationToken ct)
    {
        return _context.Rooms
            .Include(r => r.Hotel)
            .Where(r =>
                r.Hotel.City == city &&
                !_context.Bookings.Any(b =>
                    b.RoomId == r.Id &&
                    checkIn < b.CheckOut &&
                    checkOut > b.CheckIn))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Room> CreateAsync(Room room, CancellationToken ct)
    {
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync(ct);
        return room;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var room = await _context.Rooms.FindAsync([id], ct);
        if (room == null)
        {
            return;
        }

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync(ct);
    }
}
