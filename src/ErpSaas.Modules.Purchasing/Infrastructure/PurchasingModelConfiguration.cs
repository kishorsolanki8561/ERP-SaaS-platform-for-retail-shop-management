using ErpSaas.Modules.Purchasing.Entities;
using ErpSaas.Modules.Purchasing.Enums;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Purchasing.Infrastructure;

public static class PurchasingModelConfiguration
{
    public const string Schema = "purchasing";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<Supplier>(e =>
        {
            e.ToTable("Supplier", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(300).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50);
            e.HasIndex(x => new { x.ShopId, x.Code }).IsUnique().HasFilter("[Code] IS NOT NULL");
            e.Property(x => x.GstNumber).HasMaxLength(15);
            e.Property(x => x.PanNumber).HasMaxLength(10);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Address).HasMaxLength(1000);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.State).HasMaxLength(100);
            e.Property(x => x.Pincode).HasMaxLength(10);
            e.Property(x => x.OpeningBalance).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
        });

        b.Entity<PurchaseOrder>(e =>
        {
            e.ToTable("PurchaseOrder", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.PoNumber).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.PoNumber }).IsUnique();
            e.Property(x => x.SupplierNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.SupplierGstSnapshot).HasMaxLength(15);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.TotalTaxAmount).HasPrecision(18, 2);
            e.Property(x => x.GrandTotal).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.ShopId, x.SupplierId });
            e.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines)
                .WithOne(l => l.PurchaseOrder)
                .HasForeignKey(l => l.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Bills)
                .WithOne(b => b.PurchaseOrder)
                .HasForeignKey(b => b.PurchaseOrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<PurchaseOrderLine>(e =>
        {
            e.ToTable("PurchaseOrderLine", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(500).IsRequired();
            e.Property(x => x.ProductCodeSnapshot).HasMaxLength(100).IsRequired();
            e.Property(x => x.HsnSacCodeSnapshot).HasMaxLength(8);
            e.Property(x => x.UnitCodeSnapshot).HasMaxLength(20).IsRequired();
            e.Property(x => x.ConversionFactorSnapshot).HasPrecision(18, 6);
            e.Property(x => x.QuantityInBilledUnit).HasPrecision(18, 4);
            e.Property(x => x.QuantityInBaseUnit).HasPrecision(18, 4);
            e.Property(x => x.QuantityReceived).HasPrecision(18, 4);
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

        b.Entity<Bill>(e =>
        {
            e.ToTable("Bill", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.BillNumber).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.BillNumber }).IsUnique();
            e.Property(x => x.SupplierBillNumber).HasMaxLength(100);
            e.Property(x => x.SupplierNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.TotalTaxAmount).HasPrecision(18, 2);
            e.Property(x => x.GrandTotal).HasPrecision(18, 2);
            e.Property(x => x.PaidAmount).HasPrecision(18, 2);
            e.Property(x => x.OutstandingAmount).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.ShopId, x.SupplierId });
            e.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Payments)
                .WithOne(p => p.Bill)
                .HasForeignKey(p => p.BillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<BillPayment>(e =>
        {
            e.ToTable("BillPayment", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.PaymentModeCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.ReferenceNumber).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasIndex(x => new { x.ShopId, x.BillId });
            e.Property(x => x.RowVersion).IsConcurrencyToken();
        });

        b.Entity<DebitNote>(e =>
        {
            e.ToTable("DebitNote", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DebitNoteNumber).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.DebitNoteNumber }).IsUnique();
            e.Property(x => x.SupplierNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.UsedAmount).HasPrecision(18, 2);
            e.Property(x => x.RemainingAmount).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => new { x.ShopId, x.SupplierId });
            e.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<PurchaseReturn>(e =>
        {
            e.ToTable("PurchaseReturn", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ReturnNumber).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.ReturnNumber }).IsUnique();
            e.Property(x => x.SupplierNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.PoNumberSnapshot).HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.TotalTaxAmount).HasPrecision(18, 2);
            e.Property(x => x.GrandTotal).HasPrecision(18, 2);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => new { x.ShopId, x.SupplierId });
            e.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines)
                .WithOne(l => l.PurchaseReturn)
                .HasForeignKey(l => l.PurchaseReturnId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PurchaseReturnLine>(e =>
        {
            e.ToTable("PurchaseReturnLine", schema: Schema);
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
        });
    }
}
