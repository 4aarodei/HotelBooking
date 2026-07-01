using HotelBooking.Application.Admin;
using HotelBooking.Domain.Entities.Hotels;
using Xunit;

namespace HotelBooking.Tests.Services;

public class AdminHotelQueryServiceTests
{
    [Fact]
    public async Task GetHotelListAsync_ReturnsCoverImageFieldsNeededByWeb()
    {
        var hotel = TestServices.CreateHotel();
        hotel.Images.Add(new HotelImage
        {
            Id = Guid.NewGuid(),
            HotelId = hotel.Id,
            StorageKey = "hotels/secondary.webp",
            Url = "/uploads/secondary.webp",
            ContentType = "image/webp",
            AltText = "Secondary",
            Width = 320,
            Height = 240,
            IsCover = false,
            SortOrder = 0
        });
        hotel.Images.Add(new HotelImage
        {
            Id = Guid.NewGuid(),
            HotelId = hotel.Id,
            StorageKey = "hotels/cover.webp",
            Url = "/uploads/cover.webp",
            ContentType = "image/webp",
            AltText = "Cover",
            Width = 640,
            Height = 480,
            IsCover = true,
            SortOrder = 1
        });
        var service = new AdminHotelQueryService(
            new FakeHotelRepository { Hotels = [hotel] },
            new FakeRoomRepository());

        var result = await service.GetHotelListAsync(CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(hotel.Id, item.Id);
        Assert.Equal("/uploads/cover.webp", item.CoverImageUrl);
        Assert.Equal(640, item.CoverImageWidth);
        Assert.Equal(480, item.CoverImageHeight);
    }

    [Fact]
    public async Task GetHotelEditDetailsAsync_ReturnsImagesAndRoomSummaries()
    {
        var roomId = Guid.NewGuid();
        var hotel = Hotel.Create(Guid.NewGuid(), "River", "Kyiv", "Street 1", "Near the river");
        hotel.Images.Add(new HotelImage
        {
            Id = Guid.NewGuid(),
            HotelId = hotel.Id,
            StorageKey = "hotels/hotel.webp",
            Url = "/uploads/hotel.webp",
            ContentType = "image/webp",
            AltText = null,
            Width = 640,
            Height = 480,
            IsCover = true,
            SortOrder = 0
        });
        var room = TestServices.CreateRoom(roomId, hotel.Id);
        room.Images.Add(new RoomImage
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            StorageKey = "rooms/room.webp",
            Url = "/uploads/room.webp",
            ContentType = "image/webp",
            AltText = null,
            Width = 800,
            Height = 600,
            IsCover = true,
            SortOrder = 0
        });
        hotel.Rooms.Add(room);
        var service = new AdminHotelQueryService(
            new FakeHotelRepository { Hotel = hotel },
            new FakeRoomRepository());

        var result = await service.GetHotelEditDetailsAsync(hotel.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Near the river", result.Description);
        var image = Assert.Single(result.Images);
        Assert.Equal("River", image.AltText);
        var roomSummary = Assert.Single(result.Rooms);
        Assert.Equal(roomId, roomSummary.Id);
        Assert.Equal("/uploads/room.webp", roomSummary.CoverImageUrl);
    }

    [Fact]
    public async Task GetCreateRoomDetailsAsync_ReturnsDefaultsForExistingHotel()
    {
        var hotel = TestServices.CreateHotel();
        var service = new AdminHotelQueryService(
            new FakeHotelRepository { Hotel = hotel },
            new FakeRoomRepository());

        var result = await service.GetCreateRoomDetailsAsync(hotel.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.Id);
        Assert.Equal(hotel.Id, result.HotelId);
        Assert.Equal(hotel.Name, result.HotelName);
        Assert.Equal(1000m, result.PricePerNight);
        Assert.Equal(1, result.Quantity);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetEditRoomDetailsAsync_ReturnsRoomAndHotelName()
    {
        var hotel = TestServices.CreateHotel();
        var room = TestServices.CreateRoomWithImage(hotelId: hotel.Id);
        var service = new AdminHotelQueryService(
            new FakeHotelRepository { Hotel = hotel },
            new FakeRoomRepository { Room = room });

        var result = await service.GetEditRoomDetailsAsync(room.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(room.Id, result.Id);
        Assert.Equal(hotel.Name, result.HotelName);
        var image = Assert.Single(result.Images);
        Assert.Equal("Standard", image.AltText);
    }
}
