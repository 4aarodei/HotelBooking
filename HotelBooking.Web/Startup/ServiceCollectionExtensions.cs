using HotelBooking.Application;
using HotelBooking.Application.Media;
using HotelBooking.Infrastructure;
using HotelBooking.Infrastructure.Data;
using HotelBooking.Infrastructure.Storage;
using HotelBooking.Web.Health;
using HotelBooking.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HotelBooking.Web.Startup;

internal static class ServiceCollectionExtensions
{
    // Database and SQL retry policy.
    public static void AddHotelBookingDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sqlServer =>
            {
                sqlServer.EnableRetryOnFailure(
                    maxRetryCount: configuration.GetValue<int?>("Sql:MaxRetryCount") ?? 5,
                    maxRetryDelay: TimeSpan.FromSeconds(configuration.GetValue<int?>("Sql:MaxRetryDelaySeconds") ?? 10),
                    errorNumbersToAdd: null);
            }));
    }

    // Persist DataProtection keys for stable auth cookies.
    public static void AddHotelBookingDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var dataProtection = services.AddDataProtection()
            .SetApplicationName(configuration["DataProtection:ApplicationName"] ?? "HotelBooking");

        var keyPersistencePath = configuration["DataProtection:PersistKeysToFileSystemPath"];
        if (string.IsNullOrWhiteSpace(keyPersistencePath))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "DataProtection:PersistKeysToFileSystemPath is required outside Development.");
            }

            return;
        }

        Directory.CreateDirectory(keyPersistencePath);
        dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyPersistencePath));
    }

    // Trust reverse-proxy headers in container/ingress environments.
    public static void AddHotelBookingForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    // Bind options classes from appsettings/env vars.
    public static void AddHotelBookingOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpEmailOptions>(configuration.GetSection("Email:Smtp"));
        services.Configure<ImageUploadOptions>(configuration.GetSection("ImageStorage"));
        services.Configure<LocalImageStorageOptions>(configuration.GetSection("ImageStorage"));
        services.Configure<AzureBlobImageStorageOptions>(configuration.GetSection("ImageStorage:AzureBlob"));
        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.FromMinutes(5);
        });
    }

    // Use SMTP sender when configured, otherwise fallback to logger sender.
    public static void AddHotelBookingEmailSender(this IServiceCollection services, bool hasSmtpEmail)
    {
        services.AddTransient<IEmailSender, SmtpEmailSender>();

        if (!hasSmtpEmail)
        {
            services.AddTransient<IEmailSender, LoggingEmailSender>();
        }
    }

    // Pick image storage provider by config.
    public static void AddHotelBookingImageStorage(this IServiceCollection services, string imageStorageProvider)
    {
        services.AddScoped<RoomDraftImageUploadService>();

        if (imageStorageProvider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IImageStorage, AzureBlobImageStorage>();
            return;
        }

        services.AddScoped<IImageStorage, LocalImageStorageService>();
    }

    // Register Application and Infrastructure layers.
    public static void AddHotelBookingApplicationServices(this IServiceCollection services)
    {
        services.AddInfrastructure();
        services.AddApplication();
    }

    // MVC plus live/ready health checks.
    public static void AddHotelBookingMvcAndHealthChecks(this IServiceCollection services)
    {
        services.AddControllersWithViews();
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<DatabaseReadinessHealthCheck>("sql", tags: ["ready"]);
    }
}
