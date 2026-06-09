using HotelBooking.Application.Caching;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HotelBooking.Web.Health;

public sealed class RedisReadinessHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisOptions _options;

    public RedisReadinessHealthCheck(IConnectionMultiplexer redis, IOptions<RedisOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || !_options.RequiredForReadiness)
        {
            return HealthCheckResult.Healthy("Redis readiness is not required.");
        }

        try
        {
            await _redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy("Redis responded to ping.");
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or InvalidOperationException)
        {
            return HealthCheckResult.Unhealthy("Redis is required for readiness but is unavailable.", ex);
        }
    }
}
