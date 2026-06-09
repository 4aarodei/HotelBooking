namespace HotelBooking.Application.Interfaces;

public interface IFixedWindowRateLimiter
{
    Task<RateLimitResult> CheckAsync(
        string keyPrefix,
        int permitLimit,
        TimeSpan window,
        CancellationToken ct = default);
}

public sealed record RateLimitResult(bool IsAllowed, int Count, TimeSpan RetryAfter)
{
    public static RateLimitResult Allowed(int count = 0) => new(true, count, TimeSpan.Zero);
}
