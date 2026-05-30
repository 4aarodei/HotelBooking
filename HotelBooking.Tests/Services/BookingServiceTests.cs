using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Services;
using HotelBooking.Domain.Entities.Bookings;
using HotelBooking.Domain.Entities.Hotels;
using Xunit;

namespace HotelBooking.Tests.Services;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateBookingAsync_Throws_WhenCheckOutNotAfterCheckIn()
    {
        var service = new BookingService(new FakeBookingRepository(), new FakeRoomRepository(new Room { Id = Guid.NewGuid(), Name = "Standard", IsActive = true, Quantity = 2 }), new FakeClock());

        await Assert.ThrowsAsync<BookingRuleViolationException>(() =>
            service.CreateBookingAsync("user-1", Guid.NewGuid(), new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 20)));
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenRoomInactive()
    {
        var room = new Room { Id = Guid.NewGuid(), Name = "Standard", IsActive = false, Quantity = 2 };
        var service = new BookingService(new FakeBookingRepository(), new FakeRoomRepository(room), new FakeClock());

        await Assert.ThrowsAsync<BookingRuleViolationException>(() =>
            service.CreateBookingAsync("user-1", room.Id, new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 22)));
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenRoomNotFound()
    {
        var room = new Room { Id = Guid.NewGuid(), Name = "Standard", IsActive = true, Quantity = 2 };
        var service = new BookingService(new FakeBookingRepository(), new FakeRoomRepository(room), new FakeClock());

        await Assert.ThrowsAsync<BookingRuleViolationException>(() =>
            service.CreateBookingAsync("user-1", Guid.NewGuid(), new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 22)));
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenNoAvailability()
    {
        var room = new Room { Id = Guid.NewGuid(), Name = "Standard", IsActive = true, Quantity = 1, PricePerNight = 100 };
        var repository = new FakeBookingRepository
        {
            OverlapsByRoom = new Dictionary<Guid, int> { [room.Id] = 1 }
        };
        var service = new BookingService(repository, new FakeRoomRepository(room), new FakeClock());

        await Assert.ThrowsAsync<BookingRuleViolationException>(() =>
            service.CreateBookingAsync("user-1", room.Id, new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 22)));
    }

    [Fact]
    public async Task CreateBookingAsync_CreatesPendingBookingWithCalculatedTotals()
    {
        var room = new Room { Id = Guid.NewGuid(), Name = "Standard", IsActive = true, Quantity = 3, PricePerNight = 120m };
        var repository = new FakeBookingRepository();
        var clock = new FakeClock { UtcNow = new DateTimeOffset(2026, 4, 19, 10, 30, 0, TimeSpan.Zero) };
        var service = new BookingService(repository, new FakeRoomRepository(room), clock);

        var result = await service.CreateBookingAsync("user-1", room.Id, new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 23));

        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Equal(3, result.Nights);
        Assert.Equal(360m, result.TotalPrice);
        Assert.Equal(clock.UtcNow, result.CreatedAtUtc);
        Assert.Single(repository.AddedBookings);
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
}
