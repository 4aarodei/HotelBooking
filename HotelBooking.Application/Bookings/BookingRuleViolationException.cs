namespace HotelBooking.Application.Bookings;

public sealed class BookingRuleViolationException : Exception
{
    public BookingRuleViolationException(string message) : base(message)
    {
    }
}
