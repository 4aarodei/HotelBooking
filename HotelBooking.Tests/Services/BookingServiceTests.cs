using HotelBooking.Application.Caching;
using HotelBooking.Application.Bookings;
using HotelBooking.Application.Common;
using HotelBooking.Application.Persistence;
using HotelBooking.Domain.Entities.Bookings;
using HotelBooking.Domain.Entities.Hotels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HotelBooking.Tests.Services;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateBookingAsync_Throws_WhenCheckOutNotAfterCheckIn()
    {
        var service = CreateService(new FakeBookingRepository(), new FakeRoomRepository(CreateRoom(quantity: 2)));

        await Assert.ThrowsAsync<BookingRuleViolationException>(() =>
            service.CreateBookingAsync("user-1", Guid.NewGuid(), new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 20)));
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenRoomInactive()
    {
        var room = CreateRoom(isActive: false, quantity: 2);
        var service = CreateService(new FakeBookingRepository(), new FakeRoomRepository(room));

        await Assert.ThrowsAsync<BookingRuleViolationException>(() =>
            service.CreateBookingAsync("user-1", room.Id, new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 22)));
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenRoomNotFound()
    {
        var room = CreateRoom(quantity: 2);
        var service = CreateService(new FakeBookingRepository(), new FakeRoomRepository(room));

        await Assert.ThrowsAsync<BookingRuleViolationException>(() =>
            service.CreateBookingAsync("user-1", Guid.NewGuid(), new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 22)));
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenNoAvailability()
    {
        var room = CreateRoom(quantity: 1, pricePerNight: 100m);
        var repository = new FakeBookingRepository
        {
            OverlapsByRoom = new Dictionary<Guid, int> { [room.Id] = 1 }
        };
        var service = CreateService(repository, new FakeRoomRepository(room));

        await Assert.ThrowsAsync<BookingRuleViolationException>(() =>
            service.CreateBookingAsync("user-1", room.Id, new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 22)));
    }

    [Fact]
    public async Task CreateBookingAsync_CreatesPendingBookingWithCalculatedTotals()
    {
        var room = CreateRoom(quantity: 3, pricePerNight: 120m);
        var repository = new FakeBookingRepository();
        var clock = new FakeClock { UtcNow = new DateTimeOffset(2026, 4, 19, 10, 30, 0, TimeSpan.Zero) };
        var cache = new FakeAppCache();
        var service = CreateService(repository, new FakeRoomRepository(room), clock, cache);

        var result = await service.CreateBookingAsync("user-1", room.Id, new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 23));

        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Equal(3, result.Nights);
        Assert.Equal(360m, result.TotalPrice);
        Assert.Equal(clock.UtcNow, result.CreatedAtUtc);
        Assert.Single(repository.AddedBookings);
        var setCall = Assert.Single(cache.SetCalls);
        Assert.Equal(HotelCacheKeys.AvailabilityVersion, setCall.Key);
        Assert.Equal(TimeSpan.FromDays(365), setCall.Ttl);
    }

    [Fact]
    public async Task CreateBookingAsync_DoesNotFail_WhenAvailabilityCacheVersionBumpFails()
    {
        var room = CreateRoom(quantity: 3, pricePerNight: 120m);
        var repository = new FakeBookingRepository();
        var service = CreateService(
            repository,
            new FakeRoomRepository(room),
            cache: new FakeAppCache { ThrowOnSet = true });

        var result = await service.CreateBookingAsync("user-1", room.Id, new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 23));

        Assert.Equal(room.Id, result.RoomId);
        Assert.Single(repository.AddedBookings);
    }

    private static BookingService CreateService(
        FakeBookingRepository bookingRepository,
        FakeRoomRepository roomRepository,
        IClock? clock = null,
        IAppCache? cache = null)
    {
        var createBooking = new CreateBookingUseCase(
            bookingRepository,
            roomRepository,
            clock ?? new FakeClock(),
            cache ?? new FakeAppCache(),
            NullLogger<CreateBookingUseCase>.Instance);
        var getUserBookings = new GetUserBookingsUseCase(bookingRepository);

        return new BookingService(createBooking, getUserBookings);
    }

    private static Room CreateRoom(bool isActive = true, int quantity = 1, decimal pricePerNight = 100m)
    {
        return Room.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Standard",
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

        public Task AddAsync(Room room, CancellationToken ct)
            => Task.CompletedTask;

        public Task UpdateAsync(Room room, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class FakeBookingRepository : IBookingRepository
    {
        public Dictionary<Guid, int> OverlapsByRoom { get; init; } = new();
        public List<Booking> AddedBookings { get; } = new();

        public Task<Dictionary<Guid, int>> GetOverlappingActiveBookingsCountByRoomAsync(IEnumerable<Guid> roomIds, DateOnly checkIn, DateOnly checkOut, CancellationToken ct)
            => Task.FromResult(OverlapsByRoom);

        public Task<bool> TryAddIfAvailableAsync(Booking booking, int roomQuantity, CancellationToken ct)
        {
            if (OverlapsByRoom.GetValueOrDefault(booking.RoomId, 0) >= roomQuantity)
            {
                return Task.FromResult(false);
            }

            AddedBookings.Add(booking);
            return Task.FromResult(true);
        }

        public Task<List<Booking>> GetByUserAsync(string userId, CancellationToken ct)
            => Task.FromResult(new List<Booking>());
    }

    private sealed class FakeClock : IClock
    {
        public DateOnly Today { get; init; } = new(2026, 4, 19);
        public DateTimeOffset UtcNow { get; init; } = new(2026, 4, 19, 9, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeAppCache : IAppCache
    {
        public bool ThrowOnSet { get; init; }
        public List<(string Key, TimeSpan Ttl)> SetCalls { get; } = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
            => Task.FromResult<T?>(default);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
        {
            if (ThrowOnSet)
            {
                throw new InvalidOperationException("Cache unavailable.");
            }

            SetCalls.Add((key, ttl));
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
