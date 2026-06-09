using HotelBooking.Application.Caching;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Services;
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
        var room = new Room
        {
            Id = Guid.NewGuid(),
            HotelId = Guid.NewGuid(),
            Name = "Deluxe",
            Quantity = 3,
            IsActive = true,
            Hotel = new Hotel { Id = Guid.NewGuid(), Name = "River", City = "Kyiv", Address = "Street 1" }
        };
        var bookingRepository = new FakeBookingRepository
        {
            OverlapsByRoom = new Dictionary<Guid, int> { [room.Id] = 1 }
        };
        var service = new HotelService(
            new FakeHotelRepository(),
            new FakeRoomRepository(room),
            bookingRepository,
            new FakeAppCache(),
            NullLogger<HotelService>.Instance);

        var result = await service.GetRoomByIdWithAvailabilityAsync(room.Id, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 2));

        Assert.NotNull(result);
        Assert.Equal(2, result.AvailableQuantity);
        Assert.Same(room, result.Room);
    }

    [Fact]
    public async Task GetRoomByIdWithAvailabilityAsync_ReturnsNull_WhenRoomInactive()
    {
        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = "Closed",
            Quantity = 1,
            IsActive = false
        };
        var service = new HotelService(
            new FakeHotelRepository(),
            new FakeRoomRepository(room),
            new FakeBookingRepository(),
            new FakeAppCache(),
            NullLogger<HotelService>.Instance);

        var result = await service.GetRoomByIdWithAvailabilityAsync(room.Id, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 2));

        Assert.Null(result);
    }

    private static HotelService CreateService(
        FakeHotelRepository? hotelRepository = null,
        FakeRoomRepository? roomRepository = null,
        FakeBookingRepository? bookingRepository = null,
        IAppCache? cache = null)
    {
        return new HotelService(
            hotelRepository ?? new FakeHotelRepository(),
            roomRepository ?? new FakeRoomRepository(new Room { Id = Guid.NewGuid(), Name = "Room", IsActive = true }),
            bookingRepository ?? new FakeBookingRepository(),
            cache ?? new FakeAppCache(),
            NullLogger<HotelService>.Instance);
    }

    private static Hotel CreateHotelWithRoom()
    {
        var hotelId = Guid.NewGuid();
        var roomId = Guid.NewGuid();

        return new Hotel
        {
            Id = hotelId,
            Name = "River",
            City = "Kyiv",
            Address = "Street 1",
            Description = "Central hotel",
            Images =
            [
                new HotelImage
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
                }
            ],
            Rooms =
            [
                new Room
                {
                    Id = roomId,
                    HotelId = hotelId,
                    Name = "Deluxe",
                    Capacity = 2,
                    Quantity = 2,
                    PricePerNight = 120m,
                    IsActive = true,
                    Images =
                    [
                        new RoomImage
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
                        }
                    ]
                }
            ]
        };
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
