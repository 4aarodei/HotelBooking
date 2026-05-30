using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Services;
using HotelBooking.Domain.Entities.Bookings;
using HotelBooking.Domain.Entities.Hotels;
using Xunit;

namespace HotelBooking.Tests.Services;

public class HotelServiceTests
{
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
        var service = new HotelService(new FakeHotelRepository(), new FakeRoomRepository(room), bookingRepository);

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
        var service = new HotelService(new FakeHotelRepository(), new FakeRoomRepository(room), new FakeBookingRepository());

        var result = await service.GetRoomByIdWithAvailabilityAsync(room.Id, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 2));

        Assert.Null(result);
    }

    private sealed class FakeHotelRepository : IHotelRepository
    {
        public Task<List<Hotel>> GetWithActiveRoomsAsync(string? city, CancellationToken ct) => Task.FromResult(new List<Hotel>());
        public Task<Hotel?> GetWithRoomsByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Hotel?>(null);
        public Task<List<string>> GetDistinctCitiesAsync(CancellationToken ct) => Task.FromResult(new List<string>());
        public Task<List<Hotel>> GetFeaturedAsync(int count, CancellationToken ct) => Task.FromResult(new List<Hotel>());
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
}
