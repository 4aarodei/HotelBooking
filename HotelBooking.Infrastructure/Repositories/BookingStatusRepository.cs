using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Bookings;
using HotelBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class BookingStatusRepository : IBookingStatusRepository
{
    private readonly ApplicationDbContext _context;

    public BookingStatusRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<BookingStatus?> GetByCodeAsync(Guid code, CancellationToken ct)
    {
        return _context.BookingStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BookingStatusCode == code, ct);
    }
}
