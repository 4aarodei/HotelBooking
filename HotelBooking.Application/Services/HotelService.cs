using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Application.Services;

public class HotelService
{
    private readonly IHotelRepository _hotelRepository;
    private readonly IBookingRepository _bookingRepository;

    public HotelService(IHotelRepository hotelRepository, IBookingRepository bookingRepository)
    {
        _hotelRepository = hotelRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<List<string>> GetAvailableCitiesAsync(CancellationToken ct = default)
    {
        var hotels = await _hotelRepository.GetWithActiveRoomsAsync(null, ct);

        return hotels
            .Select(h => h.City)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();
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

        return hotels.Where(h => h.Rooms.Any()).ToList();
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

    public async Task<List<Hotel>> GetFeaturedAsync(int count, CancellationToken ct = default)
    {
        var hotels = await _hotelRepository.GetWithActiveRoomsAsync(null, ct);

        return hotels
            .OrderBy(h => h.Name)
            .Take(count)
            .ToList();
    }
}
