namespace HotelBooking.Application.Caching;

public sealed class RedisOptions
{
    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = "HotelBooking:";
    public bool RequiredForReadiness { get; set; }
    public RedisRateLimitingOptions RateLimiting { get; set; } = new();
}

public sealed class RedisRateLimitingOptions
{
    public bool Enabled { get; set; }
}
