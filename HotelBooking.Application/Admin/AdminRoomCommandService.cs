using HotelBooking.Application.Caching;
using HotelBooking.Application.Persistence;
using HotelBooking.Domain.Entities.Hotels;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Admin;

public sealed class AdminRoomCommandService : IAdminRoomCommandService, ICreateRoomUseCase, IUpdateRoomUseCase
{
    private static readonly TimeSpan CatalogVersionTtl = TimeSpan.FromDays(365);

    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly AdminImageLifecycleService _images;
    private readonly IAppCache _cache;
    private readonly ILogger<AdminRoomCommandService> _logger;

    public AdminRoomCommandService(
        IHotelRepository hotelRepository,
        IRoomRepository roomRepository,
        AdminImageLifecycleService images,
        IAppCache cache,
        ILogger<AdminRoomCommandService> logger)
    {
        _hotelRepository = hotelRepository;
        _roomRepository = roomRepository;
        _images = images;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Guid> CreateRoomAsync(CreateRoomCommand command, CancellationToken ct = default)
    {
        _images.EnsureFileCount(command.Photos);

        var hotel = await _hotelRepository.GetByIdAsync(command.HotelId, ct)
            ?? throw new InvalidOperationException($"Hotel {command.HotelId} was not found.");

        var room = Room.Create(
            hotel.Id,
            command.Name,
            command.Description,
            command.Amenities,
            command.Capacity,
            command.PricePerNight,
            command.Quantity,
            command.Features,
            command.IsActive);

        var newImages = new List<RoomImage>();
        await _images.AddRoomImagesAsync(room, command.Photos, newImages, ct);

        try
        {
            room.NormalizeImages();
            await _roomRepository.AddAsync(room, ct);
        }
        catch
        {
            await _images.CleanupRoomImagesAsync(newImages, ct);
            throw;
        }

        await BumpCatalogVersionAsync(ct);
        return room.Id;
    }

    public Task<Guid> ExecuteAsync(CreateRoomCommand command, CancellationToken ct = default) =>
        CreateRoomAsync(command, ct);

    public async Task UpdateRoomAsync(UpdateRoomCommand command, CancellationToken ct = default)
    {
        _images.EnsureFileCount(command.Photos);

        var room = await _roomRepository.GetByIdWithImagesAsync(command.RoomId, ct)
            ?? throw new InvalidOperationException($"Room {command.RoomId} was not found.");

        room.UpdateDetails(
            command.Name,
            command.Description,
            command.Amenities,
            command.Capacity,
            command.PricePerNight,
            command.Quantity,
            command.Features,
            command.IsActive);

        var newImages = new List<RoomImage>();
        await _images.AddRoomImagesAsync(room, command.Photos, newImages, ct);

        var removedImages = _images.RemoveRoomImages(room, command.RemoveImageIds);
        room.NormalizeImages();

        try
        {
            await _roomRepository.UpdateAsync(room, ct);
        }
        catch
        {
            await _images.CleanupRoomImagesAsync(newImages, ct);
            throw;
        }

        await _images.DeleteRoomImagesAsync(removedImages, room.Id, ct);
        await BumpCatalogVersionAsync(ct);
    }

    public Task ExecuteAsync(UpdateRoomCommand command, CancellationToken ct = default) =>
        UpdateRoomAsync(command, ct);

    private async Task BumpCatalogVersionAsync(CancellationToken ct)
    {
        try
        {
            await _cache.SetAsync(
                HotelCacheKeys.CatalogVersion,
                Guid.NewGuid().ToString("N"),
                CatalogVersionTtl,
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to bump hotel read cache catalog version.");
        }
    }
}
