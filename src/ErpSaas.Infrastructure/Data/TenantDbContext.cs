using System.Linq.Expressions;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Infrastructure.Data.Entities.Sequence;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Data;

public class TenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : DbContext(options)
{
    public DbSet<DdlItemTenant> DdlItemsTenant => Set<DdlItemTenant>();
    public DbSet<SequenceDefinition> SequenceDefinitions => Set<SequenceDefinition>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.AddInterceptors(auditInterceptor, tenantInterceptor);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DdlItemTenant>(b =>
        {
            b.ToTable("DdlItemTenant", schema: "masters");
            b.HasKey(e => e.Id);
            b.Property(e => e.CatalogKey).HasMaxLength(100).IsRequired();
            b.Property(e => e.Code).HasMaxLength(100).IsRequired();
            b.Property(e => e.LabelOverride).HasMaxLength(200).IsRequired();
            b.Property(e => e.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<SequenceDefinition>(b =>
        {
            b.ToTable("SequenceDefinition", schema: "sequence");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(50).IsRequired();
            b.Property(e => e.Prefix).HasMaxLength(20);
            b.Property(e => e.Suffix).HasMaxLength(20);
            b.HasIndex(e => new { e.ShopId, e.Code }).IsUnique();
            b.Property(e => e.RowVersion).IsRowVersion();
        });

        ApplyGlobalTenantFilters(modelBuilder, tenantContext);
    }

    private static void ApplyGlobalTenantFilters(ModelBuilder modelBuilder, ITenantContext ctx)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(TenantEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var shopIdProp = Expression.Property(parameter, nameof(TenantEntity.ShopId));
            var ctxShopId = Expression.Property(Expression.Constant(ctx), nameof(ITenantContext.ShopId));
            var lambda = Expression.Lambda(Expression.Equal(shopIdProp, ctxShopId), parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
