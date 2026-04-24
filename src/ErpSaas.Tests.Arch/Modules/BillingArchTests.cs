using System.Reflection;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Billing.Entities;
using ErpSaas.Modules.Billing.Infrastructure;
using ErpSaas.Shared.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class BillingArchTests
{
    private static readonly Assembly BillingAssembly =
        typeof(ErpSaas.Modules.Billing.Services.BillingService).Assembly;

    // ── Schema enforcement ────────────────────────────────────────────────────

    [Fact]
    public void InvoiceEntities_AreInSalesSchema()
    {
        var stubCtx = new StubTenantContext();
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase("billing-arch-schema")
            .Options;
        var ai = new AuditSaveChangesInterceptor(stubCtx);
        var ti = new TenantSaveChangesInterceptor(stubCtx);

        // Build a minimal model with only Billing entities to check schema.
        var modelBuilder = new ModelBuilder();
        BillingModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        var billingEntities = new[] { typeof(Invoice), typeof(InvoiceLine) };

        foreach (var entityType in billingEntities)
        {
            var entityTypeMeta = model.FindEntityType(entityType);
            entityTypeMeta.Should().NotBeNull($"{entityType.Name} must be registered in BillingModelConfiguration");
            entityTypeMeta!.GetSchema().Should().Be("sales",
                $"{entityType.Name} must be in the 'sales' schema per CLAUDE.md §4.2");
        }
    }

    // ── Inheritance rules ─────────────────────────────────────────────────────

    [Fact]
    public void BillingEntities_ExtendTenantEntity()
    {
        typeof(Invoice).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(InvoiceLine).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
    }

    [Fact]
    public void BillingService_ExtendsBaseService()
    {
        // NetArchTest Inherit() has limitations with open generics.
        // Use direct reflection to verify the inheritance chain.
        var serviceTypes = BillingAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Service"))
            .ToList();

        serviceTypes.Should().NotBeEmpty("Billing module must have at least one concrete service class");

        var violations = serviceTypes
            .Where(t => !IsSubclassOfBaseService(t))
            .Select(t => t.FullName!)
            .ToList();

        violations.Should().BeEmpty(
            $"these service classes do not extend BaseService<TenantDbContext>: {string.Join(", ", violations)}");
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
    public void BillingController_ExtendsBaseController()
    {
        var result = Types.InAssembly(BillingAssembly)
            .That().HaveNameEndingWith("Controller")
            .Should().Inherit(typeof(ErpSaas.Shared.Controllers.BaseController))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    // ── Unit-snapshot fields on InvoiceLine (CLAUDE.md §3.7) ─────────────────

    [Fact]
    public void InvoiceLine_HasAllUnitSnapshotFields()
    {
        var type = typeof(InvoiceLine);
        var required = new[]
        {
            nameof(InvoiceLine.ProductUnitId),
            nameof(InvoiceLine.UnitCodeSnapshot),
            nameof(InvoiceLine.ConversionFactorSnapshot),
            nameof(InvoiceLine.QuantityInBilledUnit),
            nameof(InvoiceLine.QuantityInBaseUnit),
        };

        foreach (var prop in required)
        {
            type.GetProperty(prop).Should().NotBeNull(
                $"InvoiceLine must have '{prop}' per CLAUDE.md §3.7");
        }
    }

    // ── Namespace discipline ──────────────────────────────────────────────────

    [Fact]
    public void BillingModule_DoesNotDependOnOtherBusinessModules()
    {
        // Billing must not take a compile-time dependency on CRM, Inventory, etc.
        // Those integrations go through shared TenantDbContext + db.Set<T>() only.
        var otherModuleNamespaces = new[]
        {
            "ErpSaas.Modules.Crm",
            "ErpSaas.Modules.Inventory",
            "ErpSaas.Modules.Hr",
            "ErpSaas.Modules.Accounting",
        };

        foreach (var ns in otherModuleNamespaces)
        {
            var result = Types.InAssembly(BillingAssembly)
                .ShouldNot().HaveDependencyOn(ns)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"Billing must not depend on {ns}: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    // ── Required test classes (this class is one of them) ────────────────────

    [Fact]
    public void BillingModule_HasAllSixRequiredTestClasses()
    {
        // CLAUDE.md §6.1: every module ships six test classes.
        //
        // We verify existence via the source tree so that this check works
        // regardless of which test project triggered the run.
        // The sibling test assemblies are in different output directories,
        // so we locate them relative to the repo root.
        var repoRoot = GetRepoRoot();

        var required = new[]
        {
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Unit",        "Modules", "Billing", "BillingServiceTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Billing", "BillingControllerTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Billing", "BillingTenantIsolationTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Billing", "BillingSubscriptionGateTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Billing", "BillingAuditTrailTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Arch",        "Modules", "BillingArchTests.cs"),
        };

        var missing = required.Where(f => !File.Exists(f)).ToList();
        missing.Should().BeEmpty(
            $"the following required Billing test files are missing: {string.Join(", ", missing)}");
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
