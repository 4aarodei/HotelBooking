using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Application.Interfaces;

public interface IHotelService
{
    Task<List<string>> GetAvailableCitiesAsync(CancellationToken ct = default);
    Task<List<Hotel>> GetAvailableHotelsAsync(DateOnly checkIn, DateOnly checkOut, string? city, CancellationToken ct = default);
    Task<Hotel?> GetByIdWithAvailabilityAsync(Guid id, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default);
    Task<List<Hotel>> GetFeaturedAsync(int count, CancellationToken ct = default);
}
