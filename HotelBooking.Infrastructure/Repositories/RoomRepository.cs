using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Hotels;
using HotelBooking.Infrastructure.Data;

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
}
