namespace HotelBooking.Application.Admin;

public interface IAdminHotelQueryService
{
    Task<IReadOnlyList<AdminHotelListItem>> GetHotelListAsync(CancellationToken ct = default);
    Task<AdminHotelEditDetails?> GetHotelEditDetailsAsync(Guid hotelId, CancellationToken ct = default);
    Task<AdminRoomFormDetails?> GetCreateRoomDetailsAsync(Guid hotelId, CancellationToken ct = default);
    Task<AdminRoomFormDetails?> GetEditRoomDetailsAsync(Guid roomId, CancellationToken ct = default);
    Task<bool> HotelExistsAsync(Guid hotelId, CancellationToken ct = default);
}
