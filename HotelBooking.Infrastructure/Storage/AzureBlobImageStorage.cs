using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HotelBooking.Application.Media;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Storage;

public sealed class AzureBlobImageStorage : IImageStorage
{
    private readonly AzureBlobImageStorageOptions _options;
    private readonly BlobContainerClient _container;

    public AzureBlobImageStorage(IOptions<AzureBlobImageStorageOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.ContainerName))
        {
            throw new InvalidOperationException("Azure Blob Storage container name is not configured.");
        }

        _container = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);
    }

    public Task<StoredImage> SaveHotelImageAsync(Guid hotelId, ProcessedImage image, CancellationToken ct)
    {
        return SaveAsync($"hotels/{hotelId:N}", image, ct);
    }

    public Task<StoredImage> SaveRoomImageAsync(Guid roomId, ProcessedImage image, CancellationToken ct)
    {
        return SaveAsync($"rooms/{roomId:N}", image, ct);
    }

    public async Task DeleteAsync(string storageKey, string publicUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return;
        }

        await _container.GetBlobClient(storageKey).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
    }

    private async Task<StoredImage> SaveAsync(string prefix, ProcessedImage image, CancellationToken ct)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var blobName = $"{prefix}/{Guid.NewGuid():N}{image.FileExtension}";
        var blob = _container.GetBlobClient(blobName);

        image.Content.Position = 0;
        await blob.UploadAsync(image.Content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = image.ContentType,
                CacheControl = "public, max-age=31536000, immutable"
            }
        }, ct);

        var publicUrl = BuildPublicUrl(blobName, blob.Uri);

        return new StoredImage(
            blobName,
            publicUrl,
            image.ContentType,
            image.SizeBytes,
            image.Width,
            image.Height);
    }

    private string BuildPublicUrl(string blobName, Uri blobUri)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return blobUri.ToString();
        }

        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{blobName}";
    }
}
