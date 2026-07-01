namespace HotelBooking.Domain.Entities.Hotels;

public sealed record RoomFeatures(
    bool IncludesBreakfast,
    bool HasPrivateBathroom,
    bool HasSaunaAccess,
    bool HasBalcony,
    bool HasWorkspace,
    bool HasAirConditioning);
