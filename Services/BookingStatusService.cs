using HotelBooking.Constants;
using HotelBooking.Data.ApplicationDbContext;
using HotelBooking.Models.Bookings;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Constants
{
    // Коди статусів бронювання
    public static class BookingStatusCodes
    {
        public static readonly Guid Confirmed =
            Guid.Parse("20DBF239-7AD2-47F1-BC0B-2ABE432EA08A");

        public static readonly Guid Cancelled =
            Guid.Parse("1546B45A-EDC8-4C6C-8DF3-59E482C27D0E");

        public static readonly Guid Pending =
            Guid.Parse("0D898448-E8A4-4593-953E-D854BEFC298D");
    }
}

namespace HotelBooking.Services
{
    public class BookingStatusService
    {
        private readonly ApplicationDbContext _context;

        public BookingStatusService(ApplicationDbContext db)
        {
            _context = db;
        }

        public async Task<IReadOnlyList<BookingStatus>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.BookingStatuses
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync(ct);
        }

        public async Task<BookingStatus> GetByCodeAsync(Guid code, CancellationToken ct = default)
        {
            var status = await _context.BookingStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.BookingStatusCode == code, ct);

            if (status is null)
                throw new KeyNotFoundException(
                    $"BookingStatus з кодом '{code}' не знайдено");

            return status;
        }

        public Task<BookingStatus> GetConfirmedAsync(CancellationToken ct = default)
            => GetByCodeAsync(BookingStatusCodes.Confirmed, ct);

        public Task<BookingStatus> GetCancelledAsync(CancellationToken ct = default)
            => GetByCodeAsync(BookingStatusCodes.Cancelled, ct);

        public Task<BookingStatus> GetPendingAsync(CancellationToken ct = default)
            => GetByCodeAsync(BookingStatusCodes.Pending, ct);
    }
}