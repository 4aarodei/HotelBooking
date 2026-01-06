using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Bookings;

namespace HotelBooking.Application.Services;

public class BookingStatusService
{
    private readonly IBookingStatusRepository _bookingStatusRepository;

    public BookingStatusService(IBookingStatusRepository bookingStatusRepository)
    {
        _bookingStatusRepository = bookingStatusRepository;
    }

    public Task<BookingStatus?> GetByCodeAsync(Guid code, CancellationToken ct = default) =>
        _bookingStatusRepository.GetByCodeAsync(code, ct);

    public Task<BookingStatus?> GetConfirmedAsync(CancellationToken ct = default) =>
        GetByCodeAsync(BookingStatusCodes.Confirmed, ct);

    public Task<BookingStatus?> GetCancelledAsync(CancellationToken ct = default) =>
        GetByCodeAsync(BookingStatusCodes.Cancelled, ct);

    public Task<BookingStatus?> GetPendingAsync(CancellationToken ct = default) =>
        GetByCodeAsync(BookingStatusCodes.Pending, ct);
}
