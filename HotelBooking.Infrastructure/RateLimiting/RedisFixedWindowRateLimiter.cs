using HotelBooking.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HotelBooking.Infrastructure.RateLimiting;

public sealed class RedisFixedWindowRateLimiter : IFixedWindowRateLimiter
{
    private const string IncrementWithExpiryScript = """
        local current = redis.call('INCR', KEYS[1])
        if current == 1 then
            redis.call('PEXPIRE', KEYS[1], ARGV[1])
        end
        local ttl = redis.call('PTTL', KEYS[1])
        return { current, ttl }
        """;

    private readonly IConnectionMultiplexer _redis;
    private readonly IClock _clock;
    private readonly ILogger<RedisFixedWindowRateLimiter> _logger;

    public RedisFixedWindowRateLimiter(
        IConnectionMultiplexer redis,
        IClock clock,
        ILogger<RedisFixedWindowRateLimiter> logger)
    {
        _redis = redis;
        _clock = clock;
        _logger = logger;
    }

    public async Task<RateLimitResult> CheckAsync(
        string keyPrefix,
        int permitLimit,
        TimeSpan window,
        CancellationToken ct = default)
    {
        if (permitLimit <= 0 || window <= TimeSpan.Zero)
        {
            return RateLimitResult.Allowed();
        }

        try
        {
            ct.ThrowIfCancellationRequested();

            var windowSeconds = Math.Max(1, (long)window.TotalSeconds);
            var windowNumber = _clock.UtcNow.ToUnixTimeSeconds() / windowSeconds;
            var key = $"{keyPrefix}:{windowNumber}";
            var database = _redis.GetDatabase();
            var result = (RedisResult[]?)await database.ScriptEvaluateAsync(
                IncrementWithExpiryScript,
                [key],
                [(long)window.TotalMilliseconds]);

            var count = result is { Length: > 0 } ? (int)(long)result[0] : 0;
            var retryAfter = window;
            if (result is { Length: > 1 })
            {
                var ttlMilliseconds = (long)result[1];
                if (ttlMilliseconds > 0)
                {
                    retryAfter = TimeSpan.FromMilliseconds(ttlMilliseconds);
                }
            }

            return count <= permitLimit
                ? RateLimitResult.Allowed(count)
                : new RateLimitResult(false, count, retryAfter);
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or InvalidOperationException)
        {
            _logger.LogWarning(
                ex,
                "Redis rate limiter failed open for key prefix {RateLimitKeyPrefix}.",
                keyPrefix);

            return RateLimitResult.Allowed();
        }
    }
}
