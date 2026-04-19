using HotelBooking.Domain.Entities.Bookings;

namespace HotelBooking.Application.Interfaces;

public interface IBookingRepository
{
    Task<Dictionary<Guid, int>> GetOverlappingActiveBookingsCountByRoomAsync(IEnumerable<Guid> roomIds, DateOnly checkIn, DateOnly checkOut, CancellationToken ct);
    Task AddAsync(Booking booking, CancellationToken ct);
    Task<List<Booking>> GetByUserAsync(string userId, CancellationToken ct);
}
