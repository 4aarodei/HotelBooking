using HotelBooking.Application.Caching;
using HotelBooking.Application.Persistence;
using HotelBooking.Domain.Entities.Hotels;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Admin;

public sealed class AdminHotelCommandService : IAdminHotelCommandService, ICreateHotelUseCase, IUpdateHotelUseCase
{
    private static readonly TimeSpan CatalogVersionTtl = TimeSpan.FromDays(365);

    private readonly IHotelRepository _hotelRepository;
    private readonly AdminImageLifecycleService _images;
    private readonly IAppCache _cache;
    private readonly ILogger<AdminHotelCommandService> _logger;

    public AdminHotelCommandService(
        IHotelRepository hotelRepository,
        AdminImageLifecycleService images,
        IAppCache cache,
        ILogger<AdminHotelCommandService> logger)
    {
        _hotelRepository = hotelRepository;
        _images = images;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Guid> CreateHotelAsync(CreateHotelCommand command, CancellationToken ct = default)
    {
        _images.EnsureFileCount(command.Photos);

        var hotel = Hotel.Create(command.Name, command.City, command.Address, command.Description);

        var newImages = new List<HotelImage>();
        await _images.AddHotelImagesAsync(hotel, command.Photos, newImages, ct);

        try
        {
            hotel.NormalizeImages();
            await _hotelRepository.AddAsync(hotel, ct);
        }
        catch
        {
            await _images.CleanupHotelImagesAsync(newImages, ct);
            throw;
        }

        await InvalidateHotelReadCacheAsync(ct);
        return hotel.Id;
    }

    public Task<Guid> ExecuteAsync(CreateHotelCommand command, CancellationToken ct = default) =>
        CreateHotelAsync(command, ct);

    public async Task UpdateHotelAsync(UpdateHotelCommand command, CancellationToken ct = default)
    {
        _images.EnsureFileCount(command.Photos);

        var hotel = await _hotelRepository.GetByIdWithImagesAsync(command.HotelId, ct)
            ?? throw new InvalidOperationException($"Hotel {command.HotelId} was not found.");

        hotel.UpdateDetails(command.Name, command.City, command.Address, command.Description);

        var newImages = new List<HotelImage>();
        await _images.AddHotelImagesAsync(hotel, command.Photos, newImages, ct);

        var removedImages = _images.RemoveHotelImages(hotel, command.RemoveImageIds);
        hotel.NormalizeImages();

        try
        {
            await _hotelRepository.UpdateAsync(hotel, ct);
        }
        catch
        {
            await _images.CleanupHotelImagesAsync(newImages, ct);
            throw;
        }

        await _images.DeleteHotelImagesAsync(removedImages, hotel.Id, ct);
        await InvalidateHotelReadCacheAsync(ct);
    }

    public Task ExecuteAsync(UpdateHotelCommand command, CancellationToken ct = default) =>
        UpdateHotelAsync(command, ct);

    private async Task InvalidateHotelReadCacheAsync(CancellationToken ct)
    {
        await InvalidateCitiesCacheAsync(ct);
        await BumpCatalogVersionAsync(ct);
    }

    private async Task InvalidateCitiesCacheAsync(CancellationToken ct)
    {
        try
        {
            await _cache.RemoveAsync(HotelCacheKeys.Cities, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to invalidate hotel cities cache.");
        }
    }

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
