using HotelBooking.Application.Media;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace HotelBooking.Tests.Media;

public class ImageProcessorTests
{
    [Fact]
    public async Task ProcessAsync_ReencodesValidImageToWebp()
    {
        var processor = CreateProcessor();
        var file = CreateFile("room.png", "image/png", CreateValidPng());

        using var result = await processor.ProcessAsync(file, CancellationToken.None);

        Assert.Equal("image/webp", result.ContentType);
        Assert.Equal(".webp", result.FileExtension);
        Assert.Equal(1, result.Width);
        Assert.Equal(1, result.Height);
        Assert.True(result.SizeBytes > 0);
    }

    [Fact]
    public async Task ProcessAsync_RejectsInvalidImageSignature()
    {
        var processor = CreateProcessor();
        var file = CreateFile("room.png", "image/png", "not an image"u8.ToArray());

        var exception = await Assert.ThrowsAsync<ImageUploadValidationException>(
            () => processor.ProcessAsync(file, CancellationToken.None));

        Assert.Contains("signature", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_RejectsCorruptImageContent()
    {
        var processor = CreateProcessor();
        var corruptPng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");
        var file = CreateFile("room.png", "image/png", corruptPng);

        var exception = await Assert.ThrowsAsync<ImageUploadValidationException>(
            () => processor.ProcessAsync(file, CancellationToken.None));

        Assert.Contains("readable", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_RejectsFilesOverConfiguredSize()
    {
        var processor = CreateProcessor(maxFileSizeBytes: 4);
        var file = CreateFile("room.png", "image/png", CreateValidPng());

        var exception = await Assert.ThrowsAsync<ImageUploadValidationException>(
            () => processor.ProcessAsync(file, CancellationToken.None));

        Assert.Contains("larger", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ImageProcessor CreateProcessor(long maxFileSizeBytes = 1024 * 1024)
    {
        return new ImageProcessor(Options.Create(new ImageUploadOptions
        {
            MaxFileSizeBytes = maxFileSizeBytes,
            MaxWidth = 32,
            MaxHeight = 32,
            MaxPixelCount = 1024
        }));
    }

    private static ImageUploadFile CreateFile(string fileName, string contentType, byte[] bytes)
    {
        return new ImageUploadFile(
            fileName,
            contentType,
            bytes.Length,
            () => new MemoryStream(bytes));
    }

    private static byte[] CreateValidPng()
    {
        using var image = new Image<Rgba32>(1, 1, new Rgba32(255, 255, 255, 255));
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }
}
