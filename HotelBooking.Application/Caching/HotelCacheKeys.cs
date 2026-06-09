using System.Globalization;

namespace HotelBooking.Application.Caching;

public static class HotelCacheKeys
{
    public const string Cities = "cities:v1";
    public const string CatalogVersion = "hotel-read:catalog-version:v1";
    public const string AvailabilityVersion = "hotel-read:availability-version:v1";
    public const string DefaultCatalogVersion = "0";
    public const string DefaultAvailabilityVersion = "0";

    public static string FeaturedHotels(int count, string? catalogVersion)
    {
        return $"featured-hotels:{Math.Max(count, 0)}:catalog:{NormalizeCatalogVersion(catalogVersion)}:v1";
    }

    public static string HotelSearch(
        string? city,
        DateOnly checkIn,
        DateOnly checkOut,
        string? catalogVersion,
        string? availabilityVersion)
    {
        var citySegment = string.IsNullOrWhiteSpace(city)
            ? "all"
            : Uri.EscapeDataString(city.Trim().ToLowerInvariant());

        return string.Create(
            CultureInfo.InvariantCulture,
            $"hotel-search:{citySegment}:{checkIn:yyyy-MM-dd}:{checkOut:yyyy-MM-dd}:catalog:{NormalizeCatalogVersion(catalogVersion)}:availability:{NormalizeAvailabilityVersion(availabilityVersion)}:v1");
    }

    private static string NormalizeCatalogVersion(string? catalogVersion)
    {
        return string.IsNullOrWhiteSpace(catalogVersion)
            ? DefaultCatalogVersion
            : Uri.EscapeDataString(catalogVersion.Trim());
    }

    private static string NormalizeAvailabilityVersion(string? availabilityVersion)
    {
        return string.IsNullOrWhiteSpace(availabilityVersion)
            ? DefaultAvailabilityVersion
            : Uri.EscapeDataString(availabilityVersion.Trim());
    }
}
