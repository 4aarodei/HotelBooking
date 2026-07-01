using HotelBooking.Application.Caching;

namespace HotelBooking.Infrastructure.Caching;

public sealed class NoOpAppCache : IAppCache
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
