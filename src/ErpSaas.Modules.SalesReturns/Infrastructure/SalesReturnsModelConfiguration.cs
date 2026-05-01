using ErpSaas.Modules.SalesReturns.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.SalesReturns.Infrastructure;

public static class SalesReturnsModelConfiguration
{
    public const string Schema = "sales";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<SalesReturn>(e =>
        {
            e.ToTable("SalesReturn", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ReturnNumber).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.ReturnNumber }).IsUnique();
            e.Property(x => x.InvoiceNumberSnapshot).HasMaxLength(50).IsRequired();
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.RefundMethod).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.TotalRefundAmount).HasPrecision(18, 2);
            e.Property(x => x.Reason).HasMaxLength(2000);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.ShopId, x.InvoiceId });
            e.HasMany(x => x.Lines)
                .WithOne(l => l.SalesReturn)
                .HasForeignKey(l => l.SalesReturnId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SalesReturnLine>(e =>
        {
            e.ToTable("SalesReturnLine", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(500).IsRequired();
            e.Property(x => x.ProductCodeSnapshot).HasMaxLength(100).IsRequired();
            e.Property(x => x.UnitCodeSnapshot).HasMaxLength(20).IsRequired();
            e.Property(x => x.ConversionFactorSnapshot).HasPrecision(18, 6);
            e.Property(x => x.QuantityInBilledUnit).HasPrecision(18, 4);
            e.Property(x => x.QuantityInBaseUnit).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.TaxableAmount).HasPrecision(18, 2);
            e.Property(x => x.GstRate).HasPrecision(5, 2);
            e.Property(x => x.CgstAmount).HasPrecision(18, 2);
            e.Property(x => x.SgstAmount).HasPrecision(18, 2);
            e.Property(x => x.IgstAmount).HasPrecision(18, 2);
            e.Property(x => x.LineTotal).HasPrecision(18, 2);
        });

        b.Entity<CreditNote>(e =>
        {
            e.ToTable("CreditNote", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.CreditNoteNumber).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.CreditNoteNumber }).IsUnique();
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.OriginalInvoiceNumberSnapshot).HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.UsedAmount).HasPrecision(18, 2);
            e.Property(x => x.RemainingAmount).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.ShopId, x.CustomerId });
        });
    }
}
