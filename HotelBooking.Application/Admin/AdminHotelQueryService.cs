using HotelBooking.Application.Persistence;
using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Application.Admin;

public sealed class AdminHotelQueryService :
    IAdminHotelQueryService,
    IGetAdminHotelListQuery,
    IGetAdminHotelEditDetailsQuery,
    IGetCreateRoomDetailsQuery,
    IGetEditRoomDetailsQuery,
    IAdminHotelExistsQuery
{
    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomRepository _roomRepository;

    public AdminHotelQueryService(IHotelRepository hotelRepository, IRoomRepository roomRepository)
    {
        _hotelRepository = hotelRepository;
        _roomRepository = roomRepository;
    }

    public async Task<IReadOnlyList<AdminHotelListItem>> GetHotelListAsync(CancellationToken ct = default)
    {
        var hotels = await _hotelRepository.GetAllAsync(ct);
        return hotels
            .Select(h =>
            {
                var cover = GetCoverImage(h.Images);
                return new AdminHotelListItem(
                    h.Id,
                    h.Name,
                    h.City,
                    h.Address,
                    cover?.Url,
                    cover?.Width,
                    cover?.Height);
            })
            .ToList();
    }

    public Task<IReadOnlyList<AdminHotelListItem>> ExecuteAsync(CancellationToken ct = default) =>
        GetHotelListAsync(ct);

    public async Task<AdminHotelEditDetails?> GetHotelEditDetailsAsync(Guid hotelId, CancellationToken ct = default)
    {
        var hotel = await _hotelRepository.GetByIdWithImagesAsync(hotelId, ct);
        if (hotel is null)
        {
            return null;
        }

        return new AdminHotelEditDetails(
            hotel.Id,
            hotel.Name,
            hotel.City,
            hotel.Address,
            hotel.Description,
            MapHotelImages(hotel).ToList(),
            hotel.Rooms
                .OrderBy(r => r.PricePerNight)
                .Select(r =>
                {
                    var cover = GetCoverImage(r.Images);
                    return new AdminRoomListItem(
                        r.Id,
                        r.Name,
                        r.Capacity,
                        r.Quantity,
                        r.PricePerNight,
                        r.IsActive,
                        cover?.Url,
                        cover?.Width,
                        cover?.Height);
                })
                .ToList());
    }

    public Task<AdminHotelEditDetails?> ExecuteForHotelAsync(Guid hotelId, CancellationToken ct = default) =>
        GetHotelEditDetailsAsync(hotelId, ct);

    public async Task<AdminRoomFormDetails?> GetCreateRoomDetailsAsync(Guid hotelId, CancellationToken ct = default)
    {
        var hotel = await _hotelRepository.GetByIdAsync(hotelId, ct);
        if (hotel is null)
        {
            return null;
        }

        return new AdminRoomFormDetails(
            null,
            hotel.Id,
            hotel.Name,
            string.Empty,
            null,
            null,
            0,
            1000m,
            1,
            false,
            false,
            false,
            false,
            false,
            false,
            true,
            []);
    }

    Task<AdminRoomFormDetails?> IGetCreateRoomDetailsQuery.ExecuteForHotelAsync(Guid hotelId, CancellationToken ct) =>
        GetCreateRoomDetailsAsync(hotelId, ct);

    public async Task<AdminRoomFormDetails?> GetEditRoomDetailsAsync(Guid roomId, CancellationToken ct = default)
    {
        var room = await _roomRepository.GetByIdWithImagesAsync(roomId, ct);
        if (room is null)
        {
            return null;
        }

        var hotel = await _hotelRepository.GetByIdAsync(room.HotelId, ct);
        if (hotel is null)
        {
            return null;
        }

        return new AdminRoomFormDetails(
            room.Id,
            room.HotelId,
            hotel.Name,
            room.Name,
            room.Description,
            room.Amenities,
            room.Capacity,
            room.PricePerNight,
            room.Quantity,
            room.IncludesBreakfast,
            room.HasPrivateBathroom,
            room.HasSaunaAccess,
            room.HasBalcony,
            room.HasWorkspace,
            room.HasAirConditioning,
            room.IsActive,
            MapRoomImages(room).ToList());
    }

    public Task<AdminRoomFormDetails?> ExecuteForRoomAsync(Guid roomId, CancellationToken ct = default) =>
        GetEditRoomDetailsAsync(roomId, ct);

    public async Task<bool> HotelExistsAsync(Guid hotelId, CancellationToken ct = default)
    {
        return await _hotelRepository.GetByIdAsync(hotelId, ct) is not null;
    }

    Task<bool> IAdminHotelExistsQuery.ExecuteForHotelAsync(Guid hotelId, CancellationToken ct) =>
        HotelExistsAsync(hotelId, ct);

    private static IEnumerable<AdminImageItem> MapHotelImages(Hotel hotel)
    {
        return hotel.Images
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.SortOrder)
            .Select(i => new AdminImageItem(
                i.Id,
                i.Url,
                i.AltText ?? hotel.Name,
                i.Width,
                i.Height,
                i.IsCover));
    }

    private static IEnumerable<AdminImageItem> MapRoomImages(Room room)
    {
        return room.Images
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.SortOrder)
            .Select(i => new AdminImageItem(
                i.Id,
                i.Url,
                i.AltText ?? room.Name,
                i.Width,
                i.Height,
                i.IsCover));
    }

    private static HotelImage? GetCoverImage(IEnumerable<HotelImage> images)
    {
        return images
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.SortOrder)
            .FirstOrDefault();
    }

    private static RoomImage? GetCoverImage(IEnumerable<RoomImage> images)
    {
        return images
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.SortOrder)
            .FirstOrDefault();
    }
}
