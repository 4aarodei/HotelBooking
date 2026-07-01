using HotelBooking.Application.Persistence;
using HotelBooking.Domain.Entities.Bookings;

namespace HotelBooking.Application.Bookings;

public sealed class GetUserBookingsUseCase : IGetUserBookingsUseCase
{
    private readonly IBookingRepository _bookingRepository;

    public GetUserBookingsUseCase(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public Task<List<Booking>> ExecuteAsync(string userId, CancellationToken ct = default)
    {
        return _bookingRepository.GetByUserAsync(userId, ct);
    }
}
