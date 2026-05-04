using ErpSaas.Modules.CustomerPortal.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.CustomerPortal.Infrastructure;

public static class CustomerPortalModelConfiguration
{
    public const string Schema = "portal";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<OnlineOrder>(e =>
        {
            e.ToTable("OnlineOrder", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.OrderNumber }).IsUnique();
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(200).IsRequired();
            e.Property(x => x.CustomerPhoneSnapshot).HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.RejectionReason).HasMaxLength(500);
            e.Property(x => x.SubTotal).HasPrecision(18, 4);
            e.Property(x => x.DiscountApplied).HasPrecision(18, 4);
            e.Property(x => x.ShippingCost).HasPrecision(18, 4);
            e.Property(x => x.GrandTotal).HasPrecision(18, 4);
            e.Property(x => x.DeliveryPreference).HasMaxLength(30).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasMany(x => x.Lines).WithOne(l => l.Order).HasForeignKey(l => l.OrderId);
            e.HasIndex(x => new { x.ShopId, x.PlatformCustomerId });
            e.HasIndex(x => new { x.ShopId, x.Status });
        });

        b.Entity<OnlineOrderLine>(e =>
        {
            e.ToTable("OnlineOrderLine", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.UnitCodeSnapshot).HasMaxLength(20).IsRequired();
            e.Property(x => x.ConversionFactorSnapshot).HasPrecision(18, 6);
            e.Property(x => x.QuantityInBilledUnit).HasPrecision(18, 4);
            e.Property(x => x.QuantityInBaseUnit).HasPrecision(18, 4);
            e.Property(x => x.UnitPriceSnapshot).HasPrecision(18, 4);
            e.Property(x => x.DiscountSnapshot).HasPrecision(18, 4);
            e.Property(x => x.GstRateSnapshot).HasPrecision(5, 2);
            e.Property(x => x.TaxableAmount).HasPrecision(18, 4);
            e.Property(x => x.GstAmount).HasPrecision(18, 4);
            e.Property(x => x.LineTotal).HasPrecision(18, 4);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<CustomerInquiry>(e =>
        {
            e.ToTable("CustomerInquiry", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.InquiryNumber).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.InquiryNumber }).IsUnique();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(300).IsRequired();
            e.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasMany(x => x.Messages).WithOne(m => m.Inquiry).HasForeignKey(m => m.InquiryId);
            e.HasIndex(x => new { x.ShopId, x.PlatformCustomerId });
        });

        b.Entity<CustomerInquiryMessage>(e =>
        {
            e.ToTable("CustomerInquiryMessage", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            e.Property(x => x.AttachmentFileIdsCsv).HasMaxLength(500);
            e.Property(x => x.RowVersion).IsRowVersion();
        });
    }
}
