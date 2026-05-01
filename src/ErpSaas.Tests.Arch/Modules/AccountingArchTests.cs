using System.Reflection;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Accounting.Entities;
using ErpSaas.Modules.Accounting.Infrastructure;
using ErpSaas.Modules.Accounting.Services;
using ErpSaas.Shared.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class AccountingArchTests
{
    private static readonly Assembly AccountingAssembly =
        typeof(AccountingService).Assembly;

    // ── Schema enforcement ────────────────────────────────────────────────────

    [Fact]
    public void AccountingEntities_AreInAccountingSchema()
    {
        var modelBuilder = new ModelBuilder();
        AccountingModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        var entities = new[]
        {
            typeof(AccountGroup), typeof(Account), typeof(Voucher),
            typeof(VoucherEntry), typeof(Expense), typeof(BankAccount),
            typeof(FinancialYear),
        };

        foreach (var entityType in entities)
        {
            var meta = model.FindEntityType(entityType);
            meta.Should().NotBeNull($"{entityType.Name} must be registered in AccountingModelConfiguration");
            meta!.GetSchema().Should().Be(AccountingModelConfiguration.Schema,
                $"{entityType.Name} must be in the 'accounting' schema per CLAUDE.md §4.2");
        }
    }

    // ── Inheritance rules ─────────────────────────────────────────────────────

    [Fact]
    public void AccountingEntities_ExtendTenantEntity()
    {
        typeof(AccountGroup).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(Account).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(Voucher).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(VoucherEntry).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(Expense).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(BankAccount).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(FinancialYear).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
    }

    [Fact]
    public void AccountingService_ExtendsBaseService()
    {
        typeof(AccountingService).IsSubclassOf(typeof(BaseService<TenantDbContext>))
            .Should().BeTrue("AccountingService must extend BaseService<TenantDbContext>");
    }

    // ── No raw SaveChangesAsync outside ExecuteAsync ───────────────────────────

    [Fact]
    public void AccountingModule_HasNoRawSqlInBusinessServices()
    {
        var result = Types.InAssembly(AccountingAssembly)
            .That().ResideInNamespace("ErpSaas.Modules.Accounting.Services")
            .Should().NotHaveDependencyOn("Microsoft.Data.SqlClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Services must use EF Core or Dapper via IDapperContext, not raw SqlClient");
    }

    // ── Module has all 6 required test classes ────────────────────────────────

    [Fact]
    public void AccountingModule_HasAllSixRequiredTestClasses()
    {
        var repoRoot = GetRepoRoot();

        var required = new[]
        {
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Unit",        "Modules", "Accounting", "AccountingServiceTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Accounting", "AccountingControllerTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Accounting", "AccountingTenantIsolationTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Accounting", "AccountingSubscriptionGateTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Accounting", "AccountingAuditTrailTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Arch",        "Modules", "AccountingArchTests.cs"),
        };

        var missing = required.Where(f => !File.Exists(f)).ToList();
        missing.Should().BeEmpty(
            $"the following required Accounting test files are missing: {string.Join(", ", missing)}");
    }

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ErpSaas.sln")))
            dir = dir.Parent;
        return dir?.Parent?.FullName
            ?? throw new InvalidOperationException("Could not locate repo root (no ErpSaas.sln found).");
    }
}
