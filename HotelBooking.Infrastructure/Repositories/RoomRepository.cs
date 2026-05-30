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

    public Task<Room?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return _context.Rooms.FindAsync([id], ct).AsTask();
    }

    public Task<Room?> GetByIdWithImagesAsync(Guid id, CancellationToken ct)
    {
        return _context.Rooms
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task AddAsync(Room room, CancellationToken ct)
    {
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Room room, CancellationToken ct)
    {
        _context.Rooms.Update(room);
        await _context.SaveChangesAsync(ct);
    }
}
