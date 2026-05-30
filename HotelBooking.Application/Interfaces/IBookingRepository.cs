using HotelBooking.Domain.Entities.Bookings;

namespace HotelBooking.Application.Interfaces;

public interface IBookingRepository
{
    Task<Dictionary<Guid, int>> GetOverlappingActiveBookingsCountByRoomAsync(IEnumerable<Guid> roomIds, DateOnly checkIn, DateOnly checkOut, CancellationToken ct);
    Task<bool> TryAddIfAvailableAsync(Booking booking, int roomQuantity, CancellationToken ct);
    Task<List<Booking>> GetByUserAsync(string userId, CancellationToken ct);
}
