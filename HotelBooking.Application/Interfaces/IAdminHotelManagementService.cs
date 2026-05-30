using HotelBooking.Application.Hotels;

namespace HotelBooking.Application.Interfaces;

public interface IAdminHotelManagementService
{
    Task<Guid> CreateHotelAsync(CreateHotelCommand command, CancellationToken ct = default);
    Task UpdateHotelAsync(UpdateHotelCommand command, CancellationToken ct = default);
    Task<Guid> CreateRoomAsync(CreateRoomCommand command, CancellationToken ct = default);
    Task UpdateRoomAsync(UpdateRoomCommand command, CancellationToken ct = default);
}
