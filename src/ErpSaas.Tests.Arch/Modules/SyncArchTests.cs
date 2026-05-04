using System.Reflection;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Sync.Entities;
using ErpSaas.Modules.Sync.Infrastructure;
using ErpSaas.Shared.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class SyncArchTests
{
    private static readonly Assembly SyncAssembly =
        typeof(ErpSaas.Modules.Sync.Services.DeviceService).Assembly;

    // ── Schema enforcement ────────────────────────────────────────────────────

    [Fact]
    public void SyncEntities_AreInSyncSchema()
    {
        var modelBuilder = new ModelBuilder();
        SyncModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        var syncEntities = new[]
        {
            typeof(DeviceRegistration),
            typeof(OfflineCommand),
            typeof(InvoiceNumberAllocation),
        };

        foreach (var entityType in syncEntities)
        {
            var meta = model.FindEntityType(entityType);
            meta.Should().NotBeNull($"{entityType.Name} must be registered in SyncModelConfiguration");
            meta!.GetSchema().Should().Be("sync",
                $"{entityType.Name} must be in the 'sync' schema per CLAUDE.md §4.2");
        }
    }

    // ── Inheritance rules ─────────────────────────────────────────────────────

    [Fact]
    public void SyncEntities_ExtendTenantEntity()
    {
        typeof(DeviceRegistration).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(OfflineCommand).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(InvoiceNumberAllocation).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
    }

    [Fact]
    public void SyncServices_ExtendBaseService()
    {
        var serviceTypes = SyncAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Service"))
            .ToList();

        serviceTypes.Should().NotBeEmpty("Sync module must have at least one concrete service class");

        var violations = serviceTypes
            .Where(t => !IsSubclassOfBaseService(t))
            .Select(t => t.FullName!)
            .ToList();

        violations.Should().BeEmpty(
            $"these Sync service classes do not extend BaseService<TenantDbContext>: {string.Join(", ", violations)}");
    }

    private static bool IsSubclassOfBaseService(Type t)
    {
        var current = t.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType &&
                current.GetGenericTypeDefinition().FullName?.Contains("BaseService") == true)
                return true;
            current = current.BaseType;
        }
        return false;
    }

    // ── Namespace discipline ──────────────────────────────────────────────────

    [Fact]
    public void SyncModule_DoesNotDependOnOtherBusinessModules()
    {
        var otherModuleNamespaces = new[]
        {
            "ErpSaas.Modules.Billing",
            "ErpSaas.Modules.Inventory",
            "ErpSaas.Modules.Hr",
            "ErpSaas.Modules.Accounting",
            "ErpSaas.Modules.Crm",
            "ErpSaas.Modules.Pricing",
        };

        foreach (var ns in otherModuleNamespaces)
        {
            var result = Types.InAssembly(SyncAssembly)
                .ShouldNot().HaveDependencyOn(ns)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"Sync must not depend on {ns}: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    // ── Required test classes ─────────────────────────────────────────────────

    [Fact]
    public void SyncModule_HasAllSixRequiredTestClasses()
    {
        var repoRoot = GetRepoRoot();

        var required = new[]
        {
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Unit",        "Modules", "Sync", "SyncServiceTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Sync", "SyncControllerTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Sync", "SyncTenantIsolationTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Sync", "SyncSubscriptionGateTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Sync", "SyncAuditTrailTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Arch",        "Modules", "SyncArchTests.cs"),
        };

        var missing = required.Where(f => !File.Exists(f)).ToList();
        missing.Should().BeEmpty(
            $"the following required Sync test files are missing: {string.Join(", ", missing)}");
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
