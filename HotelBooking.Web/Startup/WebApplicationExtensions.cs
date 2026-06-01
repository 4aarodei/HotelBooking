using HotelBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Web.Startup;

internal static class WebApplicationExtensions
{
    // Infra middleware: proxy headers and environment-specific error handling.
    public static void UseHotelBookingInfrastructurePipeline(this WebApplication app)
    {
        if (ShouldUseForwardedHeaders(app.Environment, app.Configuration))
        {
            app.UseForwardedHeaders();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
            return;
        }

        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    // Dev-only bootstrap data and migrations.
    public static async Task SeedDevelopmentDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();
        await IdentitySeeder.SeedAsync(services);
        await DemoDataSeeder.SeedAsync(services);
    }

    // Main web pipeline and MVC routes.
    public static void UseHotelBookingRoutingPipeline(this WebApplication app)
    {
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
    }

    // Health endpoints for live and ready probes.
    public static void MapHotelBookingHealthEndpoints(this WebApplication app)
    {
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
    }

    // Enable forwarded headers outside Development, or by explicit switch.
    private static bool ShouldUseForwardedHeaders(IHostEnvironment environment, IConfiguration configuration)
    {
        return !environment.IsDevelopment() || configuration.GetValue<bool>("ReverseProxy:Enabled");
    }
}
