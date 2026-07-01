namespace HotelBooking.Application.Hotels;

public interface IHotelService
{
    Task<List<string>> GetAvailableCitiesAsync(CancellationToken ct = default);
    Task<List<HotelReadModel>> GetAvailableHotelsAsync(DateOnly checkIn, DateOnly checkOut, string? city, CancellationToken ct = default);
    Task<HotelReadModel?> GetByIdWithAvailabilityAsync(Guid id, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default);
    Task<RoomAvailabilityDetails?> GetRoomByIdWithAvailabilityAsync(Guid roomId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default);
    Task<List<HotelReadModel>> GetFeaturedAsync(int count, CancellationToken ct = default);
}
