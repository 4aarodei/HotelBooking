using HotelBooking.Domain.Entities.Bookings;

namespace HotelBooking.Application.Bookings;

public interface ICreateBookingUseCase
{
    Task<Booking> ExecuteAsync(string userId, Guid roomId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default);
}
