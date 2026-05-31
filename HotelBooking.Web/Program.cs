using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using HotelBooking.Application;
using HotelBooking.Application.Media;
using HotelBooking.Domain.Entities.Identity;
using HotelBooking.Infrastructure;
using HotelBooking.Infrastructure.Data;
using HotelBooking.Infrastructure.Storage;
using HotelBooking.Web.Health;
using HotelBooking.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
ConfigureKeyVault(builder);

if (builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}

var requireConfirmedAccount = builder.Configuration.GetValue<bool?>("Identity:RequireConfirmedAccount")
                              ?? !builder.Environment.IsDevelopment();
var smtpSection = builder.Configuration.GetSection("Email:Smtp");
var hasSmtpEmail =
    !string.IsNullOrWhiteSpace(smtpSection["Host"]) &&
    !string.IsNullOrWhiteSpace(smtpSection["From"]);

if (!builder.Environment.IsDevelopment() && requireConfirmedAccount && !hasSmtpEmail)
{
    throw new InvalidOperationException("SMTP email settings are required when confirmed accounts are enabled outside Development.");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServer =>
    {
        sqlServer.EnableRetryOnFailure(
            maxRetryCount: builder.Configuration.GetValue<int?>("Sql:MaxRetryCount") ?? 5,
            maxRetryDelay: TimeSpan.FromSeconds(builder.Configuration.GetValue<int?>("Sql:MaxRetryDelaySeconds") ?? 10),
            errorNumbersToAdd: null);
    }));

ConfigureDataProtection(builder.Services, builder.Configuration, builder.Environment);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = requireConfirmedAccount;
        options.User.RequireUniqueEmail = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<SmtpEmailOptions>(smtpSection);
builder.Services.Configure<ImageUploadOptions>(builder.Configuration.GetSection("ImageStorage"));
builder.Services.Configure<LocalImageStorageOptions>(builder.Configuration.GetSection("ImageStorage"));
builder.Services.Configure<AzureBlobImageStorageOptions>(builder.Configuration.GetSection("ImageStorage:AzureBlob"));
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
if (!hasSmtpEmail)
{
    builder.Services.AddTransient<IEmailSender, LoggingEmailSender>();
}
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(5);
});
builder.Services.AddScoped<RoomDraftImageUploadService>();

var imageStorageProvider = builder.Configuration.GetValue<string>("ImageStorage:Provider") ?? "Local";
ValidateImageStorageConfiguration(builder.Environment, imageStorageProvider, builder.Configuration);
if (imageStorageProvider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IImageStorage, AzureBlobImageStorage>();
}
else
{
    builder.Services.AddScoped<IImageStorage, LocalImageStorageService>();
}

builder.Services.AddInfrastructure();
builder.Services.AddApplication();

builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<DatabaseReadinessHealthCheck>("sql", tags: ["ready"]);

var app = builder.Build();

if (ShouldUseForwardedHeaders(app.Environment, app.Configuration))
{
    app.UseForwardedHeaders();
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();
    await IdentitySeeder.SeedAsync(services);
    await DemoDataSeeder.SeedAsync(services);
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.MapRazorPages();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

static void ConfigureKeyVault(WebApplicationBuilder builder)
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

    builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
}

static void ConfigureDataProtection(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
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

static bool ShouldUseForwardedHeaders(IHostEnvironment environment, IConfiguration configuration)
{
    return !environment.IsDevelopment() || configuration.GetValue<bool>("ReverseProxy:Enabled");
}

static void ValidateImageStorageConfiguration(IHostEnvironment environment, string imageStorageProvider, IConfiguration configuration)
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
