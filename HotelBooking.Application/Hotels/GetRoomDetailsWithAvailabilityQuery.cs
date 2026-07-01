using HotelBooking.Application.Persistence;

namespace HotelBooking.Application.Hotels;

public sealed class GetRoomDetailsWithAvailabilityQuery : IGetRoomDetailsWithAvailabilityQuery
{
    private readonly IRoomRepository _roomRepository;
    private readonly IBookingRepository _bookingRepository;

    public GetRoomDetailsWithAvailabilityQuery(IRoomRepository roomRepository, IBookingRepository bookingRepository)
    {
        _roomRepository = roomRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<RoomAvailabilityDetails?> ExecuteAsync(Guid roomId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default)
    {
        var room = await _roomRepository.GetByIdWithHotelAndImagesAsync(roomId, ct);
        if (room is null || !room.IsActive)
        {
            return null;
        }

        var bookingsByRoom = await _bookingRepository.GetOverlappingActiveBookingsCountByRoomAsync([room.Id], checkIn, checkOut, ct);
        var bookedQuantity = bookingsByRoom.GetValueOrDefault(room.Id, 0);
        var availableQuantity = Math.Max(room.Quantity - bookedQuantity, 0);

        var snapshot = RoomReadSnapshot.FromRoom(room);
        return new RoomAvailabilityDetails(
            room.Id,
            room.HotelId,
            room.Hotel?.Name ?? string.Empty,
            room.Hotel?.City ?? string.Empty,
            room.Hotel?.Address ?? string.Empty,
            room.Name,
            room.Description,
            room.Amenities,
            snapshot.Images.Select(i => i.ToReadModel()).ToList(),
            room.Capacity,
            room.Quantity,
            availableQuantity,
            room.PricePerNight,
            room.IncludesBreakfast,
            room.HasPrivateBathroom,
            room.HasSaunaAccess,
            room.HasBalcony,
            room.HasWorkspace,
            room.HasAirConditioning);
    }
}
