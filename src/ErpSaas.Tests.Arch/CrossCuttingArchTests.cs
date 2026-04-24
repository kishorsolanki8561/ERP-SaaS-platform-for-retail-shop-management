using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Tests.Arch;

[Trait("Category", "Architecture")]
public class CrossCuttingArchTests
{
    [Fact]
    public void EveryTenantEntityHasGlobalQueryFilter()
    {
        // Build an in-memory TenantDbContext with a stub ITenantContext and verify
        // every TenantEntity-derived entity type has a query filter configured.
        var stubCtx = new StubTenantContext();
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase("arch-test-tenant")
            .Options;

        // Interceptors are not needed here — just checking model configuration.
        var auditInterceptor = new ErpSaas.Infrastructure.Data.Interceptors.AuditSaveChangesInterceptor(stubCtx);
        var tenantInterceptor = new ErpSaas.Infrastructure.Data.Interceptors.TenantSaveChangesInterceptor(stubCtx);
        using var db = new TenantDbContext(opts, stubCtx, auditInterceptor, tenantInterceptor, []);

        var entityTypes = db.Model.GetEntityTypes()
            .Where(et => typeof(TenantEntity).IsAssignableFrom(et.ClrType))
            .ToList();

        entityTypes.Should().NotBeEmpty("TenantDbContext must have at least one TenantEntity");

        foreach (var entityType in entityTypes)
        {
            entityType.GetQueryFilter()
                .Should().NotBeNull(
                    $"{entityType.ClrType.Name} is a TenantEntity but has no global query filter");
        }
    }

    [Fact]
    public void EveryEntityInTenantDbContext_HasSchemaDeclared()
    {
        AssertAllEntitiesHaveSchema<TenantDbContext>(
            "arch-test-schema-tenant",
            opts =>
            {
                var ctx = new StubTenantContext();
                var ai = new ErpSaas.Infrastructure.Data.Interceptors.AuditSaveChangesInterceptor(ctx);
                var ti = new ErpSaas.Infrastructure.Data.Interceptors.TenantSaveChangesInterceptor(ctx);
                return new TenantDbContext(opts, ctx, ai, ti, []);
            });
    }

    [Fact]
    public void EveryEntityInPlatformDbContext_HasSchemaDeclared()
    {
        AssertAllEntitiesHaveSchema<PlatformDbContext>(
            "arch-test-schema-platform",
            opts =>
            {
                var ctx = new StubTenantContext();
                var ai = new ErpSaas.Infrastructure.Data.Interceptors.AuditSaveChangesInterceptor(ctx);
                return new PlatformDbContext(opts, ai);
            });
    }

    [Fact]
    public void EveryEntityInLogDbContext_HasSchemaDeclared()
    {
        AssertAllEntitiesHaveSchema<LogDbContext>(
            "arch-test-schema-log",
            opts => new LogDbContext(opts));
    }

    private static void AssertAllEntitiesHaveSchema<TContext>(
        string dbName,
        Func<DbContextOptions<TContext>, TContext> factory)
        where TContext : DbContext
    {
        var opts = new DbContextOptionsBuilder<TContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        using var db = factory(opts);

        var missingSchema = db.Model.GetEntityTypes()
            .Where(et => et.GetSchema() is null || et.GetSchema() == "dbo")
            .Select(et => et.ClrType.Name)
            .ToList();

        missingSchema.Should().BeEmpty(
            $"these entity types in {typeof(TContext).Name} have no schema (or default 'dbo') declared: {string.Join(", ", missingSchema)}");
    }

    private sealed class StubTenantContext : ITenantContext
    {
        public long ShopId => 1;
        public long CurrentUserId => 1;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
