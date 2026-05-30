namespace HotelBooking.Infrastructure.Storage;

public class AzureBlobImageStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "hotel-images";
    public string? PublicBaseUrl { get; set; }
}
