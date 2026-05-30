using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Services;

public sealed class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
