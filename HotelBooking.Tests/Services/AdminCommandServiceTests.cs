using HotelBooking.Application.Admin;
using HotelBooking.Application.Caching;
using HotelBooking.Application.Media;
using HotelBooking.Application.Persistence;
using HotelBooking.Domain.Entities.Hotels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HotelBooking.Tests.Services;

public class AdminHotelCommandServiceTests
{
    [Fact]
    public async Task CreateHotelAsync_DeletesUploadedImages_WhenRepositorySaveFails()
    {
        var hotelRepository = new FakeHotelRepository { ThrowOnAdd = true };
        var imageStorage = new FakeImageStorage();
        var service = TestServices.CreateHotelService(hotelRepository, new FakeRoomRepository(), imageStorage);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateHotelAsync(
                new CreateHotelCommand("River", "Kyiv", "Street 1", null, [TestServices.CreateImageFile("one.png")]),
                CancellationToken.None));

        Assert.Single(imageStorage.SavedHotelImages);
        Assert.Single(imageStorage.DeletedImages);
        Assert.Equal(imageStorage.SavedHotelImages[0].StorageKey, imageStorage.DeletedImages[0].StorageKey);
    }

    [Fact]
    public async Task CreateHotelAsync_InvalidatesCitiesAndBumpsCatalogVersion_AfterSuccessfulSave()
    {
        var cache = new FakeAppCache();
        var service = TestServices.CreateHotelService(
            new FakeHotelRepository(),
            new FakeRoomRepository(),
            new FakeImageStorage(),
            cache: cache);

        await service.CreateHotelAsync(
            new CreateHotelCommand("River", "Kyiv", "Street 1", null, []),
            CancellationToken.None);

        Assert.Contains(HotelCacheKeys.Cities, cache.RemovedKeys);
        TestServices.AssertCatalogVersionWasBumped(cache);
    }

    [Fact]
    public async Task UpdateHotelAsync_InvalidatesCitiesAndBumpsCatalogVersion_AfterSuccessfulSave()
    {
        var hotel = TestServices.CreateHotel();
        var cache = new FakeAppCache();
        var service = TestServices.CreateHotelService(
            new FakeHotelRepository { Hotel = hotel },
            new FakeRoomRepository(),
            new FakeImageStorage(),
            cache: cache);

        await service.UpdateHotelAsync(
            new UpdateHotelCommand(hotel.Id, "River Prime", "Lviv", "Street 2", "Updated", [], []),
            CancellationToken.None);

        Assert.Contains(HotelCacheKeys.Cities, cache.RemovedKeys);
        TestServices.AssertCatalogVersionWasBumped(cache);
    }

    [Fact]
    public async Task CreateHotelAsync_CacheInvalidationFailure_DoesNotFailSuccessfulSave()
    {
        var cache = new FakeAppCache { ThrowOnRemove = true, ThrowOnSet = true };
        var hotelRepository = new FakeHotelRepository();
        var service = TestServices.CreateHotelService(
            hotelRepository,
            new FakeRoomRepository(),
            new FakeImageStorage(),
            cache: cache);

        var hotelId = await service.CreateHotelAsync(
            new CreateHotelCommand("River", "Kyiv", "Street 1", null, []),
            CancellationToken.None);

        Assert.Equal(hotelId, hotelRepository.Hotel?.Id);
    }

    [Fact]
    public async Task CreateHotelAsync_DoesNotDeleteUploadedImages_WhenCacheInvalidationIsCanceledAfterSave()
    {
        var cache = new FakeAppCache { CancelOnRemove = true };
        var hotelRepository = new FakeHotelRepository();
        var imageStorage = new FakeImageStorage();
        var service = TestServices.CreateHotelService(
            hotelRepository,
            new FakeRoomRepository(),
            imageStorage,
            cache: cache);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.CreateHotelAsync(
                new CreateHotelCommand("River", "Kyiv", "Street 1", null, [TestServices.CreateImageFile("one.png")]),
                CancellationToken.None));

        Assert.NotNull(hotelRepository.Hotel);
        Assert.Single(imageStorage.SavedHotelImages);
        Assert.Empty(imageStorage.DeletedImages);
    }
}

public class AdminRoomCommandServiceTests
{
    [Fact]
    public async Task UpdateRoomAsync_DeletesRemovedImages_AfterSuccessfulSave()
    {
        var room = TestServices.CreateRoomWithImage();
        var imageStorage = new FakeImageStorage();
        var roomRepository = new FakeRoomRepository { Room = room };
        var service = TestServices.CreateRoomService(new FakeHotelRepository(), roomRepository, imageStorage);

        await service.UpdateRoomAsync(
            new UpdateRoomCommand(
                room.Id,
                "Standard",
                null,
                null,
                2,
                100m,
                2,
                false,
                true,
                false,
                false,
                false,
                false,
                true,
                [],
                [room.Images.Single().Id]),
            CancellationToken.None);

        Assert.True(roomRepository.UpdateCalled);
        Assert.Single(imageStorage.DeletedImages);
        Assert.Equal("rooms/existing.webp", imageStorage.DeletedImages[0].StorageKey);
    }

    [Fact]
    public async Task CreateRoomAsync_RejectsTooManyFiles()
    {
        var service = TestServices.CreateRoomService(
            new FakeHotelRepository { Hotel = TestServices.CreateHotel() },
            new FakeRoomRepository(),
            new FakeImageStorage(),
            maxFilesPerUpload: 1);

        var hotelId = Guid.NewGuid();
        await Assert.ThrowsAsync<ImageUploadValidationException>(() =>
            service.CreateRoomAsync(
                new CreateRoomCommand(
                    hotelId,
                    "Standard",
                    null,
                    null,
                    2,
                    100m,
                    2,
                    false,
                    true,
                    false,
                    false,
                    false,
                    false,
                    true,
                    [TestServices.CreateImageFile("one.png"), TestServices.CreateImageFile("two.png")]),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateRoomAsync_BumpsCatalogVersion_AfterSuccessfulSave()
    {
        var hotelId = Guid.NewGuid();
        var cache = new FakeAppCache();
        var service = TestServices.CreateRoomService(
            new FakeHotelRepository { Hotel = TestServices.CreateHotel(hotelId) },
            new FakeRoomRepository(),
            new FakeImageStorage(),
            cache: cache);

        await service.CreateRoomAsync(TestServices.CreateRoomCommand(hotelId), CancellationToken.None);

        Assert.DoesNotContain(HotelCacheKeys.Cities, cache.RemovedKeys);
        TestServices.AssertCatalogVersionWasBumped(cache);
    }

    [Fact]
    public async Task UpdateRoomAsync_BumpsCatalogVersion_AfterSuccessfulSave()
    {
        var room = TestServices.CreateRoom();
        var cache = new FakeAppCache();
        var service = TestServices.CreateRoomService(
            new FakeHotelRepository(),
            new FakeRoomRepository { Room = room },
            new FakeImageStorage(),
            cache: cache);

        await service.UpdateRoomAsync(TestServices.UpdateRoomCommand(room.Id), CancellationToken.None);

        Assert.DoesNotContain(HotelCacheKeys.Cities, cache.RemovedKeys);
        TestServices.AssertCatalogVersionWasBumped(cache);
    }
}

internal static class TestServices
{
    public static AdminHotelCommandService CreateHotelService(
        FakeHotelRepository hotelRepository,
        FakeRoomRepository roomRepository,
        FakeImageStorage imageStorage,
        int maxFilesPerUpload = 8,
        IAppCache? cache = null)
    {
        hotelRepository.Hotel ??= CreateHotel();

        return new AdminHotelCommandService(
            hotelRepository,
            CreateImageLifecycleService(imageStorage, maxFilesPerUpload),
            cache ?? new FakeAppCache(),
            NullLogger<AdminHotelCommandService>.Instance);
    }

    public static AdminRoomCommandService CreateRoomService(
        FakeHotelRepository hotelRepository,
        FakeRoomRepository roomRepository,
        FakeImageStorage imageStorage,
        int maxFilesPerUpload = 8,
        IAppCache? cache = null)
    {
        hotelRepository.Hotel ??= CreateHotel();

        return new AdminRoomCommandService(
            hotelRepository,
            roomRepository,
            CreateImageLifecycleService(imageStorage, maxFilesPerUpload),
            cache ?? new FakeAppCache(),
            NullLogger<AdminRoomCommandService>.Instance);
    }

    public static ImageUploadFile CreateImageFile(string fileName)
    {
        return new ImageUploadFile(fileName, "image/png", 128, () => new MemoryStream([1, 2, 3]));
    }

    public static Hotel CreateHotel(Guid? id = null)
    {
        return Hotel.Create(id ?? Guid.NewGuid(), "River", "Kyiv", "Street 1");
    }

    public static Room CreateRoomWithImage(Guid? hotelId = null)
    {
        var room = CreateRoom(hotelId: hotelId);
        room.Images.Add(new RoomImage
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            StorageKey = "rooms/existing.webp",
            Url = "https://cdn.example.com/rooms/existing.webp",
            ContentType = "image/webp",
            SizeBytes = 123,
            Width = 800,
            Height = 600,
            AltText = "Standard",
            IsCover = true,
            SortOrder = 0
        });
        return room;
    }

    public static Room CreateRoom(Guid? id = null, Guid? hotelId = null)
    {
        return Room.Create(
            id ?? Guid.NewGuid(),
            hotelId ?? Guid.NewGuid(),
            "Standard",
            null,
            null,
            2,
            100m,
            2,
            includesBreakfast: false,
            hasPrivateBathroom: true,
            hasSaunaAccess: false,
            hasBalcony: false,
            hasWorkspace: false,
            hasAirConditioning: false,
            isActive: true);
    }

    public static CreateRoomCommand CreateRoomCommand(Guid hotelId)
    {
        return new CreateRoomCommand(
            hotelId,
            "Standard",
            null,
            null,
            2,
            100m,
            2,
            false,
            true,
            false,
            false,
            false,
            false,
            true,
            []);
    }

    public static UpdateRoomCommand UpdateRoomCommand(Guid roomId)
    {
        return new UpdateRoomCommand(
            roomId,
            "Standard",
            null,
            null,
            2,
            100m,
            2,
            false,
            true,
            false,
            false,
            false,
            false,
            true,
            [],
            []);
    }

    public static void AssertCatalogVersionWasBumped(FakeAppCache cache)
    {
        var setCall = Assert.Single(cache.SetCalls, c => c.Key == HotelCacheKeys.CatalogVersion);
        Assert.Equal(TimeSpan.FromDays(365), setCall.Ttl);
        var catalogVersion = Assert.IsType<string>(setCall.Value);
        Assert.False(string.IsNullOrWhiteSpace(catalogVersion));
    }

    private static AdminImageLifecycleService CreateImageLifecycleService(FakeImageStorage imageStorage, int maxFilesPerUpload)
    {
        return new AdminImageLifecycleService(
            new FakeImageProcessor(),
            imageStorage,
            Options.Create(new ImageUploadOptions { MaxFilesPerUpload = maxFilesPerUpload }),
            NullLogger<AdminImageLifecycleService>.Instance);
    }
}

internal sealed class FakeHotelRepository : IHotelRepository
{
    public Hotel? Hotel { get; set; }
    public List<Hotel>? Hotels { get; set; }
    public bool ThrowOnAdd { get; set; }

    public Task<List<Hotel>> GetWithActiveRoomsAsync(string? city, CancellationToken ct) => Task.FromResult(new List<Hotel>());
    public Task<Hotel?> GetWithRoomsByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Hotel);
    public Task<List<string>> GetDistinctCitiesAsync(CancellationToken ct) => Task.FromResult(new List<string>());
    public Task<List<Hotel>> GetFeaturedAsync(int count, CancellationToken ct) => Task.FromResult(new List<Hotel>());
    public Task<List<Hotel>> GetAllAsync(CancellationToken ct) => Task.FromResult(Hotels ?? (Hotel is null ? [] : [Hotel]));
    public Task<Hotel?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(FindHotel(id));
    public Task<Hotel?> GetByIdWithImagesAsync(Guid id, CancellationToken ct) => Task.FromResult(FindHotel(id));

    public Task AddAsync(Hotel hotel, CancellationToken ct)
    {
        if (ThrowOnAdd)
        {
            throw new InvalidOperationException("DB write failed.");
        }

        Hotel = hotel;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Hotel hotel, CancellationToken ct)
    {
        Hotel = hotel;
        return Task.CompletedTask;
    }

    private Hotel? FindHotel(Guid id)
    {
        return Hotels?.FirstOrDefault(h => h.Id == id) ?? (Hotel?.Id == id ? Hotel : null);
    }
}

internal sealed class FakeRoomRepository : IRoomRepository
{
    public Room? Room { get; set; }
    public bool UpdateCalled { get; private set; }

    public Task<Room?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Room?.Id == id ? Room : null);
    public Task<Room?> GetByIdWithImagesAsync(Guid id, CancellationToken ct) => Task.FromResult(Room?.Id == id ? Room : null);
    public Task<Room?> GetByIdWithHotelAndImagesAsync(Guid id, CancellationToken ct) => Task.FromResult(Room?.Id == id ? Room : null);

    public Task AddAsync(Room room, CancellationToken ct)
    {
        Room = room;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Room room, CancellationToken ct)
    {
        UpdateCalled = true;
        Room = room;
        return Task.CompletedTask;
    }
}

internal sealed class FakeImageProcessor : IImageProcessor
{
    public Task<ProcessedImage> ProcessAsync(ImageUploadFile file, CancellationToken ct)
    {
        return Task.FromResult(new ProcessedImage(new MemoryStream([1, 2, 3]), file.FileName, "image/webp", ".webp", 3, 640, 480));
    }
}

internal sealed class FakeImageStorage : IImageStorage
{
    public List<StoredImage> SavedHotelImages { get; } = [];
    public List<(string StorageKey, string PublicUrl)> DeletedImages { get; } = [];

    public Task<StoredImage> SaveHotelImageAsync(Guid hotelId, ProcessedImage image, CancellationToken ct)
    {
        var stored = new StoredImage($"hotels/{hotelId:N}/image.webp", $"https://cdn.example.com/hotels/{hotelId:N}/image.webp", image.ContentType, image.SizeBytes, image.Width, image.Height);
        SavedHotelImages.Add(stored);
        return Task.FromResult(stored);
    }

    public Task<StoredImage> SaveRoomImageAsync(Guid roomId, ProcessedImage image, CancellationToken ct)
    {
        return Task.FromResult(new StoredImage($"rooms/{roomId:N}/image.webp", $"https://cdn.example.com/rooms/{roomId:N}/image.webp", image.ContentType, image.SizeBytes, image.Width, image.Height));
    }

    public Task DeleteAsync(string storageKey, string publicUrl, CancellationToken ct)
    {
        DeletedImages.Add((storageKey, publicUrl));
        return Task.CompletedTask;
    }
}

internal sealed class FakeAppCache : IAppCache
{
    public List<string> RemovedKeys { get; } = [];
    public List<(string Key, object? Value, TimeSpan Ttl)> SetCalls { get; } = [];
    public bool ThrowOnRemove { get; init; }
    public bool ThrowOnSet { get; init; }
    public bool CancelOnRemove { get; init; }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        => Task.FromResult<T?>(default);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        if (ThrowOnSet)
        {
            throw new InvalidOperationException("Cache set failed.");
        }

        SetCalls.Add((key, value, ttl));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        if (CancelOnRemove)
        {
            throw new OperationCanceledException();
        }

        if (ThrowOnRemove)
        {
            throw new InvalidOperationException("Cache remove failed.");
        }

        RemovedKeys.Add(key);
        return Task.CompletedTask;
    }
}
