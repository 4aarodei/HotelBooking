namespace HotelBooking.Domain.Entities.Bookings;

public static class BookingStatusCodes
{
    public static readonly Guid Confirmed =
        Guid.Parse("20DBF239-7AD2-47F1-BC0B-2ABE432EA08A");

    public static readonly Guid Cancelled =
        Guid.Parse("1546B45A-EDC8-4C6C-8DF3-59E482C27D0E");

    public static readonly Guid Pending =
        Guid.Parse("0D898448-E8A4-4593-953E-D854BEFC298D");
}
