using HotelBooking.Application.Caching;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Hotels;

public sealed class HotelQueryCache
{
    private readonly IAppCache _cache;
    private readonly ILogger<HotelQueryCache> _logger;

    public HotelQueryCache(IAppCache cache, ILogger<HotelQueryCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> TryGetAsync<T>(string key, CancellationToken ct)
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

    public async Task TrySetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
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

    public async Task<string> GetVersionAsync(string key, string defaultVersion, CancellationToken ct)
    {
        var version = await TryGetAsync<string>(key, ct);
        return string.IsNullOrWhiteSpace(version)
            ? defaultVersion
            : version.Trim();
    }
}
