using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Application.Interfaces;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id, CancellationToken ct);
}
