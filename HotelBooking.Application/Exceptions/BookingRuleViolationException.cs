namespace HotelBooking.Application.Exceptions;

public sealed class BookingRuleViolationException : Exception
{
    public BookingRuleViolationException(string message) : base(message)
    {
    }
}
