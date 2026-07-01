using HotelBooking.Application.RateLimiting;

namespace HotelBooking.Infrastructure.RateLimiting;

public sealed class DisabledFixedWindowRateLimiter : IFixedWindowRateLimiter
{
    public Task<RateLimitResult> CheckAsync(
        string keyPrefix,
        int permitLimit,
        TimeSpan window,
        CancellationToken ct = default)
    {
        return Task.FromResult(RateLimitResult.Allowed());
    }
}
