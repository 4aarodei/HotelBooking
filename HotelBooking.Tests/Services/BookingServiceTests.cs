using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Services;
using HotelBooking.Domain.Entities.Bookings;
using HotelBooking.Domain.Entities.Hotels;

namespace HotelBooking.Tests.Services;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateBookingAsync_Throws_WhenCheckOutNotAfterCheckIn()
    {
        var service = new BookingService(new FakeBookingRepository(), new FakeRoomRepository(new Room { Id = Guid.NewGuid(), Name = "Standard", IsActive = true, Quantity = 2 }));

        await Assert.ThrowsAsync<BookingRuleViolationException>(() =>
            service.CreateBookingAsync("user-1", Guid.NewGuid(), new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 20)));
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenRoomInactive()
    {
        var room = new Room { Id = Guid.NewGuid(), Name = "Standard", IsActive = false, Quantity = 2 };
        var service = new BookingService(new FakeBookingRepository(), new FakeRoomRepository(room));

        await Assert.ThrowsAsync<BookingRuleViolationException>(() =>
            service.CreateBookingAsync("user-1", room.Id, new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 22)));
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenNoAvailability()
    {
        var room = new Room { Id = Guid.NewGuid(), Name = "Standard", IsActive = true, Quantity = 1, PricePerNight = 100 };
        var repository = new FakeBookingRepository
        {
            OverlapsByRoom = new Dictionary<Guid, int> { [room.Id] = 1 }
        };
        var service = new BookingService(repository, new FakeRoomRepository(room));

        await Assert.ThrowsAsync<BookingRuleViolationException>(() =>
            service.CreateBookingAsync("user-1", room.Id, new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 22)));
    }

    [Fact]
    public async Task CreateBookingAsync_CreatesPendingBookingWithCalculatedTotals()
    {
        var room = new Room { Id = Guid.NewGuid(), Name = "Standard", IsActive = true, Quantity = 3, PricePerNight = 120m };
        var repository = new FakeBookingRepository();
        var service = new BookingService(repository, new FakeRoomRepository(room));

        var result = await service.CreateBookingAsync("user-1", room.Id, new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 23));

        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Equal(3, result.Nights);
        Assert.Equal(360m, result.TotalPrice);
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
    }

    private sealed class FakeBookingRepository : IBookingRepository
    {
        public Dictionary<Guid, int> OverlapsByRoom { get; init; } = new();
        public List<Booking> AddedBookings { get; } = new();

        public Task<Dictionary<Guid, int>> GetOverlappingActiveBookingsCountByRoomAsync(IEnumerable<Guid> roomIds, DateOnly checkIn, DateOnly checkOut, CancellationToken ct)
            => Task.FromResult(OverlapsByRoom);

        public Task AddAsync(Booking booking, CancellationToken ct)
        {
            AddedBookings.Add(booking);
            return Task.CompletedTask;
        }

        public Task<List<Booking>> GetByUserAsync(string userId, CancellationToken ct)
            => Task.FromResult(new List<Booking>());
    }
}
