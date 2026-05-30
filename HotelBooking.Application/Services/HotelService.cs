using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Hotels;
using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Application.Services;

public class HotelService : IHotelService
{
    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IBookingRepository _bookingRepository;

    public HotelService(IHotelRepository hotelRepository, IRoomRepository roomRepository, IBookingRepository bookingRepository)
    {
        _hotelRepository = hotelRepository;
        _roomRepository = roomRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<List<string>> GetAvailableCitiesAsync(CancellationToken ct = default)
    {
        return await _hotelRepository.GetDistinctCitiesAsync(ct);
    }

    public async Task<List<Hotel>> GetAvailableHotelsAsync(DateOnly checkIn, DateOnly checkOut, string? city, CancellationToken ct = default)
    {
        var hotels = await _hotelRepository.GetWithActiveRoomsAsync(city, ct);
        var roomIds = hotels.SelectMany(h => h.Rooms).Select(r => r.Id).ToList();

        if (roomIds.Count == 0)
        {
            return [];
        }

        var bookingsByRoom = await _bookingRepository.GetOverlappingActiveBookingsCountByRoomAsync(
            roomIds,
            checkIn,
            checkOut,
            ct);

        foreach (var hotel in hotels)
        {
            hotel.Rooms = hotel.Rooms
                .Where(room => bookingsByRoom.GetValueOrDefault(room.Id, 0) < room.Quantity)
                .ToList();
        }

        return hotels.Where(h => h.Rooms.Count > 0).ToList();
    }

    public async Task<Hotel?> GetByIdWithAvailabilityAsync(Guid id, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default)
    {
        var hotel = await _hotelRepository.GetWithRoomsByIdAsync(id, ct);
        if (hotel is null)
        {
            return null;
        }

        var roomIds = hotel.Rooms.Select(r => r.Id).ToList();
        var bookingsByRoom = await _bookingRepository.GetOverlappingActiveBookingsCountByRoomAsync(
            roomIds,
            checkIn,
            checkOut,
            ct);

        hotel.Rooms = hotel.Rooms
            .Where(room => bookingsByRoom.GetValueOrDefault(room.Id, 0) < room.Quantity)
            .ToList();

        return hotel;
    }

    public async Task<RoomAvailabilityDetails?> GetRoomByIdWithAvailabilityAsync(Guid roomId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default)
    {
        var room = await _roomRepository.GetByIdWithHotelAndImagesAsync(roomId, ct);
        if (room is null || !room.IsActive)
        {
            return null;
        }

        var bookingsByRoom = await _bookingRepository.GetOverlappingActiveBookingsCountByRoomAsync(
            [room.Id],
            checkIn,
            checkOut,
            ct);

        var bookedQuantity = bookingsByRoom.GetValueOrDefault(room.Id, 0);
        var availableQuantity = Math.Max(room.Quantity - bookedQuantity, 0);

        return new RoomAvailabilityDetails(room, availableQuantity);
    }

    public async Task<List<Hotel>> GetFeaturedAsync(int count, CancellationToken ct = default)
    {
        return await _hotelRepository.GetFeaturedAsync(count, ct);
    }
}
