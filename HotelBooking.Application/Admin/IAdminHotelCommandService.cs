namespace HotelBooking.Application.Admin;

public interface IAdminHotelCommandService
{
    Task<Guid> CreateHotelAsync(CreateHotelCommand command, CancellationToken ct = default);
    Task UpdateHotelAsync(UpdateHotelCommand command, CancellationToken ct = default);
}
