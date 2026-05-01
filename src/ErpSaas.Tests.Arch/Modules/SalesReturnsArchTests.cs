using System.Reflection;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.SalesReturns.Entities;
using ErpSaas.Modules.SalesReturns.Infrastructure;
using ErpSaas.Shared.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class SalesReturnsArchTests
{
    private static readonly Assembly SalesReturnsAssembly =
        typeof(ErpSaas.Modules.SalesReturns.Services.SalesReturnsService).Assembly;

    [Fact]
    public void SalesReturnsEntities_AreInSalesSchema()
    {
        var modelBuilder = new ModelBuilder();
        SalesReturnsModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        var entities = new[] { typeof(SalesReturn), typeof(SalesReturnLine), typeof(CreditNote) };

        foreach (var entityType in entities)
        {
            var meta = model.FindEntityType(entityType);
            meta.Should().NotBeNull($"{entityType.Name} must be registered");
            meta!.GetSchema().Should().Be(SalesReturnsModelConfiguration.Schema,
                $"{entityType.Name} must be in 'sales' schema");
        }
    }

    [Fact]
    public void SalesReturnsEntities_ExtendTenantEntity()
    {
        typeof(SalesReturn).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(SalesReturnLine).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(CreditNote).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
    }

    [Fact]
    public void SalesReturnsService_ExtendsBaseService()
    {
        typeof(ErpSaas.Modules.SalesReturns.Services.SalesReturnsService)
            .IsSubclassOf(typeof(BaseService<TenantDbContext>))
            .Should().BeTrue();
    }

    [Fact]
    public void SalesReturnsModule_HasNoRawSqlInBusinessServices()
    {
        var result = Types.InAssembly(SalesReturnsAssembly)
            .That().ResideInNamespace("ErpSaas.Modules.SalesReturns.Services")
            .Should().NotHaveDependencyOn("Microsoft.Data.SqlClient")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void SalesReturnLine_HasAllUnitSnapshotFields()
    {
        var type = typeof(SalesReturnLine);
        new[] { nameof(SalesReturnLine.ProductUnitId), nameof(SalesReturnLine.UnitCodeSnapshot),
                nameof(SalesReturnLine.ConversionFactorSnapshot), nameof(SalesReturnLine.QuantityInBilledUnit),
                nameof(SalesReturnLine.QuantityInBaseUnit) }
            .ToList().ForEach(prop => type.GetProperty(prop).Should().NotBeNull($"SalesReturnLine must have '{prop}'"));
    }

    [Fact]
    public void SalesReturnsModule_HasAllSixRequiredTestClasses()
    {
        var repoRoot = GetRepoRoot();
        var required = new[]
        {
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Unit",        "Modules", "SalesReturns", "SalesReturnsServiceTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "SalesReturns", "SalesReturnsControllerTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "SalesReturns", "SalesReturnsTenantIsolationTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "SalesReturns", "SalesReturnsSubscriptionGateTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "SalesReturns", "SalesReturnsAuditTrailTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Arch",        "Modules", "SalesReturnsArchTests.cs"),
        };
        var missing = required.Where(f => !File.Exists(f)).ToList();
        missing.Should().BeEmpty($"missing: {string.Join(", ", missing)}");
    }

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ErpSaas.sln")))
            dir = dir.Parent;
        return dir?.Parent?.FullName ?? throw new InvalidOperationException("Repo root not found.");
    }
}
