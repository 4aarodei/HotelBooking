using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace HotelBooking.Application.Media;

public sealed class ImageProcessor : IImageProcessor
{
    private const string OutputContentType = "image/webp";
    private const string OutputExtension = ".webp";

    private readonly ImageUploadOptions _options;

    public ImageProcessor(IOptions<ImageUploadOptions> options)
    {
        _options = options.Value;
    }

    public async Task<ProcessedImage> ProcessAsync(ImageUploadFile file, CancellationToken ct)
    {
        ValidateEnvelope(file);
        await ValidateSignatureAsync(file, ct);

        try
        {
            await using var identifyStream = file.OpenReadStream();
            var imageInfo = await Image.IdentifyAsync(identifyStream, ct);
            if (imageInfo is null)
            {
                throw new ImageUploadValidationException($"{file.FileName} is not a readable image.");
            }

            var decodedFormat = imageInfo.Metadata.DecodedImageFormat;
            if (decodedFormat is null || !_options.AllowedContentTypes.Contains(decodedFormat.DefaultMimeType, StringComparer.OrdinalIgnoreCase))
            {
                throw new ImageUploadValidationException($"{file.FileName} must be a JPG, PNG, or WebP image.");
            }

            ValidateDimensions(file.FileName, imageInfo.Width, imageInfo.Height);

            await using var decodeStream = file.OpenReadStream();
            using var image = await Image.LoadAsync(decodeStream, ct);
            StripMetadata(image);

            var output = new MemoryStream();
            await image.SaveAsWebpAsync(output, new WebpEncoder
            {
                Quality = Math.Clamp(_options.ReencodeQuality, 1, 100)
            }, ct);

            output.Position = 0;

            return new ProcessedImage(
                output,
                file.FileName,
                OutputContentType,
                OutputExtension,
                output.Length,
                image.Width,
                image.Height);
        }
        catch (Exception ex) when (ex is InvalidImageContentException or UnknownImageFormatException)
        {
            throw new ImageUploadValidationException($"{file.FileName} is not a readable image.");
        }
    }

    private static async Task ValidateSignatureAsync(ImageUploadFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var buffer = new byte[12];
        var bytesRead = await stream.ReadAsync(buffer, ct);

        if (!HasAllowedSignature(buffer.AsSpan(0, bytesRead)))
        {
            throw new ImageUploadValidationException($"{file.FileName} does not match an allowed image signature.");
        }
    }

    private static bool HasAllowedSignature(ReadOnlySpan<byte> bytes)
    {
        return HasJpegSignature(bytes) || HasPngSignature(bytes) || HasWebpSignature(bytes);
    }

    private static bool HasJpegSignature(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 3 &&
               bytes[0] == 0xFF &&
               bytes[1] == 0xD8 &&
               bytes[2] == 0xFF;
    }

    private static bool HasPngSignature(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 8 &&
               bytes[0] == 0x89 &&
               bytes[1] == 0x50 &&
               bytes[2] == 0x4E &&
               bytes[3] == 0x47 &&
               bytes[4] == 0x0D &&
               bytes[5] == 0x0A &&
               bytes[6] == 0x1A &&
               bytes[7] == 0x0A;
    }

    private static bool HasWebpSignature(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 12 &&
               bytes[0] == 0x52 &&
               bytes[1] == 0x49 &&
               bytes[2] == 0x46 &&
               bytes[3] == 0x46 &&
               bytes[8] == 0x57 &&
               bytes[9] == 0x45 &&
               bytes[10] == 0x42 &&
               bytes[11] == 0x50;
    }

    private void ValidateEnvelope(ImageUploadFile file)
    {
        if (file.Length <= 0)
        {
            throw new ImageUploadValidationException($"{file.FileName} is empty.");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            var maxMb = _options.MaxFileSizeBytes / 1024 / 1024;
            throw new ImageUploadValidationException($"{file.FileName} is larger than {maxMb} MB.");
        }

        if (!_options.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ImageUploadValidationException($"{file.FileName} must be a JPG, PNG, or WebP image.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            !_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new ImageUploadValidationException($"{file.FileName} has an unsupported file extension.");
        }
    }

    private void ValidateDimensions(string fileName, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ImageUploadValidationException($"{fileName} has invalid image dimensions.");
        }

        if (width > _options.MaxWidth || height > _options.MaxHeight)
        {
            throw new ImageUploadValidationException($"{fileName} exceeds the maximum image dimensions.");
        }

        if ((long)width * height > _options.MaxPixelCount)
        {
            throw new ImageUploadValidationException($"{fileName} has too many pixels.");
        }
    }

    private static void StripMetadata(Image image)
    {
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.XmpProfile = null;
    }
}
