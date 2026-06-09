using Azure.Extensions.AspNetCore.Configuration.Secrets;
using HotelBooking.Application.Caching;

namespace HotelBooking.Web.Startup;

internal static class BuilderConfigurationExtensions
{
    // Add Key Vault provider only when URI is configured.
    public static void ConfigureKeyVaultIfEnabled(this WebApplicationBuilder builder)
    {
        var keyVaultUriRaw = builder.Configuration["Azure:KeyVault:Uri"];
        if (string.IsNullOrWhiteSpace(keyVaultUriRaw))
        {
            return;
        }

        if (!Uri.TryCreate(keyVaultUriRaw, UriKind.Absolute, out var keyVaultUri))
        {
            throw new InvalidOperationException("Azure:KeyVault:Uri must be a valid absolute URI.");
        }

        builder.Configuration.AddAzureKeyVault(keyVaultUri, new Azure.Identity.DefaultAzureCredential());
    }

    // Keep logs easy to read in local development.
    public static void ConfigureDevelopmentLogging(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
        {
            return;
        }

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
    }

    // Read values from config and create one runtime settings object.
    public static RuntimeSettings BuildRuntimeSettings(this WebApplicationBuilder builder)
    {
        var requireConfirmedAccount = builder.Configuration.GetValue<bool?>("Identity:RequireConfirmedAccount")
                                      ?? !builder.Environment.IsDevelopment();
        var smtpSection = builder.Configuration.GetSection("Email:Smtp");
        var hasSmtpEmail =
            !string.IsNullOrWhiteSpace(smtpSection["Host"]) &&
            !string.IsNullOrWhiteSpace(smtpSection["From"]);
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        var imageStorageProvider = builder.Configuration.GetValue<string>("ImageStorage:Provider") ?? "Local";

        return new RuntimeSettings(
            RequireConfirmedAccount: requireConfirmedAccount,
            HasSmtpEmail: hasSmtpEmail,
            ConnectionString: connectionString,
            ImageStorageProvider: imageStorageProvider);
    }

    // Validate settings before service registration.
    public static void ValidateRuntimeSettings(this WebApplicationBuilder builder, RuntimeSettings runtimeSettings)
    {
        if (!builder.Environment.IsDevelopment() &&
            runtimeSettings.RequireConfirmedAccount &&
            !runtimeSettings.HasSmtpEmail)
        {
            throw new InvalidOperationException("SMTP email settings are required when confirmed accounts are enabled outside Development.");
        }

        ValidateImageStorageConfiguration(builder.Environment, runtimeSettings.ImageStorageProvider, builder.Configuration);
        ValidateRedisConfiguration(builder.Configuration);
    }

    // In Stage/Prod we allow only Azure Blob and required Blob values.
    private static void ValidateImageStorageConfiguration(
        IHostEnvironment environment,
        string imageStorageProvider,
        IConfiguration configuration)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        if (!imageStorageProvider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Image storage provider '{imageStorageProvider}' is not allowed in {environment.EnvironmentName}. Use 'AzureBlob'.");
        }

        var connectionString = configuration["ImageStorage:AzureBlob:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ImageStorage:AzureBlob:ConnectionString is required outside Development.");
        }

        var containerName = configuration["ImageStorage:AzureBlob:ContainerName"];
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidOperationException(
                "ImageStorage:AzureBlob:ContainerName is required outside Development.");
        }

        var publicBaseUrl = configuration["ImageStorage:AzureBlob:PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(publicBaseUrl) ||
            !Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var parsedUri) ||
            string.IsNullOrWhiteSpace(parsedUri.Scheme) ||
            string.IsNullOrWhiteSpace(parsedUri.Host))
        {
            throw new InvalidOperationException(
                "ImageStorage:AzureBlob:PublicBaseUrl must be a valid absolute URL outside Development.");
        }
    }

    private static void ValidateRedisConfiguration(IConfiguration configuration)
    {
        var redis = configuration.GetSection("Redis").Get<RedisOptions>() ?? new RedisOptions();
        if (!redis.Enabled)
        {
            if (redis.RateLimiting.Enabled)
            {
                throw new InvalidOperationException("Redis:RateLimiting:Enabled requires Redis:Enabled=true.");
            }

            if (redis.RequiredForReadiness)
            {
                throw new InvalidOperationException("Redis:RequiredForReadiness requires Redis:Enabled=true.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(redis.ConnectionString))
        {
            throw new InvalidOperationException("Redis:ConnectionString is required when Redis:Enabled=true.");
        }

        if (string.IsNullOrWhiteSpace(redis.InstanceName))
        {
            throw new InvalidOperationException("Redis:InstanceName is required when Redis:Enabled=true.");
        }
    }
}
