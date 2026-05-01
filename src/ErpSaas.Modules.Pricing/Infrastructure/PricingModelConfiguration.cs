using ErpSaas.Modules.Pricing.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Pricing.Infrastructure;

public static class PricingModelConfiguration
{
    public const string Schema = "pricing";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<DiscountRule>(e =>
        {
            e.ToTable("DiscountRule", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.DiscountTypeCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.Scope).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.PercentValue).HasPrecision(5, 2);
            e.Property(x => x.FixedValue).HasPrecision(18, 2);
            e.Property(x => x.MinInvoiceAmount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.ShopId, x.IsActive, x.StartDate, x.EndDate });
        });

        b.Entity<ExtraChargeRule>(e =>
        {
            e.ToTable("ExtraChargeRule", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.Value).HasPrecision(18, 4);
            e.Property(x => x.GstRate).HasPrecision(5, 2);
            e.HasIndex(x => new { x.ShopId, x.IsActive });
        });

        b.Entity<Offer>(e =>
        {
            e.ToTable("Offer", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.Code }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.RulesJson).HasMaxLength(4000);
            e.HasIndex(x => new { x.ShopId, x.IsActive, x.StartDate, x.EndDate });
        });
    }
}
