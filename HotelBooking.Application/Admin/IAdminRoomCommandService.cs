namespace HotelBooking.Application.Admin;

public interface IAdminRoomCommandService
{
    Task<Guid> CreateRoomAsync(CreateRoomCommand command, CancellationToken ct = default);
    Task UpdateRoomAsync(UpdateRoomCommand command, CancellationToken ct = default);
}
