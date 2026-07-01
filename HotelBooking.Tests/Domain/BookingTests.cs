using HotelBooking.Domain.Entities.Bookings;
using HotelBooking.Domain.Entities.Hotels;
using HotelBooking.Domain.Exceptions;
using Xunit;

namespace HotelBooking.Tests.Domain;

public class BookingTests
{
    [Fact]
    public void DateRange_Create_Throws_WhenCheckOutIsNotAfterCheckIn()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            DateRange.Create(new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 20)));
    }

    [Fact]
    public void DateRange_Create_CalculatesNights()
    {
        var range = DateRange.Create(new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 23));

        Assert.Equal(new DateOnly(2026, 4, 20), range.CheckIn);
        Assert.Equal(new DateOnly(2026, 4, 23), range.CheckOut);
        Assert.Equal(3, range.Nights);
    }

    [Fact]
    public void Booking_Create_CalculatesSnapshotTotalsAndInitialStatus()
    {
        var room = CreateBookableRoom(quantity: 2, pricePerNight: 125m);
        var createdAtUtc = new DateTimeOffset(2026, 4, 19, 10, 30, 0, TimeSpan.Zero);
        var dateRange = DateRange.Create(new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 23));

        var booking = Booking.Create(
            "user-1",
            room,
            dateRange,
            createdAtUtc);

        Assert.Equal("user-1", booking.UserId);
        Assert.Equal(room.Id, booking.RoomId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(3, booking.Nights);
        Assert.Equal(125m, booking.PricePerNightSnapshot);
        Assert.Equal(375m, booking.TotalPrice);
        Assert.Equal(createdAtUtc, booking.CreatedAtUtc);
    }

    [Fact]
    public void Booking_Create_Throws_WhenUserIsMissing()
    {
        var room = CreateBookableRoom();
        var dateRange = DateRange.Create(new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 22));

        Assert.Throws<DomainRuleViolationException>(() =>
            Booking.Create(
                " ",
                room,
                dateRange,
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Booking_Create_Throws_WhenRoomCannotBeBooked()
    {
        var room = CreateBookableRoom(isActive: false);
        var dateRange = DateRange.Create(new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 22));

        Assert.Throws<DomainRuleViolationException>(() =>
            Booking.Create(
                "user-1",
                room,
                dateRange,
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Room_Create_Throws_WhenRoomPriceIsNotPositive()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            CreateBookableRoom(pricePerNight: 0m));
    }

    [Fact]
    public void Room_EnsureCanBeBooked_Throws_WhenInactive()
    {
        var inactiveRoom = CreateBookableRoom(isActive: false);

        Assert.Throws<DomainRuleViolationException>(() => inactiveRoom.EnsureCanBeBooked());
    }

    [Fact]
    public void Room_Create_Throws_WhenQuantityIsZero()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            CreateBookableRoom(quantity: 0));
    }

    [Fact]
    public void Room_Create_AppliesRoomFeatures()
    {
        var features = new RoomFeatures(
            IncludesBreakfast: true,
            HasPrivateBathroom: true,
            HasSaunaAccess: true,
            HasBalcony: true,
            HasWorkspace: true,
            HasAirConditioning: true);

        var room = Room.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Suite",
            null,
            null,
            2,
            150m,
            1,
            features,
            isActive: true);

        Assert.True(room.IncludesBreakfast);
        Assert.True(room.HasPrivateBathroom);
        Assert.True(room.HasSaunaAccess);
        Assert.True(room.HasBalcony);
        Assert.True(room.HasWorkspace);
        Assert.True(room.HasAirConditioning);
    }

    [Fact]
    public void Hotel_AddAndRemoveImages_NormalizesCoverAndSortOrder()
    {
        var hotel = Hotel.Create("River", "Kyiv", "Street 1");
        var first = HotelImage.Create("hotels/1.webp", "/1.webp", "image/webp", 1, 100, 100, "First");
        var second = HotelImage.Create("hotels/2.webp", "/2.webp", "image/webp", 1, 100, 100, "Second");

        hotel.AddImage(first);
        hotel.AddImage(second);
        var removed = hotel.RemoveImages([first.Id]);

        Assert.Single(removed);
        var remaining = Assert.Single(hotel.Images);
        Assert.Equal(second.Id, remaining.Id);
        Assert.True(remaining.IsCover);
        Assert.Equal(0, remaining.SortOrder);
    }

    [Fact]
    public void Room_AddAndRemoveImages_NormalizesCoverAndSortOrder()
    {
        var room = CreateBookableRoom();
        var first = RoomImage.Create("rooms/1.webp", "/1.webp", "image/webp", 1, 100, 100, "First");
        var second = RoomImage.Create("rooms/2.webp", "/2.webp", "image/webp", 1, 100, 100, "Second");

        room.AddImage(first);
        room.AddImage(second);
        var removed = room.RemoveImages([first.Id]);

        Assert.Single(removed);
        var remaining = Assert.Single(room.Images);
        Assert.Equal(second.Id, remaining.Id);
        Assert.True(remaining.IsCover);
        Assert.Equal(0, remaining.SortOrder);
    }

    private static Room CreateBookableRoom(
        bool isActive = true,
        int quantity = 1,
        decimal pricePerNight = 100m)
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
}
