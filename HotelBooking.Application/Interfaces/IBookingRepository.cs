using HotelBooking.Domain.Entities.Bookings;

namespace HotelBooking.Application.Interfaces;

public interface IBookingRepository
{
    Task<bool> HasOverlapAsync(Guid roomId, DateTime checkIn, DateTime checkOut, Guid cancelledStatusCode, CancellationToken ct);
    Task<Dictionary<Guid, int>> GetActiveBookingsCountByRoomAsync(IEnumerable<Guid> roomIds, DateTime checkIn, DateTime checkOut, Guid cancelledStatusCode, CancellationToken ct);
    Task AddAsync(Booking booking, CancellationToken ct);
}
