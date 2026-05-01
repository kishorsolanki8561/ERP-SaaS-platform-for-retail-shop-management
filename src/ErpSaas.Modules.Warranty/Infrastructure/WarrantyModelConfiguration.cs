using ErpSaas.Modules.Warranty.Entities;
using ErpSaas.Modules.Warranty.Enums;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Warranty.Infrastructure;

public static class WarrantyModelConfiguration
{
    public const string Schema = "warranty";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<WarrantyRegistration>(e =>
        {
            e.ToTable("WarrantyRegistration", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.SerialNumber).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.SerialNumber }).IsUnique();
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(500).IsRequired();
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.StatusCode).HasMaxLength(20).IsRequired();
            e.Property(x => x.TermsSnapshot).HasMaxLength(4000);
            e.HasIndex(x => new { x.ShopId, x.CustomerId });
            e.HasIndex(x => new { x.ShopId, x.ProductId });
            e.HasIndex(x => new { x.ShopId, x.WarrantyEndDate });
            e.HasMany(x => x.Claims)
                .WithOne(c => c.Registration)
                .HasForeignKey(c => c.WarrantyRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<WarrantyClaim>(e =>
        {
            e.ToTable("WarrantyClaim", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ClaimNumber).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.ClaimNumber }).IsUnique();
            e.Property(x => x.IssueDescription).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.ResolutionNotes).HasMaxLength(2000);
            e.Property(x => x.RepairCost).HasPrecision(18, 2);
            e.Property(x => x.AttachmentFileIds).HasMaxLength(1000);
            e.HasIndex(x => new { x.ShopId, x.WarrantyRegistrationId });
        });
    }
}
