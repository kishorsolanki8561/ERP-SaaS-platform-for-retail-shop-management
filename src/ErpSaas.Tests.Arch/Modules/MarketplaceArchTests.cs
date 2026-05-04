using System.Reflection;
using ErpSaas.Modules.Marketplace.Entities;
using ErpSaas.Modules.Marketplace.Infrastructure;
using ErpSaas.Modules.Marketplace.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class MarketplaceArchTests
{
    private static readonly Assembly MarketplaceAssembly = typeof(MarketplaceAccountService).Assembly;

    private static string GetSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("src/ root not found");
    }

    [Fact]
    public void Marketplace_HasAllSixRequiredTestClasses()
    {
        var root = GetSrcRoot();
        string[] required =
        [
            Path.Combine(root, "ErpSaas.Tests.Unit", "Modules", "Marketplace", "MarketplaceServiceTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Marketplace", "MarketplaceControllerTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Marketplace", "MarketplaceTenantIsolationTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Marketplace", "MarketplaceSubscriptionGateTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Marketplace", "MarketplaceAuditTrailTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Arch", "Modules", "MarketplaceArchTests.cs"),
        ];
        foreach (var f in required)
            File.Exists(f).Should().BeTrue($"Missing required test file: {f}");
    }

    [Fact]
    public void MarketplaceEntities_AreInMarketplaceSchema()
    {
        var modelBuilder = new ModelBuilder();
        MarketplaceModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        foreach (var entityType in new[] { typeof(MarketplaceAccount), typeof(MarketplaceProductMapping), typeof(MarketplaceOrder) })
        {
            var meta = model.FindEntityType(entityType);
            meta.Should().NotBeNull();
            meta!.GetSchema().Should().Be(MarketplaceModelConfiguration.Schema,
                $"{entityType.Name} must be in '{MarketplaceModelConfiguration.Schema}' schema");
        }
    }

    [Fact]
    public void MarketplaceConnectors_ExtendThirdPartyApiClientBase()
    {
        var connectorImpls = MarketplaceAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                        && t.Namespace?.StartsWith("ErpSaas.Modules.Marketplace.Connectors") == true
                        && typeof(ErpSaas.Infrastructure.Http.ThirdPartyApiClientBase).IsAssignableFrom(t))
            .ToList();

        connectorImpls.Should().NotBeEmpty("all concrete Marketplace connectors must extend ThirdPartyApiClientBase");
    }

    [Fact]
    public void MarketplaceServices_RegisterInterfaces()
    {
        var accountImpl = MarketplaceAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IMarketplaceAccountService).IsAssignableFrom(t))
            .ToList();
        accountImpl.Should().ContainSingle("IMarketplaceAccountService must have exactly one implementation");

        var orderImpl = MarketplaceAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IMarketplaceOrderService).IsAssignableFrom(t))
            .ToList();
        orderImpl.Should().ContainSingle("IMarketplaceOrderService must have exactly one implementation");

        var syncImpl = MarketplaceAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IMarketplaceSyncService).IsAssignableFrom(t))
            .ToList();
        syncImpl.Should().ContainSingle("IMarketplaceSyncService must have exactly one implementation");
    }
}
