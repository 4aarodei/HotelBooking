using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Application.Interfaces;

public interface IRoomRepository
{
    Task<List<Room>> GetByHotelAsync(Guid hotelId, CancellationToken ct);
    Task<Room?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Room>> SearchAsync(string city, DateTime checkIn, DateTime checkOut, CancellationToken ct);
    Task<Room> CreateAsync(Room room, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
