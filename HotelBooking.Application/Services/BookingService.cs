using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Bookings;

namespace HotelBooking.Application.Services;

public class BookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingStatusRepository _bookingStatusRepository;

    public BookingService(IBookingRepository bookingRepository, IBookingStatusRepository bookingStatusRepository)
    {
        _bookingRepository = bookingRepository;
        _bookingStatusRepository = bookingStatusRepository;
    }

    public async Task<Booking> CreateAsync(string userId, Guid roomId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default)
    {
        checkIn = checkIn.Date;
        checkOut = checkOut.Date;

        if (checkOut <= checkIn)
        {
            throw new InvalidOperationException("Check-out має бути пізніше Check-in");
        }

        var hasOverlap = await _bookingRepository.HasOverlapAsync(
            roomId,
            checkIn,
            checkOut,
            BookingStatusCodes.Cancelled,
            ct);

        if (hasOverlap)
        {
            throw new InvalidOperationException("Кімната вже заброньована на ці дати");
        }

        var pendingStatus = await _bookingStatusRepository.GetByCodeAsync(BookingStatusCodes.Pending, ct)
            ?? throw new InvalidOperationException("Статус очікування не знайдено");

        var booking = new Booking
        {
            UserId = userId,
            RoomId = roomId,
            StatusId = pendingStatus.Id,
            CheckIn = checkIn,
            CheckOut = checkOut,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _bookingRepository.AddAsync(booking, ct);

        return booking;
    }
}
