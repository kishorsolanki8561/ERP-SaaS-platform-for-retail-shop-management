using System.Reflection;
using ErpSaas.Modules.Transport.Entities;
using ErpSaas.Modules.Transport.Infrastructure;
using ErpSaas.Modules.Transport.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class TransportArchTests
{
    private static readonly Assembly TransportAssembly = typeof(TransportService).Assembly;

    private static string GetSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("src/ root not found");
    }

    [Fact]
    public void Transport_HasAllSixRequiredTestClasses()
    {
        var root = GetSrcRoot();
        string[] required =
        [
            Path.Combine(root, "ErpSaas.Tests.Unit", "Modules", "Transport", "TransportServiceTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Transport", "TransportControllerTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Transport", "TransportTenantIsolationTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Transport", "TransportSubscriptionGateTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Transport", "TransportAuditTrailTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Arch", "Modules", "TransportArchTests.cs"),
        ];
        foreach (var f in required)
            File.Exists(f).Should().BeTrue($"Missing required test file: {f}");
    }

    [Fact]
    public void TransportEntities_AreInTransportSchema()
    {
        var modelBuilder = new ModelBuilder();
        TransportModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        foreach (var entityType in new[] { typeof(TransportProvider), typeof(Vehicle), typeof(Delivery), typeof(DeliveryLog) })
        {
            var meta = model.FindEntityType(entityType);
            meta.Should().NotBeNull();
            meta!.GetSchema().Should().Be(TransportModelConfiguration.Schema,
                $"{entityType.Name} must be in '{TransportModelConfiguration.Schema}' schema");
        }
    }

    [Fact]
    public void TransportService_DoesNotUseRawHttpClient()
    {
        var result = Types.InAssembly(TransportAssembly)
            .That().ResideInNamespaceStartingWith("ErpSaas.Modules.Transport.Services")
            .ShouldNot().HaveDependencyOn("System.Net.Http.HttpClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Transport services must not use HttpClient directly");
    }

    [Fact]
    public void TransportService_RegistersInterface()
    {
        var impl = TransportAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ITransportService).IsAssignableFrom(t))
            .ToList();
        impl.Should().ContainSingle();
    }
}
