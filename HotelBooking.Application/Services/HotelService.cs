using HotelBooking.Application.Caching;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Hotels;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Services;

public class HotelService : IHotelService
{
    private static readonly TimeSpan CitiesCacheTtl = TimeSpan.FromHours(12);
    private static readonly TimeSpan FeaturedHotelsCacheTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan HotelSearchCacheTtl = TimeSpan.FromSeconds(60);

    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IAppCache _cache;
    private readonly ILogger<HotelService> _logger;

    public HotelService(
        IHotelRepository hotelRepository,
        IRoomRepository roomRepository,
        IBookingRepository bookingRepository,
        IAppCache cache,
        ILogger<HotelService> logger)
    {
        _hotelRepository = hotelRepository;
        _roomRepository = roomRepository;
        _bookingRepository = bookingRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<string>> GetAvailableCitiesAsync(CancellationToken ct = default)
    {
        var cached = await TryGetCacheAsync<List<string>>(HotelCacheKeys.Cities, ct);
        if (cached is not null)
        {
            return cached;
        }

        var cities = await _hotelRepository.GetDistinctCitiesAsync(ct);
        await TrySetCacheAsync(HotelCacheKeys.Cities, cities, CitiesCacheTtl, ct);
        return cities;
    }

    public async Task<List<Hotel>> GetAvailableHotelsAsync(DateOnly checkIn, DateOnly checkOut, string? city, CancellationToken ct = default)
    {
        var catalogVersion = await GetCatalogVersionAsync(ct);
        var availabilityVersion = await GetAvailabilityVersionAsync(ct);
        var cacheKey = HotelCacheKeys.HotelSearch(city, checkIn, checkOut, catalogVersion, availabilityVersion);
        var cached = await TryGetCacheAsync<List<HotelReadSnapshot>>(cacheKey, ct);
        if (cached is not null)
        {
            return cached.Select(h => h.ToHotel()).ToList();
        }

        var hotels = await _hotelRepository.GetWithActiveRoomsAsync(city, ct);
        var roomIds = hotels.SelectMany(h => h.Rooms).Select(r => r.Id).ToList();

        if (roomIds.Count == 0)
        {
            await TrySetCacheAsync(cacheKey, new List<HotelReadSnapshot>(), HotelSearchCacheTtl, ct);
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

        var availableHotels = hotels.Where(h => h.Rooms.Count > 0).ToList();
        await TrySetCacheAsync(
            cacheKey,
            availableHotels.Select(HotelReadSnapshot.FromHotel).ToList(),
            HotelSearchCacheTtl,
            ct);

        return availableHotels;
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
        var catalogVersion = await GetCatalogVersionAsync(ct);
        var cacheKey = HotelCacheKeys.FeaturedHotels(count, catalogVersion);
        var cached = await TryGetCacheAsync<List<HotelReadSnapshot>>(cacheKey, ct);
        if (cached is not null)
        {
            return cached.Select(h => h.ToHotel()).ToList();
        }

        var hotels = await _hotelRepository.GetFeaturedAsync(count, ct);
        await TrySetCacheAsync(
            cacheKey,
            hotels.Select(HotelReadSnapshot.FromHotel).ToList(),
            FeaturedHotelsCacheTtl,
            ct);

        return hotels;
    }

    private async Task<string> GetCatalogVersionAsync(CancellationToken ct)
    {
        return await GetVersionAsync(HotelCacheKeys.CatalogVersion, HotelCacheKeys.DefaultCatalogVersion, ct);
    }

    private async Task<string> GetAvailabilityVersionAsync(CancellationToken ct)
    {
        return await GetVersionAsync(HotelCacheKeys.AvailabilityVersion, HotelCacheKeys.DefaultAvailabilityVersion, ct);
    }

    private async Task<string> GetVersionAsync(string key, string defaultVersion, CancellationToken ct)
    {
        var version = await TryGetCacheAsync<string>(key, ct);
        return string.IsNullOrWhiteSpace(version)
            ? defaultVersion
            : version.Trim();
    }

    private async Task<T?> TryGetCacheAsync<T>(string key, CancellationToken ct)
    {
        try
        {
            return await _cache.GetAsync<T>(key, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache read failed for key {CacheKey}; falling back to SQL.", key);
            return default;
        }
    }

    private async Task TrySetCacheAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        try
        {
            await _cache.SetAsync(key, value, ttl, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache write failed for key {CacheKey}; continuing with SQL result.", key);
        }
    }
}
