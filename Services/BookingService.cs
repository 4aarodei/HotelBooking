using HotelBooking.Constants;
using HotelBooking.Data.ApplicationDbContext;
using HotelBooking.Models.Bookings;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Services;

public class BookingService
{
    private readonly ApplicationDbContext _context;
    private readonly BookingStatusService _bookingStatusService;

    public BookingService(
        ApplicationDbContext db,
        BookingStatusService bookingStatusService)
    {
        _context = db;
        _bookingStatusService = bookingStatusService;
    }

    // CREATE
    public async Task<Booking> CreateAsync(
        string userId,
        Guid roomId,
        DateTime checkIn,
        DateTime checkOut,
        CancellationToken ct = default)
    {
        checkIn = checkIn.Date;
        checkOut = checkOut.Date;

        if (checkOut <= checkIn)
            throw new InvalidOperationException("Check-out має бути пізніше Check-in");

        // 🔒 Перевірка перетинів (ігноруємо скасовані)
        var cancelledStatus = await _bookingStatusService.GetCancelledAsync(ct);

        var hasOverlap = await _context.Bookings
            .AnyAsync(b =>
                b.RoomId == roomId &&
                b.StatusId != cancelledStatus.Id &&
                checkIn < b.CheckOut &&
                checkOut > b.CheckIn,
                ct);

        if (hasOverlap)
            throw new InvalidOperationException("Кімната вже заброньована на ці дати");

        var pendingStatus = await _bookingStatusService.GetPendingAsync(ct);

        var booking = new Booking
        {
            UserId = userId,
            RoomId = roomId,
            StatusId = pendingStatus.Id,
            CheckIn = checkIn,
            CheckOut = checkOut,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(ct);

        return booking;
    }

    public async Task<bool> IsRoomAvailableAsync(Guid roomId, DateTime checkIn, DateTime checkOut, CancellationToken ct)
    {
        return await _context.Bookings
            .AnyAsync(b =>
                b.RoomId == roomId &&
                b.CheckIn < checkOut &&
                b.CheckOut > checkIn,
                ct);
    }

}
