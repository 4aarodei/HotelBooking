using HotelBooking.Domain.Entities.Bookings;

namespace HotelBooking.Application.Bookings;

public class BookingService : IBookingService
{
    private readonly ICreateBookingUseCase _createBooking;
    private readonly IGetUserBookingsUseCase _getUserBookings;

    public BookingService(ICreateBookingUseCase createBooking, IGetUserBookingsUseCase getUserBookings)
    {
        _createBooking = createBooking;
        _getUserBookings = getUserBookings;
    }

    public async Task<Booking> CreateBookingAsync(string userId, Guid roomId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default)
    {
        return await _createBooking.ExecuteAsync(userId, roomId, checkIn, checkOut, ct);
    }

    public Task<List<Booking>> GetBookingsByUserAsync(string userId, CancellationToken ct = default) =>
        _getUserBookings.ExecuteAsync(userId, ct);
}
