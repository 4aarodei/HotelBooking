using HotelBooking.Application.Persistence;
using HotelBooking.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace HotelBooking.Tests.Architecture;

public class ArchitectureBoundaryTests
{
    [Fact]
    public void Domain_DoesNotReference_AspNetIdentityOrEfCore()
    {
        var references = typeof(HotelBooking.Domain.Entities.Bookings.Booking).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        Assert.DoesNotContain(references, name => name is not null && name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name is not null && name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name is not null && name.Contains("Identity", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Application_DoesNotReference_InfrastructureOrWeb()
    {
        var references = typeof(IHotelRepository).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        Assert.DoesNotContain("HotelBooking.Infrastructure", references);
        Assert.DoesNotContain("HotelBooking.Web", references);
    }

    [Fact]
    public void WebControllers_DoNotDependOnRepositories()
    {
        var repositoryTypes = typeof(IHotelRepository).Assembly
            .GetTypes()
            .Where(t => t.IsInterface && t.Namespace == typeof(IHotelRepository).Namespace && t.Name.EndsWith("Repository", StringComparison.Ordinal))
            .ToHashSet();

        var controllerTypes = typeof(HomeController).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(Controller).IsAssignableFrom(t));

        var violations = controllerTypes
            .SelectMany(controller => controller.GetConstructors()
                .SelectMany(ctor => ctor.GetParameters()
                    .Where(parameter => repositoryTypes.Contains(parameter.ParameterType))
                    .Select(parameter => $"{controller.FullName} -> {parameter.ParameterType.FullName}")))
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void WebProject_DoesNotReference_DomainProjectDirectly()
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "HotelBooking.Web",
            "HotelBooking.Web.csproj"));

        var projectXml = File.ReadAllText(projectPath);

        Assert.DoesNotContain("HotelBooking.Domain.csproj", projectXml, StringComparison.OrdinalIgnoreCase);
    }
}
