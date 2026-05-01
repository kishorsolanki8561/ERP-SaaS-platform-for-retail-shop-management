using System.Reflection;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Purchasing.Entities;
using ErpSaas.Modules.Purchasing.Infrastructure;
using ErpSaas.Shared.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class PurchasingArchTests
{
    private static readonly Assembly PurchasingAssembly =
        typeof(ErpSaas.Modules.Purchasing.Services.PurchasingService).Assembly;

    [Fact]
    public void PurchasingEntities_AreInPurchasingSchema()
    {
        var modelBuilder = new ModelBuilder();
        PurchasingModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        var entities = new[]
        {
            typeof(Supplier), typeof(PurchaseOrder), typeof(PurchaseOrderLine),
            typeof(Bill), typeof(BillPayment),
        };

        foreach (var entityType in entities)
        {
            var meta = model.FindEntityType(entityType);
            meta.Should().NotBeNull($"{entityType.Name} must be registered in PurchasingModelConfiguration");
            meta!.GetSchema().Should().Be(PurchasingModelConfiguration.Schema,
                $"{entityType.Name} must be in the 'purchasing' schema");
        }
    }

    [Fact]
    public void PurchasingEntities_ExtendTenantEntity()
    {
        typeof(Supplier).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(PurchaseOrder).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(PurchaseOrderLine).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(Bill).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(BillPayment).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
    }

    [Fact]
    public void PurchasingService_ExtendsBaseService()
    {
        typeof(ErpSaas.Modules.Purchasing.Services.PurchasingService)
            .IsSubclassOf(typeof(BaseService<TenantDbContext>))
            .Should().BeTrue("PurchasingService must extend BaseService<TenantDbContext>");
    }

    [Fact]
    public void PurchasingModule_HasNoRawSqlInBusinessServices()
    {
        var result = Types.InAssembly(PurchasingAssembly)
            .That().ResideInNamespace("ErpSaas.Modules.Purchasing.Services")
            .Should().NotHaveDependencyOn("Microsoft.Data.SqlClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Services must use EF Core or Dapper via IDapperContext, not raw SqlClient");
    }

    [Fact]
    public void PurchasingOrderLine_HasAllUnitSnapshotFields()
    {
        var type = typeof(PurchaseOrderLine);
        var required = new[]
        {
            nameof(PurchaseOrderLine.ProductUnitId),
            nameof(PurchaseOrderLine.UnitCodeSnapshot),
            nameof(PurchaseOrderLine.ConversionFactorSnapshot),
            nameof(PurchaseOrderLine.QuantityInBilledUnit),
            nameof(PurchaseOrderLine.QuantityInBaseUnit),
        };

        foreach (var prop in required)
            type.GetProperty(prop).Should().NotBeNull($"PurchaseOrderLine must have '{prop}' per CLAUDE.md §3.7");
    }

    [Fact]
    public void PurchasingModule_HasAllSixRequiredTestClasses()
    {
        var repoRoot = GetRepoRoot();

        var required = new[]
        {
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Unit",        "Modules", "Purchasing", "PurchasingServiceTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Purchasing", "PurchasingControllerTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Purchasing", "PurchasingTenantIsolationTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Purchasing", "PurchasingSubscriptionGateTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Purchasing", "PurchasingAuditTrailTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Arch",        "Modules", "PurchasingArchTests.cs"),
        };

        var missing = required.Where(f => !File.Exists(f)).ToList();
        missing.Should().BeEmpty(
            $"the following required Purchasing test files are missing: {string.Join(", ", missing)}");
    }

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ErpSaas.sln")))
            dir = dir.Parent;
        return dir?.Parent?.FullName
            ?? throw new InvalidOperationException("Could not locate repo root.");
    }
}
