using ErpSaas.Modules.Inventory.Services;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

/// <summary>
/// Architecture tests specific to the Inventory module.
/// These run as part of the <c>Category=Architecture</c> filter.
/// </summary>
[Trait("Category", "Architecture")]
[Trait("Module", "Inventory")]
public class InventoryArchTests
{
    private static readonly System.Reflection.Assembly InventoryAssembly =
        typeof(InventoryService).Assembly;

    // ── Schema correctness ────────────────────────────────────────────────────

    [Fact]
    public void InventoryEntities_MustUseInventorySchema()
    {
        // All entities in the Inventory module namespace must be mapped to schema "inventory".
        // This is enforced by registering them in TenantDbContext.OnModelCreating with
        // b.ToTable("...", schema: "inventory"). The arch test validates the in-memory model.

        var stubCtx = new StubTenantContext();
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase("arch-inventory-schema")
            .Options;

        var ai = new ErpSaas.Infrastructure.Data.Interceptors.AuditSaveChangesInterceptor(stubCtx);
        var ti = new ErpSaas.Infrastructure.Data.Interceptors.TenantSaveChangesInterceptor(stubCtx);
        using var db = new TenantDbContext(opts, stubCtx, ai, ti, []);

        // Collect entity types whose CLR type lives in the Inventory.Entities namespace.
        var inventoryEntities = db.Model.GetEntityTypes()
            .Where(et => et.ClrType.Namespace?.StartsWith(
                "ErpSaas.Modules.Inventory.Entities", StringComparison.Ordinal) == true)
            .ToList();

        // When the Inventory module is wired up, there will be entities here.
        // During Phase 0 / before TenantDbContext is updated, this list may be empty —
        // that is acceptable (the test does not fail on zero entities).
        var wrongSchema = inventoryEntities
            .Where(et =>
            {
                var schema = et.GetSchema();
                return schema is null || schema != "inventory";
            })
            .Select(et => $"{et.ClrType.Name} (schema='{et.GetSchema() ?? "null"}')")
            .ToList();

        wrongSchema.Should().BeEmpty(
            "all Inventory module entities must declare schema: \"inventory\"");
    }

    // ── Service pattern ───────────────────────────────────────────────────────

    [Fact]
    public void InventoryService_MustExtend_BaseServiceOfTenantDbContext()
    {
        // NetArchTest Inherit() has limitations with open generics — use reflection instead.
        var serviceTypes = InventoryAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Service"))
            .ToList();

        serviceTypes.Should().NotBeEmpty("Inventory module must have at least one concrete service class");

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

    // ── Controller pattern ────────────────────────────────────────────────────

    [Fact]
    public void InventoryControllers_MustExtend_BaseController()
    {
        var result = Types.InAssembly(InventoryAssembly)
            .That().HaveNameEndingWith("Controller")
            .And().AreNotAbstract()
            .Should().Inherit(typeof(ErpSaas.Shared.Controllers.BaseController))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    // ── Namespace isolation ───────────────────────────────────────────────────

    [Fact]
    public void Inventory_MustNotDependOn_PlatformDbContext_Directly()
    {
        // Business module services and controllers must not inject PlatformDbContext.
        // Seeders are exempt — they legitimately seed platform-level data (permissions, menus).
        var types = InventoryAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                && !t.Name.EndsWith("Seeder", StringComparison.Ordinal));

        var violations = new List<string>();
        foreach (var type in types)
        {
            var ctors = type.GetConstructors();
            foreach (var ctor in ctors)
            {
                var banned = ctor.GetParameters()
                    .Where(p => p.ParameterType == typeof(PlatformDbContext)
                                || p.ParameterType == typeof(LogDbContext)
                                || p.ParameterType == typeof(AnalyticsDbContext))
                    .Select(p => $"{type.Name}({p.ParameterType.Name})")
                    .ToList();
                violations.AddRange(banned);
            }
        }

        violations.Should().BeEmpty(
            $"Inventory module classes inject forbidden DbContexts: {string.Join(", ", violations)}");
    }

    // ── TenantEntity isolation ────────────────────────────────────────────────

    [Fact]
    public void InventoryEntities_MustExtend_TenantEntity()
    {
        var result = Types.InAssembly(InventoryAssembly)
            .That().ResideInNamespace("ErpSaas.Modules.Inventory.Entities")
            .And().AreClasses()
            .And().AreNotAbstract()
            .Should().Inherit(typeof(TenantEntity))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    // ── Stub helper ──────────────────────────────────────────────────────────

    private sealed class StubTenantContext : ITenantContext
    {
        public long ShopId => 1;
        public long CurrentUserId => 1;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
