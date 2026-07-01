using HotelBooking.Application.Admin;
using HotelBooking.Application.Media;
using HotelBooking.ViewModels.Admin;
using HotelBooking.Web.Areas.Admin.Controllers;
using HotelBooking.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Xunit;

namespace HotelBooking.Tests.Controllers;

public class AdminHotelsControllerTests
{
    [Fact]
    public async Task EditGet_ReturnsNotFound_WhenHotelDoesNotExist()
    {
        var controller = CreateHotelsController(new FakeAdminHotelQueryService());

        var result = await controller.Edit(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditPost_ReturnsBadRequest_WhenHotelIdIsMissing()
    {
        var controller = CreateHotelsController(new FakeAdminHotelQueryService());

        var result = await controller.Edit(new AdminHotelFormViewModel(), CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task EditPost_RedisplaysQueryData_WhenModelStateIsInvalid()
    {
        var hotelId = Guid.NewGuid();
        var queryService = new FakeAdminHotelQueryService
        {
            HotelEditDetails = CreateHotelEditDetails(hotelId)
        };
        var controller = CreateHotelsController(queryService);
        controller.ModelState.AddModelError(nameof(AdminHotelFormViewModel.Name), "Name is required.");

        var result = await controller.Edit(
            new AdminHotelFormViewModel
            {
                Id = hotelId,
                Name = "Edited name",
                City = "Edited city",
                Address = "Edited address",
                Description = "Edited description"
            },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AdminHotelFormViewModel>(view.Model);
        Assert.Equal("Edited name", model.Name);
        Assert.Equal("Edited city", model.City);
        Assert.Single(model.ExistingImages);
        Assert.Single(model.Rooms);
    }

    [Fact]
    public async Task RoomCreateGet_UsesRoomsController()
    {
        var hotelId = Guid.NewGuid();
        var controller = CreateRoomsController(new FakeAdminHotelQueryService
        {
            CreateRoomDetails = CreateRoomDetails(hotelId)
        });

        var result = await controller.Create(hotelId, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AdminRoomFormViewModel>(view.Model);
        Assert.Equal(hotelId, model.HotelId);
        Assert.False(string.IsNullOrWhiteSpace(model.DraftUploadId));
    }

    [Fact]
    public async Task UploadRoomDraftPhotos_ReturnsNotFound_WhenHotelDoesNotExist()
    {
        var controller = CreateDraftController(new FakeAdminHotelQueryService { HotelExists = false });

        var result = await controller.Upload(
            Guid.NewGuid(),
            Guid.NewGuid().ToString("N"),
            [],
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UploadRoomDraftPhotos_ReturnsBadRequest_WhenDraftIdIsInvalid()
    {
        var controller = CreateDraftController(new FakeAdminHotelQueryService { HotelExists = true });

        var result = await controller.Upload(
            Guid.NewGuid(),
            "not-a-guid",
            [],
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid draft id.", badRequest.Value);
    }

    private static HotelsController CreateHotelsController(FakeAdminHotelQueryService queryService)
    {
        var commands = new FakeAdminCommandService();
        var controller = new HotelsController(commands, commands, queryService, queryService);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static RoomsController CreateRoomsController(FakeAdminHotelQueryService queryService)
    {
        var commands = new FakeAdminCommandService();
        var controller = new RoomsController(commands, commands, queryService, queryService, CreateDraftUploadService());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static RoomDraftPhotosController CreateDraftController(FakeAdminHotelQueryService queryService)
    {
        var controller = new RoomDraftPhotosController(queryService, CreateDraftUploadService());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static RoomDraftImageUploadService CreateDraftUploadService()
    {
        var webRoot = Path.Combine(Path.GetTempPath(), "hotelbooking-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(webRoot);

        return new RoomDraftImageUploadService(
            new FakeWebHostEnvironment(webRoot),
            Options.Create(new LocalImageStorageOptions()),
            new FakeImageProcessor());
    }

    private static AdminHotelEditDetails CreateHotelEditDetails(Guid hotelId)
    {
        return new AdminHotelEditDetails(
            hotelId,
            "River",
            "Kyiv",
            "Street 1",
            "Near the river",
            [
                new AdminImageItem(
                    Guid.NewGuid(),
                    "/uploads/hotel.webp",
                    "River",
                    640,
                    480,
                    true)
            ],
            [
                new AdminRoomListItem(
                    Guid.NewGuid(),
                    "Standard",
                    2,
                    1,
                    100m,
                    true,
                    "/uploads/room.webp",
                    800,
                    600)
            ]);
    }

    private static AdminRoomFormDetails CreateRoomDetails(Guid hotelId)
    {
        return new AdminRoomFormDetails(
            null,
            hotelId,
            "River",
            string.Empty,
            null,
            null,
            0,
            1000m,
            1,
            false,
            true,
            false,
            false,
            false,
            false,
            true,
            []);
    }

    private sealed class FakeAdminHotelQueryService :
        IGetAdminHotelListQuery,
        IGetAdminHotelEditDetailsQuery,
        IGetCreateRoomDetailsQuery,
        IGetEditRoomDetailsQuery,
        IAdminHotelExistsQuery
    {
        public AdminHotelEditDetails? HotelEditDetails { get; init; }
        public AdminRoomFormDetails? CreateRoomDetails { get; init; }
        public AdminRoomFormDetails? EditRoomDetails { get; init; }
        public bool HotelExists { get; init; }

        public Task<IReadOnlyList<AdminHotelListItem>> ExecuteAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AdminHotelListItem>>([]);

        public Task<AdminHotelEditDetails?> ExecuteForHotelAsync(Guid hotelId, CancellationToken ct = default)
            => Task.FromResult(HotelEditDetails?.Id == hotelId ? HotelEditDetails : null);

        Task<AdminRoomFormDetails?> IGetCreateRoomDetailsQuery.ExecuteForHotelAsync(Guid hotelId, CancellationToken ct)
            => Task.FromResult(CreateRoomDetails?.HotelId == hotelId ? CreateRoomDetails : null);

        public Task<AdminRoomFormDetails?> ExecuteForRoomAsync(Guid roomId, CancellationToken ct = default)
            => Task.FromResult(EditRoomDetails?.Id == roomId ? EditRoomDetails : null);

        Task<bool> IAdminHotelExistsQuery.ExecuteForHotelAsync(Guid hotelId, CancellationToken ct)
            => Task.FromResult(HotelExists);
    }

    private sealed class FakeAdminCommandService :
        ICreateHotelUseCase,
        IUpdateHotelUseCase,
        ICreateRoomUseCase,
        IUpdateRoomUseCase
    {
        public Task<Guid> ExecuteAsync(CreateHotelCommand command, CancellationToken ct = default)
            => Task.FromResult(Guid.NewGuid());

        public Task ExecuteAsync(UpdateHotelCommand command, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<Guid> ExecuteAsync(CreateRoomCommand command, CancellationToken ct = default)
            => Task.FromResult(Guid.NewGuid());

        public Task ExecuteAsync(UpdateRoomCommand command, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeImageProcessor : IImageProcessor
    {
        public Task<ProcessedImage> ProcessAsync(ImageUploadFile file, CancellationToken ct)
        {
            return Task.FromResult(new ProcessedImage(
                new MemoryStream([1, 2, 3]),
                file.FileName,
                "image/webp",
                ".webp",
                3,
                1,
                1));
        }
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment(string webRootPath)
        {
            WebRootPath = webRootPath;
            WebRootFileProvider = new PhysicalFileProvider(webRootPath);
        }

        public string ApplicationName { get; set; } = "HotelBooking.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public string EnvironmentName { get; set; } = "Development";
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
    }
}
