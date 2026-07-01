namespace HotelBooking.Application.Hotels;

public interface IGetAvailableCitiesQuery
{
    Task<List<string>> ExecuteAsync(CancellationToken ct = default);
}

public interface ISearchAvailableHotelsQuery
{
    Task<List<HotelReadModel>> ExecuteAsync(DateOnly checkIn, DateOnly checkOut, string? city, CancellationToken ct = default);
}

public interface IGetHotelDetailsWithAvailabilityQuery
{
    Task<HotelReadModel?> ExecuteAsync(Guid id, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default);
}

public interface IGetRoomDetailsWithAvailabilityQuery
{
    Task<RoomAvailabilityDetails?> ExecuteAsync(Guid roomId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default);
}

public interface IGetFeaturedHotelsQuery
{
    Task<List<HotelReadModel>> ExecuteAsync(int count, CancellationToken ct = default);
}
