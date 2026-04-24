using System.Linq.Expressions;
using ErpSaas.Infrastructure.Data.Entities.Files;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Sequence;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Data;

public class TenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor,
    IEnumerable<IEntityModelConfigurator> modelConfigurators)
    : DbContext(options)
{
    public DbSet<DdlItemTenant> DdlItemsTenant => Set<DdlItemTenant>();
    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
    public DbSet<SequenceDefinition> SequenceDefinitions => Set<SequenceDefinition>();
    public DbSet<MenuItemTenantOverride> MenuItemTenantOverrides => Set<MenuItemTenantOverride>();

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

        modelBuilder.Entity<MenuItemTenantOverride>(e =>
        {
            e.ToTable("MenuItemTenantOverride", schema: "menu");
            e.HasKey(x => x.Id);
            e.Property(x => x.MenuItemCode).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.MenuItemCode }).IsUnique();
            e.Property(x => x.LabelOverride).HasMaxLength(200);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<UploadedFile>(e =>
        {
            e.ToTable("UploadedFile", schema: "files");
            e.HasKey(x => x.Id);
            e.Property(x => x.OriginalFileName).HasMaxLength(500).IsRequired();
            e.Property(x => x.StorageKey).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.StorageKey).IsUnique();
            e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Purpose).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(100);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        foreach (var configurator in modelConfigurators)
            configurator.Configure(modelBuilder);

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
