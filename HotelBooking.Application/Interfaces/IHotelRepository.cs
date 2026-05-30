using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Application.Interfaces;

public interface IHotelRepository
{
    Task<List<Hotel>> GetWithActiveRoomsAsync(string? city, CancellationToken ct);
    Task<Hotel?> GetWithRoomsByIdAsync(Guid id, CancellationToken ct);
    Task<List<string>> GetDistinctCitiesAsync(CancellationToken ct);
    Task<List<Hotel>> GetFeaturedAsync(int count, CancellationToken ct);
    Task<List<Hotel>> GetAllAsync(CancellationToken ct);
    Task<Hotel?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Hotel?> GetByIdWithImagesAsync(Guid id, CancellationToken ct);
    Task AddAsync(Hotel hotel, CancellationToken ct);
    Task UpdateAsync(Hotel hotel, CancellationToken ct);
}
