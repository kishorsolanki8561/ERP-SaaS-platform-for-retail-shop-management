using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Data;

public class PlatformDbContext(
    DbContextOptions<PlatformDbContext> options,
    AuditSaveChangesInterceptor auditInterceptor)
    : DbContext(options)
{
    public DbSet<DdlCatalog> DdlCatalogs => Set<DdlCatalog>();
    public DbSet<DdlItem> DdlItems => Set<DdlItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.AddInterceptors(auditInterceptor);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DdlCatalog>(b =>
        {
            b.ToTable("DdlCatalog", schema: "masters");
            b.HasKey(e => e.Id);
            b.Property(e => e.Key).HasMaxLength(100).IsRequired();
            b.HasIndex(e => e.Key).IsUnique();
            b.HasMany(e => e.Items).WithOne(i => i.Catalog).HasForeignKey(i => i.CatalogId);
        });

        modelBuilder.Entity<DdlItem>(b =>
        {
            b.ToTable("DdlItem", schema: "masters");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(100).IsRequired();
            b.Property(e => e.Label).HasMaxLength(200).IsRequired();
            b.Property(e => e.ParentCode).HasMaxLength(100);
        });
    }
}
