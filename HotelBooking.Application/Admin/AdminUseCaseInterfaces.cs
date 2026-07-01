namespace HotelBooking.Application.Admin;

public interface ICreateHotelUseCase
{
    Task<Guid> ExecuteAsync(CreateHotelCommand command, CancellationToken ct = default);
}

public interface IUpdateHotelUseCase
{
    Task ExecuteAsync(UpdateHotelCommand command, CancellationToken ct = default);
}

public interface ICreateRoomUseCase
{
    Task<Guid> ExecuteAsync(CreateRoomCommand command, CancellationToken ct = default);
}

public interface IUpdateRoomUseCase
{
    Task ExecuteAsync(UpdateRoomCommand command, CancellationToken ct = default);
}

public interface IGetAdminHotelListQuery
{
    Task<IReadOnlyList<AdminHotelListItem>> ExecuteAsync(CancellationToken ct = default);
}

public interface IGetAdminHotelEditDetailsQuery
{
    Task<AdminHotelEditDetails?> ExecuteForHotelAsync(Guid hotelId, CancellationToken ct = default);
}

public interface IGetCreateRoomDetailsQuery
{
    Task<AdminRoomFormDetails?> ExecuteForHotelAsync(Guid hotelId, CancellationToken ct = default);
}

public interface IGetEditRoomDetailsQuery
{
    Task<AdminRoomFormDetails?> ExecuteForRoomAsync(Guid roomId, CancellationToken ct = default);
}

public interface IAdminHotelExistsQuery
{
    Task<bool> ExecuteForHotelAsync(Guid hotelId, CancellationToken ct = default);
}
