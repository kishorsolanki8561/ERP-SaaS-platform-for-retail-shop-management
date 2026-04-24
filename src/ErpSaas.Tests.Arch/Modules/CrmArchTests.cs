#pragma warning disable CS9113 // Primary constructor parameter is unread (arch-test helper only)
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Crm.Configuration;
using ErpSaas.Modules.Crm.Entities;
using ErpSaas.Shared.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class CrmArchTests
{
    /// <summary>
    /// Verifies that every CRM entity gets the "crm" schema when the module
    /// configurations are applied to a TenantDbContext.
    /// </summary>
    [Fact]
    public void CrmEntities_HaveCrmSchema()
    {
        var ctx = new StubCtx();
        var ai = new AuditSaveChangesInterceptor(ctx);
        var ti = new TenantSaveChangesInterceptor(ctx);

        var opts = new DbContextOptionsBuilder<CrmAwareTestDbContext>()
            .UseInMemoryDatabase("arch-crm")
            .Options;

        using var db = new CrmAwareTestDbContext(opts, ctx, ai, ti);

        var crmEntities = db.Model.GetEntityTypes()
            .Where(e => e.ClrType.Namespace?.Contains("Modules.Crm") == true)
            .ToList();

        crmEntities.Should().NotBeEmpty("CRM entity configurations must be registered");

        foreach (var e in crmEntities)
        {
            e.GetSchema().Should().Be("crm",
                $"{e.ClrType.Name} must be in schema 'crm'");
        }
    }

    /// <summary>
    /// Verifies that every CRM entity that extends TenantEntity has a global query filter.
    /// </summary>
    [Fact]
    public void CrmTenantEntities_HaveGlobalQueryFilter()
    {
        var ctx = new StubCtx();
        var ai = new AuditSaveChangesInterceptor(ctx);
        var ti = new TenantSaveChangesInterceptor(ctx);

        var opts = new DbContextOptionsBuilder<CrmAwareTestDbContext>()
            .UseInMemoryDatabase("arch-crm-filter")
            .Options;

        using var db = new CrmAwareTestDbContext(opts, ctx, ai, ti);

        var tenantCrmEntities = db.Model.GetEntityTypes()
            .Where(e =>
                e.ClrType.Namespace?.Contains("Modules.Crm") == true &&
                typeof(TenantEntity).IsAssignableFrom(e.ClrType))
            .ToList();

        tenantCrmEntities.Should().NotBeEmpty();

        foreach (var e in tenantCrmEntities)
        {
            e.GetQueryFilter().Should().NotBeNull(
                $"{e.ClrType.Name} is a TenantEntity but has no global query filter");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class StubCtx : ITenantContext
    {
        public long ShopId => 1;
        public long CurrentUserId => 1;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }

    /// <summary>
    /// A TenantDbContext subclass that also applies CRM entity configurations,
    /// used exclusively in architecture tests.
    /// </summary>
    private sealed class CrmAwareTestDbContext(
        DbContextOptions<CrmAwareTestDbContext> _,
        ITenantContext tenantContext,
        AuditSaveChangesInterceptor auditInterceptor,
        TenantSaveChangesInterceptor tenantInterceptor)
        : TenantDbContext(
            new DbContextOptionsBuilder<TenantDbContext>()
                .UseInMemoryDatabase("arch-crm-inner")
                .Options,
            tenantContext,
            auditInterceptor,
            tenantInterceptor,
            [])
    {
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<CustomerGroup> CustomerGroups => Set<CustomerGroup>();
        public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new CustomerGroupEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new CustomerEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new CustomerAddressEntityTypeConfiguration());
        }
    }
}
