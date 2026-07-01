using HotelBooking.Domain.Entities.Bookings;

namespace HotelBooking.Application.Bookings;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(string userId, Guid roomId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default);
    Task<List<Booking>> GetBookingsByUserAsync(string userId, CancellationToken ct = default);
}
