using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Application.Persistence;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Room?> GetByIdWithImagesAsync(Guid id, CancellationToken ct);
    Task<Room?> GetByIdWithHotelAndImagesAsync(Guid id, CancellationToken ct);
    Task AddAsync(Room room, CancellationToken ct);
    Task UpdateAsync(Room room, CancellationToken ct);
}
