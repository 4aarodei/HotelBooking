using HotelBooking.Domain.Exceptions;

namespace HotelBooking.Domain.Entities.Bookings;

public sealed record DateRange
{
    private DateRange(DateOnly checkIn, DateOnly checkOut)
    {
        CheckIn = checkIn;
        CheckOut = checkOut;
        Nights = checkOut.DayNumber - checkIn.DayNumber;
    }

    public DateOnly CheckIn { get; }
    public DateOnly CheckOut { get; }
    public int Nights { get; }

    public static DateRange Create(DateOnly checkIn, DateOnly checkOut)
    {
        if (checkOut <= checkIn)
        {
            throw new DomainRuleViolationException("Check-out date must be later than check-in date.");
        }

        return new DateRange(checkIn, checkOut);
    }
}
