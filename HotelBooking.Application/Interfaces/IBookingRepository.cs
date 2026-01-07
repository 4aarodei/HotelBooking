using HotelBooking.Domain.Entities.Bookings;

namespace HotelBooking.Application.Interfaces;

public interface IBookingRepository
{
    Task<Dictionary<Guid, int>> GetActiveBookingsCountByRoomAsync(IEnumerable<Guid> roomIds, DateTime checkIn, DateTime checkOut, Guid cancelledStatusCode, CancellationToken ct);
    Task AddAsync(Booking booking, CancellationToken ct);
    Task<List<Booking>> GetByUserAsync(string userId, CancellationToken ct);
}
