using System.Reflection;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Wallet.Entities;
using ErpSaas.Modules.Wallet.Infrastructure;
using ErpSaas.Shared.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class WalletArchTests
{
    private static readonly Assembly WalletAssembly =
        typeof(ErpSaas.Modules.Wallet.Services.WalletService).Assembly;

    // ── Schema enforcement ────────────────────────────────────────────────────

    [Fact]
    public void WalletEntities_AreInWalletSchema()
    {
        // Build a minimal model with only Wallet entities to check schema.
        var modelBuilder = new ModelBuilder();
        WalletModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        var walletEntities = new[] { typeof(WalletBalance), typeof(WalletTransaction) };

        foreach (var entityType in walletEntities)
        {
            var entityTypeMeta = model.FindEntityType(entityType);
            entityTypeMeta.Should().NotBeNull(
                $"{entityType.Name} must be registered in WalletModelConfiguration");
            entityTypeMeta!.GetSchema().Should().Be("wallet",
                $"{entityType.Name} must be in the 'wallet' schema per CLAUDE.md §4.2");
        }
    }

    // ── Inheritance rules ─────────────────────────────────────────────────────

    [Fact]
    public void WalletEntities_ExtendTenantEntity()
    {
        typeof(WalletBalance).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(WalletTransaction).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
    }

    [Fact]
    public void WalletService_ExtendsBaseService()
    {
        var serviceTypes = WalletAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Service"))
            .ToList();

        serviceTypes.Should().NotBeEmpty(
            "Wallet module must have at least one concrete service class");

        var violations = serviceTypes
            .Where(t => !IsSubclassOfBaseService(t))
            .Select(t => t.FullName!)
            .ToList();

        violations.Should().BeEmpty(
            $"these service classes do not extend BaseService<TenantDbContext>: " +
            $"{string.Join(", ", violations)}");
    }

    private static bool IsSubclassOfBaseService(Type t)
    {
        var current = t.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType
                && current.GetGenericTypeDefinition() == typeof(BaseService<>)
                && current.GetGenericArguments().Length == 1
                && current.GetGenericArguments()[0] == typeof(TenantDbContext))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    // ── Controller rules ──────────────────────────────────────────────────────

    [Fact]
    public void WalletController_ExtendsBaseController()
    {
        var result = Types.InAssembly(WalletAssembly)
            .That().HaveNameEndingWith("Controller")
            .Should().Inherit(typeof(ErpSaas.Shared.Controllers.BaseController))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    // ── Namespace discipline ──────────────────────────────────────────────────

    [Fact]
    public void WalletModule_DoesNotDependOnOtherBusinessModules()
    {
        var otherModuleNamespaces = new[]
        {
            "ErpSaas.Modules.Crm",
            "ErpSaas.Modules.Inventory",
            "ErpSaas.Modules.Hr",
            "ErpSaas.Modules.Accounting",
            "ErpSaas.Modules.Billing",
            "ErpSaas.Modules.Shift",
        };

        foreach (var ns in otherModuleNamespaces)
        {
            var result = Types.InAssembly(WalletAssembly)
                .ShouldNot().HaveDependencyOn(ns)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"Wallet must not depend on {ns}: " +
                $"{string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    // ── Enum-as-string enforcement (CLAUDE.md §3.9) ───────────────────────────

    [Fact]
    public void WalletTransaction_TransactionType_IsStoredAsString()
    {
        var modelBuilder = new ModelBuilder();
        WalletModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        var txEntity = model.FindEntityType(typeof(WalletTransaction));
        txEntity.Should().NotBeNull();

        var prop = txEntity!.FindProperty(nameof(WalletTransaction.TransactionType));
        prop.Should().NotBeNull();
        // HasConversion<string>() means the column type maps to a CLR string
        prop!.GetValueConverter()?.ProviderClrType.Should().Be(typeof(string),
            "TransactionType must use HasConversion<string>() per CLAUDE.md §3.9");
    }

    // ── Required test classes (this class is one of them) ────────────────────

    [Fact]
    public void WalletModule_HasAllSixRequiredTestClasses()
    {
        var repoRoot = GetRepoRoot();

        var required = new[]
        {
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Unit",        "Modules", "Wallet", "WalletServiceTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Wallet", "WalletControllerTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Wallet", "WalletTenantIsolationTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Wallet", "WalletSubscriptionGateTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Wallet", "WalletAuditTrailTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Arch",        "Modules", "WalletArchTests.cs"),
        };

        var missing = required.Where(f => !File.Exists(f)).ToList();
        missing.Should().BeEmpty(
            $"the following required Wallet test files are missing: {string.Join(", ", missing)}");
    }

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ErpSaas.sln")))
            dir = dir.Parent;
        return dir?.Parent?.FullName
            ?? throw new InvalidOperationException("Could not locate repo root (no ErpSaas.sln found).");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class StubTenantContext : ITenantContext
    {
        public long ShopId => 1;
        public long CurrentUserId => 1;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
