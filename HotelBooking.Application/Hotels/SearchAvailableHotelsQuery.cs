using HotelBooking.Application.Caching;
using HotelBooking.Application.Persistence;
namespace HotelBooking.Application.Hotels;

public sealed class SearchAvailableHotelsQuery : ISearchAvailableHotelsQuery
{
    private static readonly TimeSpan HotelSearchCacheTtl = TimeSpan.FromSeconds(60);

    private readonly IHotelRepository _hotelRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly HotelQueryCache _cache;

    public SearchAvailableHotelsQuery(
        IHotelRepository hotelRepository,
        IBookingRepository bookingRepository,
        HotelQueryCache cache)
    {
        _hotelRepository = hotelRepository;
        _bookingRepository = bookingRepository;
        _cache = cache;
    }

    public async Task<List<HotelReadModel>> ExecuteAsync(DateOnly checkIn, DateOnly checkOut, string? city, CancellationToken ct = default)
    {
        var catalogVersion = await _cache.GetVersionAsync(HotelCacheKeys.CatalogVersion, HotelCacheKeys.DefaultCatalogVersion, ct);
        var availabilityVersion = await _cache.GetVersionAsync(HotelCacheKeys.AvailabilityVersion, HotelCacheKeys.DefaultAvailabilityVersion, ct);
        var cacheKey = HotelCacheKeys.HotelSearch(city, checkIn, checkOut, catalogVersion, availabilityVersion);
        var cached = await _cache.TryGetAsync<List<HotelReadSnapshot>>(cacheKey, ct);
        if (cached is not null)
        {
            return cached.Select(h => h.ToReadModel()).ToList();
        }

        var hotels = await _hotelRepository.GetWithActiveRoomsAsync(city, ct);
        var roomIds = hotels.SelectMany(h => h.Rooms).Select(r => r.Id).ToList();

        if (roomIds.Count == 0)
        {
            await _cache.TrySetAsync(cacheKey, new List<HotelReadSnapshot>(), HotelSearchCacheTtl, ct);
            return [];
        }

        var bookingsByRoom = await _bookingRepository.GetOverlappingActiveBookingsCountByRoomAsync(roomIds, checkIn, checkOut, ct);

        var availableSnapshots = hotels
            .Select(HotelReadSnapshot.FromHotel)
            .Select(hotel => hotel.ToReadModel(
                hotel.Rooms.Where(room => bookingsByRoom.GetValueOrDefault(room.Id, 0) < room.Quantity)))
            .Where(hotel => hotel.Rooms.Count > 0)
            .ToList();

        await _cache.TrySetAsync(
            cacheKey,
            availableSnapshots.Select(HotelReadSnapshot.FromReadModel).ToList(),
            HotelSearchCacheTtl,
            ct);

        return availableSnapshots;
    }
}
