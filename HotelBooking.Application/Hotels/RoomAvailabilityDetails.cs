using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Application.Hotels;

public sealed record RoomAvailabilityDetails(Room Room, int AvailableQuantity);
