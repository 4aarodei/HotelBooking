using System.Net;
using HotelBooking.Application;
using HotelBooking.Application.Caching;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Media;
using HotelBooking.Infrastructure;
using HotelBooking.Infrastructure.Caching;
using HotelBooking.Infrastructure.Data;
using HotelBooking.Infrastructure.RateLimiting;
using HotelBooking.Infrastructure.Storage;
using HotelBooking.Web.Health;
using HotelBooking.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using ForwardedHeadersIpNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

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
    public static void AddHotelBookingForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;

            foreach (var proxy in configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
            {
                if (!IPAddress.TryParse(proxy, out var address))
                {
                    throw new InvalidOperationException($"ReverseProxy:KnownProxies contains invalid IP address '{proxy}'.");
                }

                options.KnownProxies.Add(address);
            }

            foreach (var network in configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [])
            {
                options.KnownNetworks.Add(ParseCidr(network));
            }
        });
    }

    private static ForwardedHeadersIpNetwork ParseCidr(string value)
    {
        var parts = value.Split('/', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 ||
            !IPAddress.TryParse(parts[0], out var prefix) ||
            !int.TryParse(parts[1], out var prefixLength))
        {
            throw new InvalidOperationException($"ReverseProxy:KnownNetworks contains invalid CIDR '{value}'.");
        }

        try
        {
            return new ForwardedHeadersIpNetwork(prefix, prefixLength);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new InvalidOperationException($"ReverseProxy:KnownNetworks contains invalid CIDR '{value}'.", ex);
        }
    }

    // Bind options classes from appsettings/env vars.
    public static void AddHotelBookingOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpEmailOptions>(configuration.GetSection("Email:Smtp"));
        services.Configure<ImageUploadOptions>(configuration.GetSection("ImageStorage"));
        services.Configure<LocalImageStorageOptions>(configuration.GetSection("ImageStorage"));
        services.Configure<AzureBlobImageStorageOptions>(configuration.GetSection("ImageStorage:AzureBlob"));
        services.Configure<RedisOptions>(configuration.GetSection("Redis"));
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

    // Redis-backed cache and distributed rate limiter. Disabled config uses fail-open no-op services.
    public static void AddHotelBookingRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redis = configuration.GetSection("Redis").Get<RedisOptions>() ?? new RedisOptions();
        if (!redis.Enabled)
        {
            services.AddSingleton<IAppCache, NoOpAppCache>();
            services.AddSingleton<IFixedWindowRateLimiter, DisabledFixedWindowRateLimiter>();
            return;
        }

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redis.ConnectionString;
            options.InstanceName = redis.InstanceName;
        });

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redis.ConnectionString);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<RedisFixedWindowRateLimiter>();
        services.AddSingleton<DisabledFixedWindowRateLimiter>();
        services.AddSingleton<IAppCache, DistributedAppCache>();
        services.AddSingleton<IFixedWindowRateLimiter>(serviceProvider =>
            redis.RateLimiting.Enabled
                ? serviceProvider.GetRequiredService<RedisFixedWindowRateLimiter>()
                : serviceProvider.GetRequiredService<DisabledFixedWindowRateLimiter>());
    }

    // MVC plus live/ready health checks.
    public static void AddHotelBookingMvcAndHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var redis = configuration.GetSection("Redis").Get<RedisOptions>() ?? new RedisOptions();

        services.AddControllersWithViews();
        var healthChecks = services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<DatabaseReadinessHealthCheck>("sql", tags: ["ready"]);

        if (redis.Enabled && redis.RequiredForReadiness)
        {
            healthChecks.AddCheck<RedisReadinessHealthCheck>("redis", tags: ["ready"]);
        }
    }
}
