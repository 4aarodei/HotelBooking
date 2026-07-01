using HotelBooking.Application.Caching;
using HotelBooking.Application.Persistence;

namespace HotelBooking.Application.Hotels;

public sealed class GetAvailableCitiesQuery : IGetAvailableCitiesQuery
{
    private static readonly TimeSpan CitiesCacheTtl = TimeSpan.FromHours(12);

    private readonly IHotelRepository _hotelRepository;
    private readonly HotelQueryCache _cache;

    public GetAvailableCitiesQuery(IHotelRepository hotelRepository, HotelQueryCache cache)
    {
        _hotelRepository = hotelRepository;
        _cache = cache;
    }

    public async Task<List<string>> ExecuteAsync(CancellationToken ct = default)
    {
        var cached = await _cache.TryGetAsync<List<string>>(HotelCacheKeys.Cities, ct);
        if (cached is not null)
        {
            return cached;
        }

        var cities = await _hotelRepository.GetDistinctCitiesAsync(ct);
        await _cache.TrySetAsync(HotelCacheKeys.Cities, cities, CitiesCacheTtl, ct);
        return cities;
    }
}
