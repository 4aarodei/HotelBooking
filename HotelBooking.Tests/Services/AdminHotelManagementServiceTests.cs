using HotelBooking.Application.Hotels;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Media;
using HotelBooking.Application.Services;
using HotelBooking.Domain.Entities.Hotels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HotelBooking.Tests.Services;

public class AdminHotelManagementServiceTests
{
    [Fact]
    public async Task CreateHotelAsync_DeletesUploadedImages_WhenRepositorySaveFails()
    {
        var hotelRepository = new FakeHotelRepository { ThrowOnAdd = true };
        var imageStorage = new FakeImageStorage();
        var service = CreateService(hotelRepository, new FakeRoomRepository(), imageStorage);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateHotelAsync(
                new CreateHotelCommand("River", "Kyiv", "Street 1", null, [CreateImageFile("one.png")]),
                CancellationToken.None));

        Assert.Single(imageStorage.SavedHotelImages);
        Assert.Single(imageStorage.DeletedImages);
        Assert.Equal(imageStorage.SavedHotelImages[0].StorageKey, imageStorage.DeletedImages[0].StorageKey);
    }

    [Fact]
    public async Task UpdateRoomAsync_DeletesRemovedImages_AfterSuccessfulSave()
    {
        var room = new Room
        {
            Id = Guid.NewGuid(),
            HotelId = Guid.NewGuid(),
            Name = "Standard",
            Capacity = 2,
            PricePerNight = 100m,
            Quantity = 2,
            IsActive = true,
            Images =
            [
                new RoomImage
                {
                    Id = Guid.NewGuid(),
                    StorageKey = "rooms/existing.webp",
                    Url = "https://cdn.example.com/rooms/existing.webp",
                    ContentType = "image/webp",
                    SizeBytes = 123,
                    Width = 800,
                    Height = 600,
                    AltText = "Standard",
                    IsCover = true,
                    SortOrder = 0
                }
            ]
        };

        var imageStorage = new FakeImageStorage();
        var roomRepository = new FakeRoomRepository { Room = room };
        var service = CreateService(new FakeHotelRepository(), roomRepository, imageStorage);

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
        var service = CreateService(
            new FakeHotelRepository { Hotel = new Hotel { Id = Guid.NewGuid(), Name = "River", City = "Kyiv", Address = "Street 1" } },
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
                    [CreateImageFile("one.png"), CreateImageFile("two.png")]),
                CancellationToken.None));
    }

    private static AdminHotelManagementService CreateService(
        FakeHotelRepository hotelRepository,
        FakeRoomRepository roomRepository,
        FakeImageStorage imageStorage,
        int maxFilesPerUpload = 8)
    {
        hotelRepository.Hotel ??= new Hotel
        {
            Id = Guid.NewGuid(),
            Name = "River",
            City = "Kyiv",
            Address = "Street 1"
        };

        return new AdminHotelManagementService(
            hotelRepository,
            roomRepository,
            new FakeImageProcessor(),
            imageStorage,
            Options.Create(new ImageUploadOptions { MaxFilesPerUpload = maxFilesPerUpload }),
            NullLogger<AdminHotelManagementService>.Instance);
    }

    private static ImageUploadFile CreateImageFile(string fileName)
    {
        return new ImageUploadFile(fileName, "image/png", 128, () => new MemoryStream([1, 2, 3]));
    }

    private sealed class FakeHotelRepository : IHotelRepository
    {
        public Hotel? Hotel { get; set; }
        public bool ThrowOnAdd { get; set; }

        public Task<List<Hotel>> GetWithActiveRoomsAsync(string? city, CancellationToken ct) => Task.FromResult(new List<Hotel>());
        public Task<Hotel?> GetWithRoomsByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Hotel);
        public Task<List<string>> GetDistinctCitiesAsync(CancellationToken ct) => Task.FromResult(new List<string>());
        public Task<List<Hotel>> GetFeaturedAsync(int count, CancellationToken ct) => Task.FromResult(new List<Hotel>());
        public Task<List<Hotel>> GetAllAsync(CancellationToken ct) => Task.FromResult(new List<Hotel>());
        public Task<Hotel?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Hotel?.Id == id ? Hotel : null);
        public Task<Hotel?> GetByIdWithImagesAsync(Guid id, CancellationToken ct) => Task.FromResult(Hotel?.Id == id ? Hotel : null);

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
    }

    private sealed class FakeRoomRepository : IRoomRepository
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

    private sealed class FakeImageProcessor : IImageProcessor
    {
        public Task<ProcessedImage> ProcessAsync(ImageUploadFile file, CancellationToken ct)
        {
            return Task.FromResult(new ProcessedImage(new MemoryStream([1, 2, 3]), file.FileName, "image/webp", ".webp", 3, 640, 480));
        }
    }

    private sealed class FakeImageStorage : IImageStorage
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
}
