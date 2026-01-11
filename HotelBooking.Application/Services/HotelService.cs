using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Bookings;
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

    public async Task<List<Hotel>> GetAvailableHotelsAsync(DateTime checkIn, DateTime checkOut, string? city, CancellationToken ct = default)
    {
        checkIn = checkIn.Date;
        checkOut = checkOut.Date;

        var hotels = await _hotelRepository.GetWithActiveRoomsAsync(city, ct);
        var roomIds = hotels.SelectMany(h => h.Rooms).Select(r => r.Id).ToList();

        if (!roomIds.Any())
        {
            return new List<Hotel>();
        }

        var bookingsByRoom = await _bookingRepository.GetActiveBookingsCountByRoomAsync(
            roomIds,
            checkIn,
            checkOut,
            BookingStatusCodes.Cancelled,
            ct);

        foreach (var hotel in hotels)
        {
            hotel.Rooms = hotel.Rooms
                .Where(room => bookingsByRoom.GetValueOrDefault(room.Id, 0) < room.Quantity)
                .ToList();
        }

        return hotels.Where(h => h.Rooms.Any()).ToList();
    }

    public async Task<Hotel?> GetByIdWithAvailabilityAsync(Guid id, DateTime checkIn, DateTime checkOut, CancellationToken ct = default)
    {
        checkIn = checkIn.Date;
        checkOut = checkOut.Date;

        var hotel = await _hotelRepository.GetWithRoomsByIdAsync(id, ct);
        if (hotel is null)
        {
            return null;
        }

        var roomIds = hotel.Rooms.Select(r => r.Id).ToList();
        var bookingsByRoom = await _bookingRepository.GetActiveBookingsCountByRoomAsync(
            roomIds,
            checkIn,
            checkOut,
            BookingStatusCodes.Cancelled,
            ct);

        hotel.Rooms = hotel.Rooms
            .Where(room => bookingsByRoom.GetValueOrDefault(room.Id, 0) < room.Quantity)
            .ToList();

        return hotel;
    }

    public Task<List<Hotel>> GetAllAsync(CancellationToken ct = default) =>
        _hotelRepository.GetAllAsync(ct);

    public Task<Hotel?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _hotelRepository.GetByIdAsync(id, ct);

    public Task AddAsync(Hotel hotel, CancellationToken ct = default) =>
        _hotelRepository.AddAsync(hotel, ct);

    public Task UpdateAsync(Hotel hotel, CancellationToken ct = default) =>
        _hotelRepository.UpdateAsync(hotel, ct);

    public async Task<List<Hotel>> GetFeaturedAsync(int count, CancellationToken ct = default)
    {
        var hotels = await _hotelRepository.GetWithActiveRoomsAsync(null, ct);

        return hotels
            .OrderBy(h => h.Name)
            .Take(count)
            .ToList();
    }
}
