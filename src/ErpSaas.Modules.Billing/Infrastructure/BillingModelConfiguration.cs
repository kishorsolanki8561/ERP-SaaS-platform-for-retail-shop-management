using ErpSaas.Modules.Billing.Entities;
using ErpSaas.Modules.Billing.Enums;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Billing.Infrastructure;

/// <summary>
/// EF Core model configuration for the Billing module.
/// Call <see cref="Configure"/> from <c>TenantDbContext.OnModelCreating</c>
/// when wiring the Billing module into the data layer.
/// </summary>
public static class BillingModelConfiguration
{
    public static void Configure(ModelBuilder b)
    {
        b.Entity<Invoice>(e =>
        {
            e.ToTable("Invoice", schema: "sales");
            e.HasKey(x => x.Id);
            e.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.InvoiceNumber }).IsUnique();
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.CustomerGstSnapshot).HasMaxLength(15);
            e.Property(x => x.BillingAddressSnapshot).HasMaxLength(1000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.TotalDiscount).HasPrecision(18, 2);
            e.Property(x => x.TotalTaxAmount).HasPrecision(18, 2);
            e.Property(x => x.RoundOff).HasPrecision(18, 2);
            e.Property(x => x.GrandTotal).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.ShiftId);
            e.Property(x => x.BranchId);
            e.HasIndex(x => new { x.ShopId, x.ShiftId });
            // IsRowVersion() is SQL Server-specific; use IsConcurrencyToken() so EF
            // can build the model on other providers (SQLite in unit tests).
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.Property(x => x.PaymentTerms).HasMaxLength(50);
            e.Property(x => x.PaidAmount).HasPrecision(18, 2);
            e.Property(x => x.OutstandingAmount).HasPrecision(18, 2);
            e.HasMany(x => x.Lines)
                .WithOne(l => l.Invoice)
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Payments)
                .WithOne(p => p.Invoice)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<InvoicePayment>(e =>
        {
            e.ToTable("InvoicePayment", schema: "sales");
            e.HasKey(x => x.Id);
            e.Property(x => x.Mode).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.ReferenceNumber).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasIndex(x => new { x.ShopId, x.InvoiceId });
        });

        b.Entity<InvoiceLine>(e =>
        {
            e.ToTable("InvoiceLine", schema: "sales");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(500).IsRequired();
            e.Property(x => x.ProductCodeSnapshot).HasMaxLength(100).IsRequired();
            e.Property(x => x.HsnSacCodeSnapshot).HasMaxLength(8);
            e.Property(x => x.UnitCodeSnapshot).HasMaxLength(20).IsRequired();
            e.Property(x => x.ConversionFactorSnapshot).HasPrecision(18, 6);
            e.Property(x => x.QuantityInBilledUnit).HasPrecision(18, 4);
            e.Property(x => x.QuantityInBaseUnit).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxableAmount).HasPrecision(18, 2);
            e.Property(x => x.GstRate).HasPrecision(5, 2);
            e.Property(x => x.CgstAmount).HasPrecision(18, 2);
            e.Property(x => x.SgstAmount).HasPrecision(18, 2);
            e.Property(x => x.IgstAmount).HasPrecision(18, 2);
            e.Property(x => x.LineTotal).HasPrecision(18, 2);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
        });
    }
}
