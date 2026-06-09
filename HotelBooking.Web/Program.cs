using HotelBooking.Web.Startup;

var builder = WebApplication.CreateBuilder(args);

// 1) Bootstrap config and logging.
builder.ConfigureKeyVaultIfEnabled();
builder.ConfigureDevelopmentLogging();

// 2) Read and validate runtime settings.
var runtimeSettings = builder.BuildRuntimeSettings();
builder.ValidateRuntimeSettings(runtimeSettings);

// 3) Register infrastructure services.
builder.Services.AddHotelBookingDatabase(builder.Configuration, runtimeSettings.ConnectionString);
builder.Services.AddHotelBookingDataProtection(builder.Configuration, builder.Environment);
builder.Services.AddHotelBookingForwardedHeaders(builder.Configuration);
builder.Services.AddHotelBookingApplicationServices();

// 4) Register app features.
builder.Services.AddHotelBookingIdentity(runtimeSettings.RequireConfirmedAccount);
builder.Services.AddHotelBookingOptions(builder.Configuration);
builder.Services.AddHotelBookingRedis(builder.Configuration);
builder.Services.AddHotelBookingEmailSender(runtimeSettings.HasSmtpEmail);
builder.Services.AddHotelBookingImageStorage(runtimeSettings.ImageStorageProvider);
builder.Services.AddHotelBookingMvcAndHealthChecks(builder.Configuration);

var app = builder.Build();

// 5) Configure middleware pipeline.
app.UseHotelBookingInfrastructurePipeline();

if (app.Environment.IsDevelopment())
{
    // Local setup data for demo and testing.
    await app.SeedDevelopmentDataAsync();
}

// 6) Map routes and health endpoints.
app.UseHotelBookingRoutingPipeline();
app.MapHotelBookingHealthEndpoints();

app.Run();
