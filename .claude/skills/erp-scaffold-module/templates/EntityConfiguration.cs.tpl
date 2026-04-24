using ErpSaas.Modules.{Module}.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpSaas.Modules.{Module}.EntityConfigurations;

public class {EntityName}Configuration : IEntityTypeConfiguration<{EntityName}>
{
    public void Configure(EntityTypeBuilder<{EntityName}> b)
    {
        // REQUIRED — arch test Schema_Ownership_Matches_Module enforces this
        b.ToTable("{EntityName}", schema: {Module}Module.Schema);

        // Primary key: inherited from BaseEntity (Id), nothing to do unless composite
        b.HasKey(x => x.Id);

        // ── String column sizing — NEVER leave strings as nvarchar(MAX) ───
        // b.Property(x => x.SomeCode).HasMaxLength(32).IsRequired();
        // b.Property(x => x.SomeName).HasMaxLength(200).IsRequired();
        // b.Property(x => x.SomeNotes).HasMaxLength(1000);

        // ── Decimals — specify precision for money ────────────────────────
        // b.Property(x => x.GrandTotal).HasPrecision(18, 2);
        // b.Property(x => x.ConversionFactor).HasPrecision(18, 6);   // unit conversions need 6 decimals

        // ── Indexes ────────────────────────────────────────────────────────
        // Unique natural key (always includes ShopId for tenant-scoped entities):
        // b.HasIndex(x => new { x.ShopId, x.InvoiceNumber }).IsUnique();

        // Query indexes for common filters (date range, customer lookup, status):
        // b.HasIndex(x => new { x.ShopId, x.InvoiceDate });
        // b.HasIndex(x => new { x.ShopId, x.CustomerId });
        // b.HasIndex(x => new { x.ShopId, x.StatusCode });

        // ── Foreign keys (prefer Restrict for financial integrity) ────────
        // b.HasOne<Customer>()
        //     .WithMany()
        //     .HasForeignKey(x => x.CustomerId)
        //     .OnDelete(DeleteBehavior.Restrict);

        // ── Concurrency token (already on BaseEntity as RowVersion) ───────

        // ── Global query filter: Tenant isolation is applied globally in
        //     TenantDbContext.OnModelCreating — do NOT apply filter here.
    }
}
