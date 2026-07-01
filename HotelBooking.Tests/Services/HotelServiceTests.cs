using HotelBooking.Application.Caching;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Persistence;
using HotelBooking.Domain.Entities.Bookings;
using HotelBooking.Domain.Entities.Hotels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HotelBooking.Tests.Services;

public class HotelServiceTests
{
    [Fact]
    public async Task GetAvailableCitiesAsync_CacheHitSkipsRepository()
    {
        var cache = new FakeAppCache();
        cache.Seed(HotelCacheKeys.Cities, new List<string> { "Kyiv" });
        var repository = new FakeHotelRepository { Cities = ["Lviv"] };
        var service = CreateService(repository, cache: cache);

        var result = await service.GetAvailableCitiesAsync();

        Assert.Equal(["Kyiv"], result);
        Assert.Equal(0, repository.CitiesReadCount);
    }

    [Fact]
    public async Task GetAvailableCitiesAsync_CacheMissReadsRepositoryAndWritesCache()
    {
        var cache = new FakeAppCache();
        var repository = new FakeHotelRepository { Cities = ["Kyiv", "Lviv"] };
        var service = CreateService(repository, cache: cache);

        var result = await service.GetAvailableCitiesAsync();

        Assert.Equal(["Kyiv", "Lviv"], result);
        Assert.Equal(1, repository.CitiesReadCount);
        var setCall = Assert.Single(cache.SetCalls);
        Assert.Equal(HotelCacheKeys.Cities, setCall.Key);
        Assert.Equal(TimeSpan.FromHours(12), setCall.Ttl);
    }

    [Fact]
    public async Task GetFeaturedAsync_CachesDtoSnapshotWithTtl()
    {
        var cache = new FakeAppCache();
        var hotel = CreateHotelWithRoom();
        var repository = new FakeHotelRepository { FeaturedHotels = [hotel] };
        var service = CreateService(repository, cache: cache);

        var result = await service.GetFeaturedAsync(6);

        Assert.Single(result);
        Assert.Equal(1, repository.FeaturedReadCount);
        var setCall = Assert.Single(cache.SetCalls);
        Assert.Equal(HotelCacheKeys.FeaturedHotels(6, HotelCacheKeys.DefaultCatalogVersion), setCall.Key);
        Assert.Equal(TimeSpan.FromMinutes(15), setCall.Ttl);
        var cached = await cache.GetAsync<List<HotelReadSnapshot>>(
            HotelCacheKeys.FeaturedHotels(6, HotelCacheKeys.DefaultCatalogVersion));
        Assert.NotNull(cached);
        Assert.Equal(hotel.Id, cached.Single().Id);
    }

    [Fact]
    public async Task GetAvailableHotelsAsync_UsesNormalizedSearchKeyAndCachesAvailabilitySnapshot()
    {
        var cache = new FakeAppCache();
        cache.Seed(HotelCacheKeys.CatalogVersion, "catalog-123");
        var hotel = CreateHotelWithRoom();
        var repository = new FakeHotelRepository { SearchHotels = [hotel] };
        var service = CreateService(repository, cache: cache);

        var result = await service.GetAvailableHotelsAsync(
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 2),
            " Kyiv ");

        Assert.Single(result);
        var setCall = Assert.Single(cache.SetCalls);
        Assert.Equal("hotel-search:kyiv:2026-05-01:2026-05-02:catalog:catalog-123:availability:0:v1", setCall.Key);
        Assert.Equal(TimeSpan.FromSeconds(60), setCall.Ttl);
    }

    [Fact]
    public async Task GetAvailableHotelsAsync_UsesDefaultCatalogVersion_WhenVersionCacheMisses()
    {
        var cache = new FakeAppCache();
        var hotel = CreateHotelWithRoom();
        var repository = new FakeHotelRepository { SearchHotels = [hotel] };
        var service = CreateService(repository, cache: cache);

        var result = await service.GetAvailableHotelsAsync(
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 2),
            "Kyiv");

        Assert.Single(result);
        var setCall = Assert.Single(cache.SetCalls);
        Assert.Equal("hotel-search:kyiv:2026-05-01:2026-05-02:catalog:0:availability:0:v1", setCall.Key);
    }

    [Fact]
    public async Task GetAvailableHotelsAsync_CatalogVersionExceptionFallsBackToDefaultAndRepository()
    {
        var cache = new FakeAppCache { ThrowOnGet = true };
        var hotel = CreateHotelWithRoom();
        var repository = new FakeHotelRepository { SearchHotels = [hotel] };
        var service = CreateService(repository, cache: cache);

        var result = await service.GetAvailableHotelsAsync(
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 2),
            "Kyiv");

        Assert.Single(result);
        Assert.Equal(1, repository.SearchReadCount);
        var setCall = Assert.Single(cache.SetCalls);
        Assert.Equal("hotel-search:kyiv:2026-05-01:2026-05-02:catalog:0:availability:0:v1", setCall.Key);
    }

    [Fact]
    public async Task GetAvailableCitiesAsync_CacheExceptionFallsBackToRepository()
    {
        var repository = new FakeHotelRepository { Cities = ["Kyiv"] };
        var service = CreateService(repository, cache: new FakeAppCache { ThrowOnGet = true });

        var result = await service.GetAvailableCitiesAsync();

        Assert.Equal(["Kyiv"], result);
        Assert.Equal(1, repository.CitiesReadCount);
    }

    [Fact]
    public async Task GetRoomByIdWithAvailabilityAsync_ReturnsAvailableQuantity()
    {
        var room = CreateRoom(quantity: 3);
        var bookingRepository = new FakeBookingRepository
        {
            OverlapsByRoom = new Dictionary<Guid, int> { [room.Id] = 1 }
        };
        var service = CreateService(
            roomRepository: new FakeRoomRepository(room),
            bookingRepository: bookingRepository);

        var result = await service.GetRoomByIdWithAvailabilityAsync(room.Id, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 2));

        Assert.NotNull(result);
        Assert.Equal(2, result.AvailableQuantity);
        Assert.Equal(room.Id, result.Id);
        Assert.Equal(room.Quantity, result.Quantity);
        Assert.Equal(room.PricePerNight, result.PricePerNight);
    }

    [Fact]
    public async Task GetRoomByIdWithAvailabilityAsync_ReturnsNull_WhenRoomInactive()
    {
        var room = CreateRoom(name: "Closed", isActive: false);
        var service = CreateService(roomRepository: new FakeRoomRepository(room));

        var result = await service.GetRoomByIdWithAvailabilityAsync(room.Id, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 2));

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAvailableHotelsAsync_ReturnsReadModelsWithoutMutatingRepositoryEntities()
    {
        var unavailableRoom = CreateRoom(name: "Sold out", quantity: 1);
        var availableRoom = CreateRoom(name: "Available", quantity: 2);
        var hotel = Hotel.Create(Guid.NewGuid(), "River", "Kyiv", "Street 1");
        hotel.Rooms.Add(unavailableRoom);
        hotel.Rooms.Add(availableRoom);

        var service = CreateService(
            hotelRepository: new FakeHotelRepository { SearchHotels = [hotel] },
            bookingRepository: new FakeBookingRepository
            {
                OverlapsByRoom = new Dictionary<Guid, int> { [unavailableRoom.Id] = 1 }
            });

        var result = await service.GetAvailableHotelsAsync(
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 2),
            "Kyiv");

        var readModel = Assert.Single(result);
        var room = Assert.Single(readModel.Rooms);
        Assert.Equal(availableRoom.Id, room.Id);
        Assert.Equal(2, hotel.Rooms.Count);
    }

    private static HotelService CreateService(
        FakeHotelRepository? hotelRepository = null,
        FakeRoomRepository? roomRepository = null,
        FakeBookingRepository? bookingRepository = null,
        IAppCache? cache = null)
    {
        var resolvedHotelRepository = hotelRepository ?? new FakeHotelRepository();
        var resolvedRoomRepository = roomRepository ?? new FakeRoomRepository(CreateRoom(name: "Room"));
        var resolvedBookingRepository = bookingRepository ?? new FakeBookingRepository();
        var queryCache = new HotelQueryCache(cache ?? new FakeAppCache(), NullLogger<HotelQueryCache>.Instance);

        return new HotelService(
            new GetAvailableCitiesQuery(resolvedHotelRepository, queryCache),
            new SearchAvailableHotelsQuery(resolvedHotelRepository, resolvedBookingRepository, queryCache),
            new GetHotelDetailsWithAvailabilityQuery(resolvedHotelRepository, resolvedBookingRepository),
            new GetRoomDetailsWithAvailabilityQuery(resolvedRoomRepository, resolvedBookingRepository),
            new GetFeaturedHotelsQuery(resolvedHotelRepository, queryCache));
    }

    private static Hotel CreateHotelWithRoom()
    {
        var hotelId = Guid.NewGuid();
        var roomId = Guid.NewGuid();

        var hotel = Hotel.Create(hotelId, "River", "Kyiv", "Street 1", "Central hotel");
        hotel.Images.Add(new HotelImage
        {
            Id = Guid.NewGuid(),
            HotelId = hotelId,
            StorageKey = "hotels/cover.webp",
            Url = "https://cdn.example.com/hotels/cover.webp",
            ContentType = "image/webp",
            SizeBytes = 123,
            Width = 800,
            Height = 600,
            AltText = "River",
            IsCover = true,
            SortOrder = 0
        });

        var room = CreateRoom(roomId, hotelId, "Deluxe", quantity: 2, pricePerNight: 120m);
        room.Images.Add(new RoomImage
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            StorageKey = "rooms/cover.webp",
            Url = "https://cdn.example.com/rooms/cover.webp",
            ContentType = "image/webp",
            SizeBytes = 123,
            Width = 800,
            Height = 600,
            AltText = "Deluxe",
            IsCover = true,
            SortOrder = 0
        });

        hotel.Rooms.Add(room);
        return hotel;
    }

    private static Room CreateRoom(
        string name = "Deluxe",
        bool isActive = true,
        int quantity = 1,
        decimal pricePerNight = 100m)
    {
        return CreateRoom(Guid.NewGuid(), Guid.NewGuid(), name, isActive, quantity, pricePerNight);
    }

    private static Room CreateRoom(
        Guid id,
        Guid hotelId,
        string name,
        bool isActive = true,
        int quantity = 1,
        decimal pricePerNight = 100m)
    {
        return Room.Create(
            id,
            hotelId,
            name,
            null,
            null,
            2,
            pricePerNight,
            quantity,
            includesBreakfast: false,
            hasPrivateBathroom: true,
            hasSaunaAccess: false,
            hasBalcony: false,
            hasWorkspace: false,
            hasAirConditioning: false,
            isActive);
    }

    private sealed class FakeHotelRepository : IHotelRepository
    {
        public int CitiesReadCount { get; private set; }
        public int FeaturedReadCount { get; private set; }
        public int SearchReadCount { get; private set; }
        public List<string> Cities { get; init; } = [];
        public List<Hotel> FeaturedHotels { get; init; } = [];
        public List<Hotel> SearchHotels { get; init; } = [];

        public Task<List<Hotel>> GetWithActiveRoomsAsync(string? city, CancellationToken ct)
        {
            SearchReadCount++;
            return Task.FromResult(SearchHotels);
        }

        public Task<Hotel?> GetWithRoomsByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Hotel?>(null);
        public Task<List<string>> GetDistinctCitiesAsync(CancellationToken ct)
        {
            CitiesReadCount++;
            return Task.FromResult(Cities);
        }

        public Task<List<Hotel>> GetFeaturedAsync(int count, CancellationToken ct)
        {
            FeaturedReadCount++;
            return Task.FromResult(FeaturedHotels.Take(count).ToList());
        }

        public Task<List<Hotel>> GetAllAsync(CancellationToken ct) => Task.FromResult(new List<Hotel>());
        public Task<Hotel?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Hotel?>(null);
        public Task<Hotel?> GetByIdWithImagesAsync(Guid id, CancellationToken ct) => Task.FromResult<Hotel?>(null);
        public Task AddAsync(Hotel hotel, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Hotel hotel, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeRoomRepository : IRoomRepository
    {
        private readonly Room _room;

        public FakeRoomRepository(Room room)
        {
            _room = room;
        }

        public Task<Room?> GetByIdAsync(Guid id, CancellationToken ct)
            => Task.FromResult(id == _room.Id ? _room : null);

        public Task<Room?> GetByIdWithImagesAsync(Guid id, CancellationToken ct)
            => GetByIdAsync(id, ct);

        public Task<Room?> GetByIdWithHotelAndImagesAsync(Guid id, CancellationToken ct)
            => GetByIdAsync(id, ct);

        public Task AddAsync(Room room, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Room room, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeBookingRepository : IBookingRepository
    {
        public Dictionary<Guid, int> OverlapsByRoom { get; init; } = new();

        public Task<Dictionary<Guid, int>> GetOverlappingActiveBookingsCountByRoomAsync(IEnumerable<Guid> roomIds, DateOnly checkIn, DateOnly checkOut, CancellationToken ct)
            => Task.FromResult(OverlapsByRoom);

        public Task<bool> TryAddIfAvailableAsync(Booking booking, int roomQuantity, CancellationToken ct)
            => Task.FromResult(true);

        public Task<List<Booking>> GetByUserAsync(string userId, CancellationToken ct)
            => Task.FromResult(new List<Booking>());
    }

    private sealed class FakeAppCache : IAppCache
    {
        private readonly Dictionary<string, object?> _values = new();

        public bool ThrowOnGet { get; init; }
        public bool ThrowOnSet { get; init; }
        public List<(string Key, TimeSpan Ttl)> SetCalls { get; } = [];

        public void Seed<T>(string key, T value)
        {
            _values[key] = value;
        }

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            if (ThrowOnGet)
            {
                throw new InvalidOperationException("Cache unavailable.");
            }

            return Task.FromResult(_values.TryGetValue(key, out var value) ? (T?)value : default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
        {
            if (ThrowOnSet)
            {
                throw new InvalidOperationException("Cache unavailable.");
            }

            SetCalls.Add((key, ttl));
            _values[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }
    }
}
