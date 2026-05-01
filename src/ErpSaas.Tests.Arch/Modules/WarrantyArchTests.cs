using System.Reflection;
using ErpSaas.Modules.Warranty.Entities;
using ErpSaas.Modules.Warranty.Infrastructure;
using ErpSaas.Modules.Warranty.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class WarrantyArchTests
{
    private static readonly Assembly WarrantyAssembly = typeof(WarrantyService).Assembly;

    private static string GetSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("src/ root not found");
    }

    [Fact]
    public void Warranty_HasAllSixRequiredTestClasses()
    {
        var root = GetSrcRoot();
        string[] required =
        [
            Path.Combine(root, "ErpSaas.Tests.Unit", "Modules", "Warranty", "WarrantyServiceTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Warranty", "WarrantyControllerTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Warranty", "WarrantyTenantIsolationTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Warranty", "WarrantySubscriptionGateTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Warranty", "WarrantyAuditTrailTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Arch", "Modules", "WarrantyArchTests.cs"),
        ];
        foreach (var f in required)
            File.Exists(f).Should().BeTrue($"Missing required test file: {f}");
    }

    [Fact]
    public void WarrantyEntities_AreInWarrantySchema()
    {
        var modelBuilder = new ModelBuilder();
        WarrantyModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        foreach (var entityType in new[] { typeof(WarrantyRegistration), typeof(WarrantyClaim) })
        {
            var meta = model.FindEntityType(entityType);
            meta.Should().NotBeNull();
            meta!.GetSchema().Should().Be(WarrantyModelConfiguration.Schema,
                $"{entityType.Name} must be in '{WarrantyModelConfiguration.Schema}' schema");
        }
    }

    [Fact]
    public void WarrantyService_DoesNotUseRawHttpClient()
    {
        var result = Types.InAssembly(WarrantyAssembly)
            .That().ResideInNamespaceStartingWith("ErpSaas.Modules.Warranty.Services")
            .ShouldNot().HaveDependencyOn("System.Net.Http.HttpClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Warranty services must not use HttpClient directly");
    }

    [Fact]
    public void WarrantyService_RegistersInterface()
    {
        var impl = WarrantyAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IWarrantyService).IsAssignableFrom(t))
            .ToList();
        impl.Should().ContainSingle();
    }
}
