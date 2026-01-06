using HotelBooking.Domain.Entities.Bookings;

namespace HotelBooking.Application.Interfaces;

public interface IBookingStatusRepository
{
    Task<BookingStatus?> GetByCodeAsync(Guid code, CancellationToken ct);
}
