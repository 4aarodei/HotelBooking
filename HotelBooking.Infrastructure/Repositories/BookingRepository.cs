using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Bookings;
using HotelBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _context;

    public BookingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<Guid, int>> GetOverlappingActiveBookingsCountByRoomAsync(IEnumerable<Guid> roomIds, DateOnly checkIn, DateOnly checkOut, CancellationToken ct)
    {
        return await _context.Bookings
            .Where(b =>
                roomIds.Contains(b.RoomId) &&
                b.Status != BookingStatus.Cancelled &&
                b.CheckIn < checkOut &&
                b.CheckOut > checkIn)
            .GroupBy(b => b.RoomId)
            .Select(g => new { RoomId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoomId, x => x.Count, ct);
    }

    public async Task AddAsync(Booking booking, CancellationToken ct)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<List<Booking>> GetByUserAsync(string userId, CancellationToken ct)
    {
        return await _context.Bookings
            .Include(b => b.Room)
            .ThenInclude(r => r!.Hotel)
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(ct);
    }
}
