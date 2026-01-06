using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Application.Services;

public class RoomService
{
    private readonly IRoomRepository _roomRepository;

    public RoomService(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public Task<List<Room>> GetByHotelAsync(Guid hotelId, CancellationToken ct = default) =>
        _roomRepository.GetByHotelAsync(hotelId, ct);

    public Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _roomRepository.GetByIdAsync(id, ct);

    public Task<List<Room>> SearchAsync(string city, DateTime checkIn, DateTime checkOut, CancellationToken ct = default) =>
        _roomRepository.SearchAsync(city, checkIn, checkOut, ct);

    public Task<Room> CreateAsync(Room room, CancellationToken ct = default) =>
        _roomRepository.CreateAsync(room, ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        _roomRepository.DeleteAsync(id, ct);
}
