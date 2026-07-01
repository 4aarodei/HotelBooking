namespace HotelBooking.Application.Hotels;

public class HotelService : IHotelService
{
    private readonly IGetAvailableCitiesQuery _getAvailableCities;
    private readonly ISearchAvailableHotelsQuery _searchAvailableHotels;
    private readonly IGetHotelDetailsWithAvailabilityQuery _getHotelDetails;
    private readonly IGetRoomDetailsWithAvailabilityQuery _getRoomDetails;
    private readonly IGetFeaturedHotelsQuery _getFeaturedHotels;

    public HotelService(
        IGetAvailableCitiesQuery getAvailableCities,
        ISearchAvailableHotelsQuery searchAvailableHotels,
        IGetHotelDetailsWithAvailabilityQuery getHotelDetails,
        IGetRoomDetailsWithAvailabilityQuery getRoomDetails,
        IGetFeaturedHotelsQuery getFeaturedHotels)
    {
        _getAvailableCities = getAvailableCities;
        _searchAvailableHotels = searchAvailableHotels;
        _getHotelDetails = getHotelDetails;
        _getRoomDetails = getRoomDetails;
        _getFeaturedHotels = getFeaturedHotels;
    }

    public Task<List<string>> GetAvailableCitiesAsync(CancellationToken ct = default) =>
        _getAvailableCities.ExecuteAsync(ct);

    public Task<List<HotelReadModel>> GetAvailableHotelsAsync(DateOnly checkIn, DateOnly checkOut, string? city, CancellationToken ct = default) =>
        _searchAvailableHotels.ExecuteAsync(checkIn, checkOut, city, ct);

    public Task<HotelReadModel?> GetByIdWithAvailabilityAsync(Guid id, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default) =>
        _getHotelDetails.ExecuteAsync(id, checkIn, checkOut, ct);

    public Task<RoomAvailabilityDetails?> GetRoomByIdWithAvailabilityAsync(Guid roomId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default) =>
        _getRoomDetails.ExecuteAsync(roomId, checkIn, checkOut, ct);

    public Task<List<HotelReadModel>> GetFeaturedAsync(int count, CancellationToken ct = default) =>
        _getFeaturedHotels.ExecuteAsync(count, ct);
}
