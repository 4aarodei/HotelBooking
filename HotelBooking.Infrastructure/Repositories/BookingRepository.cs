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

    public Task<bool> HasOverlapAsync(Guid roomId, DateTime checkIn, DateTime checkOut, Guid cancelledStatusCode, CancellationToken ct)
    {
        return _context.Bookings
            .AnyAsync(b =>
                b.RoomId == roomId &&
                b.Status.BookingStatusCode != cancelledStatusCode &&
                checkIn < b.CheckOut &&
                checkOut > b.CheckIn,
                ct);
    }

    public async Task<Dictionary<Guid, int>> GetActiveBookingsCountByRoomAsync(IEnumerable<Guid> roomIds, DateTime checkIn, DateTime checkOut, Guid cancelledStatusCode, CancellationToken ct)
    {
        return await _context.Bookings
            .Where(b =>
                roomIds.Contains(b.RoomId) &&
                b.Status.BookingStatusCode != cancelledStatusCode &&
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
}
