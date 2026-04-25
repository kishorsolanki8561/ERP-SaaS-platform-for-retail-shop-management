#pragma warning disable CS9113 // Primary constructor parameter is unread (arch-test helper only)
using System.Reflection;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Shift.Entities;
using ErpSaas.Modules.Shift.Enums;
using ErpSaas.Modules.Shift.Infrastructure;
using ErpSaas.Shared.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;
using ShiftEntity = ErpSaas.Modules.Shift.Entities.Shift;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class ShiftArchTests
{
    private static readonly Assembly ShiftAssembly =
        typeof(ErpSaas.Modules.Shift.Services.ShiftService).Assembly;

    // ── Schema enforcement ────────────────────────────────────────────────────

    [Fact]
    public void ShiftEntities_AreInShiftSchema()
    {
        var stubCtx = new StubTenantContext();
        var ai = new AuditSaveChangesInterceptor(stubCtx);
        var ti = new TenantSaveChangesInterceptor(stubCtx);

        var opts = new DbContextOptionsBuilder<ShiftAwareTestDbContext>()
            .UseInMemoryDatabase("arch-shift-schema")
            .Options;

        using var db = new ShiftAwareTestDbContext(opts, stubCtx, ai, ti);

        var shiftEntities = db.Model.GetEntityTypes()
            .Where(e => e.ClrType.Namespace?.Contains("Modules.Shift") == true)
            .ToList();

        shiftEntities.Should().NotBeEmpty(
            "Shift entity configurations must be registered");

        foreach (var e in shiftEntities)
        {
            e.GetSchema().Should().Be("shift",
                $"{e.ClrType.Name} must be in schema 'shift' per CLAUDE.md §4.2");
        }
    }

    // ── Inheritance rules ─────────────────────────────────────────────────────

    [Fact]
    public void ShiftEntities_ExtendTenantEntity()
    {
        typeof(ShiftEntity).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(ShiftCashMovement).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
        typeof(ShiftDenominationCount).IsSubclassOf(typeof(TenantEntity)).Should().BeTrue();
    }

    [Fact]
    public void ShiftService_ExtendsBaseService()
    {
        var serviceTypes = ShiftAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Service"))
            .ToList();

        serviceTypes.Should().NotBeEmpty(
            "Shift module must have at least one concrete service class");

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
    public void ShiftController_ExtendsBaseController()
    {
        var result = Types.InAssembly(ShiftAssembly)
            .That().HaveNameEndingWith("Controller")
            .Should().Inherit(typeof(ErpSaas.Shared.Controllers.BaseController))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    // ── Enum-as-string enforcement (CLAUDE.md §3.9) ───────────────────────────

    [Fact]
    public void ShiftStatus_IsStoredAsString()
    {
        var stubCtx = new StubTenantContext();
        var ai = new AuditSaveChangesInterceptor(stubCtx);
        var ti = new TenantSaveChangesInterceptor(stubCtx);

        var opts = new DbContextOptionsBuilder<ShiftAwareTestDbContext>()
            .UseInMemoryDatabase("arch-shift-enum")
            .Options;

        using var db = new ShiftAwareTestDbContext(opts, stubCtx, ai, ti);
        var model = db.Model;

        var shiftEntity = model.FindEntityType(typeof(ShiftEntity));
        shiftEntity.Should().NotBeNull();

        var statusProp = shiftEntity!.FindProperty(nameof(ShiftEntity.Status));
        statusProp.Should().NotBeNull();
        statusProp!.GetValueConverter()?.ProviderClrType.Should().Be(typeof(string),
            "ShiftStatus must use HasConversion<string>() per CLAUDE.md §3.9");
    }

    // ── Namespace discipline ──────────────────────────────────────────────────

    [Fact]
    public void ShiftModule_DoesNotDependOnOtherBusinessModules()
    {
        var otherModuleNamespaces = new[]
        {
            "ErpSaas.Modules.Crm",
            "ErpSaas.Modules.Inventory",
            "ErpSaas.Modules.Hr",
            "ErpSaas.Modules.Accounting",
            "ErpSaas.Modules.Billing",
            "ErpSaas.Modules.Wallet",
        };

        foreach (var ns in otherModuleNamespaces)
        {
            var result = Types.InAssembly(ShiftAssembly)
                .ShouldNot().HaveDependencyOn(ns)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"Shift must not depend on {ns}: " +
                $"{string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    // ── Required test classes (this class is one of them) ────────────────────

    [Fact]
    public void ShiftModule_HasAllSixRequiredTestClasses()
    {
        var repoRoot = GetRepoRoot();

        var required = new[]
        {
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Unit",        "Modules", "Shift", "ShiftServiceTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Shift", "ShiftControllerTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Shift", "ShiftTenantIsolationTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Shift", "ShiftSubscriptionGateTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Shift", "ShiftAuditTrailTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Arch",        "Modules", "ShiftArchTests.cs"),
        };

        var missing = required.Where(f => !File.Exists(f)).ToList();
        missing.Should().BeEmpty(
            $"the following required Shift test files are missing: {string.Join(", ", missing)}");
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

    /// <summary>
    /// A TenantDbContext subclass that also applies Shift entity configurations,
    /// used exclusively in architecture tests.
    /// </summary>
    private sealed class ShiftAwareTestDbContext(
        DbContextOptions<ShiftAwareTestDbContext> _,
        ITenantContext tenantContext,
        AuditSaveChangesInterceptor auditInterceptor,
        TenantSaveChangesInterceptor tenantInterceptor)
        : TenantDbContext(
            new DbContextOptionsBuilder<TenantDbContext>()
                .UseInMemoryDatabase("arch-shift-inner")
                .Options,
            tenantContext,
            auditInterceptor,
            tenantInterceptor,
            [])
    {
        public DbSet<ShiftEntity> Shifts => Set<ShiftEntity>();
        public DbSet<ShiftCashMovement> CashMovements => Set<ShiftCashMovement>();
        public DbSet<ShiftDenominationCount> DenominationCounts => Set<ShiftDenominationCount>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            new ShiftModelConfigurator().Configure(modelBuilder);
        }
    }
}
