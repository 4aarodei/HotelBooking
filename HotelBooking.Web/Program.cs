using HotelBooking.Application;
using HotelBooking.Application.Media;
using HotelBooking.Domain.Entities.Identity;
using HotelBooking.Infrastructure;
using HotelBooking.Infrastructure.Data;
using HotelBooking.Infrastructure.Storage;
using HotelBooking.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
    options.UseSqlServer(connectionString));

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

var imageStorageProvider = builder.Configuration.GetValue<string>("ImageStorage:Provider") ?? "Local";
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
builder.Services.AddHealthChecks();

var app = builder.Build();

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

app.MapRazorPages();

app.MapHealthChecks("/health");

app.Run();
