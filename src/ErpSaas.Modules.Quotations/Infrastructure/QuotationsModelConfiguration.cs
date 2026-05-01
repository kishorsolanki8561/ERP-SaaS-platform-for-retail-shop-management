using ErpSaas.Modules.Quotations.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Quotations.Infrastructure;

public static class QuotationsModelConfiguration
{
    public const string Schema = "sales";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<Quotation>(e =>
        {
            e.ToTable("Quotation", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.QuotationNumber).HasMaxLength(30).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.QuotationNumber }).IsUnique();
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.TotalDiscount).HasPrecision(18, 2);
            e.Property(x => x.TotalTax).HasPrecision(18, 2);
            e.Property(x => x.GrandTotal).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => new { x.ShopId, x.Status, x.ValidUntil });
        });

        b.Entity<QuotationLine>(e =>
        {
            e.ToTable("QuotationLine", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.UnitCodeSnapshot).HasMaxLength(20).IsRequired();
            e.Property(x => x.ConversionFactorSnapshot).HasPrecision(18, 6);
            e.Property(x => x.QuantityInBilledUnit).HasPrecision(18, 4);
            e.Property(x => x.QuantityInBaseUnit).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxableAmount).HasPrecision(18, 2);
            e.Property(x => x.GstRate).HasPrecision(5, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.LineTotal).HasPrecision(18, 2);
            e.HasOne(x => x.Quotation).WithMany(q => q.Lines)
                .HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SalesOrder>(e =>
        {
            e.ToTable("SalesOrder", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.SoNumber).HasMaxLength(30).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.SoNumber }).IsUnique();
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.TotalDiscount).HasPrecision(18, 2);
            e.Property(x => x.TotalTax).HasPrecision(18, 2);
            e.Property(x => x.GrandTotal).HasPrecision(18, 2);
            e.Property(x => x.ShippingAddress).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => new { x.ShopId, x.Status, x.OrderDate });
        });

        b.Entity<SalesOrderLine>(e =>
        {
            e.ToTable("SalesOrderLine", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.UnitCodeSnapshot).HasMaxLength(20).IsRequired();
            e.Property(x => x.ConversionFactorSnapshot).HasPrecision(18, 6);
            e.Property(x => x.QuantityInBilledUnit).HasPrecision(18, 4);
            e.Property(x => x.QuantityInBaseUnit).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxableAmount).HasPrecision(18, 2);
            e.Property(x => x.GstRate).HasPrecision(5, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.LineTotal).HasPrecision(18, 2);
            e.HasOne(x => x.SalesOrder).WithMany(s => s.Lines)
                .HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<DeliveryChallan>(e =>
        {
            e.ToTable("DeliveryChallan", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DcNumber).HasMaxLength(30).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.DcNumber }).IsUnique();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.DeliveryAddress).HasMaxLength(500);
            e.Property(x => x.TransporterName).HasMaxLength(200);
            e.Property(x => x.VehicleNumber).HasMaxLength(20);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => new { x.ShopId, x.Status });
            e.HasOne(x => x.SalesOrder).WithMany(s => s.DeliveryChallans)
                .HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<DeliveryChallanLine>(e =>
        {
            e.ToTable("DeliveryChallanLine", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.UnitCodeSnapshot).HasMaxLength(20).IsRequired();
            e.Property(x => x.ConversionFactorSnapshot).HasPrecision(18, 6);
            e.Property(x => x.QuantityInBilledUnit).HasPrecision(18, 4);
            e.Property(x => x.QuantityInBaseUnit).HasPrecision(18, 4);
            e.HasOne(x => x.DeliveryChallan).WithMany(d => d.Lines)
                .HasForeignKey(x => x.DeliveryChallanId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
