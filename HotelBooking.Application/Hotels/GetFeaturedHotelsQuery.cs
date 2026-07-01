using HotelBooking.Application.Caching;
using HotelBooking.Application.Persistence;
namespace HotelBooking.Application.Hotels;

public sealed class GetFeaturedHotelsQuery : IGetFeaturedHotelsQuery
{
    private static readonly TimeSpan FeaturedHotelsCacheTtl = TimeSpan.FromMinutes(15);

    private readonly IHotelRepository _hotelRepository;
    private readonly HotelQueryCache _cache;

    public GetFeaturedHotelsQuery(IHotelRepository hotelRepository, HotelQueryCache cache)
    {
        _hotelRepository = hotelRepository;
        _cache = cache;
    }

    public async Task<List<HotelReadModel>> ExecuteAsync(int count, CancellationToken ct = default)
    {
        var catalogVersion = await _cache.GetVersionAsync(HotelCacheKeys.CatalogVersion, HotelCacheKeys.DefaultCatalogVersion, ct);
        var cacheKey = HotelCacheKeys.FeaturedHotels(count, catalogVersion);
        var cached = await _cache.TryGetAsync<List<HotelReadSnapshot>>(cacheKey, ct);
        if (cached is not null)
        {
            return cached.Select(h => h.ToReadModel()).ToList();
        }

        var hotels = await _hotelRepository.GetFeaturedAsync(count, ct);
        await _cache.TrySetAsync(
            cacheKey,
            hotels.Select(HotelReadSnapshot.FromHotel).ToList(),
            FeaturedHotelsCacheTtl,
            ct);

        return hotels.Select(HotelReadSnapshot.FromHotel).Select(h => h.ToReadModel()).ToList();
    }
}
