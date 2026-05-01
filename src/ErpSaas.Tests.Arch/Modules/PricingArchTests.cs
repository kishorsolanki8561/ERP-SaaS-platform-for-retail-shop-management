using System.Reflection;
using ErpSaas.Modules.Pricing.Entities;
using ErpSaas.Modules.Pricing.Infrastructure;
using ErpSaas.Modules.Pricing.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class PricingArchTests
{
    private static readonly Assembly PricingAssembly = typeof(PricingManagementService).Assembly;

    private static string GetSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("src/ root not found");
    }

    [Fact]
    public void Pricing_HasAllSixRequiredTestClasses()
    {
        var root = GetSrcRoot();
        string[] required =
        [
            Path.Combine(root, "ErpSaas.Tests.Unit", "Modules", "Pricing", "PricingEngineTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Pricing", "PricingControllerTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Pricing", "PricingTenantIsolationTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Pricing", "PricingSubscriptionGateTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Pricing", "PricingAuditTrailTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Arch", "Modules", "PricingArchTests.cs"),
        ];
        foreach (var f in required)
            File.Exists(f).Should().BeTrue($"Missing required test file: {f}");
    }

    [Fact]
    public void PricingEntities_AreInPricingSchema()
    {
        var modelBuilder = new ModelBuilder();
        PricingModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        foreach (var entityType in new[] { typeof(DiscountRule), typeof(ExtraChargeRule), typeof(Offer) })
        {
            var meta = model.FindEntityType(entityType);
            meta.Should().NotBeNull();
            meta!.GetSchema().Should().Be(PricingModelConfiguration.Schema,
                $"{entityType.Name} must be in '{PricingModelConfiguration.Schema}' schema");
        }
    }

    [Fact]
    public void PricingService_DoesNotUseRawHttpClient()
    {
        var result = Types.InAssembly(PricingAssembly)
            .That().ResideInNamespaceStartingWith("ErpSaas.Modules.Pricing.Services")
            .ShouldNot().HaveDependencyOn("System.Net.Http.HttpClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Pricing services must not use HttpClient directly");
    }

    [Fact]
    public void PricingService_RegistersInterface()
    {
        var impl = PricingAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IPricingManagementService).IsAssignableFrom(t))
            .ToList();
        impl.Should().ContainSingle();
    }
}
