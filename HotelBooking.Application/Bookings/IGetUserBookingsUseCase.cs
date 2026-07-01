using HotelBooking.Domain.Entities.Bookings;

namespace HotelBooking.Application.Bookings;

public interface IGetUserBookingsUseCase
{
    Task<List<Booking>> ExecuteAsync(string userId, CancellationToken ct = default);
}
