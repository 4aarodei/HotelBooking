using HotelBooking.Application.Persistence;
namespace HotelBooking.Application.Hotels;

public sealed class GetHotelDetailsWithAvailabilityQuery : IGetHotelDetailsWithAvailabilityQuery
{
    private readonly IHotelRepository _hotelRepository;
    private readonly IBookingRepository _bookingRepository;

    public GetHotelDetailsWithAvailabilityQuery(IHotelRepository hotelRepository, IBookingRepository bookingRepository)
    {
        _hotelRepository = hotelRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<HotelReadModel?> ExecuteAsync(Guid id, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default)
    {
        var hotel = await _hotelRepository.GetWithRoomsByIdAsync(id, ct);
        if (hotel is null)
        {
            return null;
        }

        var roomIds = hotel.Rooms.Select(r => r.Id).ToList();
        var bookingsByRoom = await _bookingRepository.GetOverlappingActiveBookingsCountByRoomAsync(roomIds, checkIn, checkOut, ct);

        var snapshot = HotelReadSnapshot.FromHotel(hotel);
        return snapshot.ToReadModel(
            snapshot.Rooms.Where(room => bookingsByRoom.GetValueOrDefault(room.Id, 0) < room.Quantity));
    }
}
