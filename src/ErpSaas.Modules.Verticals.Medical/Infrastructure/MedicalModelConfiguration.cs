using ErpSaas.Modules.Verticals.Medical.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Verticals.Medical.Infrastructure;

public static class MedicalModelConfiguration
{
    public const string Schema = "verticals_medical";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<DrugBatch>(e =>
        {
            e.ToTable("DrugBatch", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.BatchNumber).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.ProductId, x.BatchNumber }).IsUnique();
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.GenericName).HasMaxLength(300);
            e.Property(x => x.Manufacturer).HasMaxLength(300);
            e.Property(x => x.Schedule).HasConversion<string>().HasMaxLength(10).IsRequired();
            e.Property(x => x.InitialQuantity).HasPrecision(18, 4);
            e.Property(x => x.CurrentQuantity).HasPrecision(18, 4);
            e.Property(x => x.PurchasePrice).HasPrecision(18, 2);
            e.Property(x => x.SellingPrice).HasPrecision(18, 2);
            e.Property(x => x.SupplierNameSnapshot).HasMaxLength(300);
            e.HasIndex(x => new { x.ShopId, x.ExpiryDate });
            e.HasIndex(x => new { x.ShopId, x.ProductId });
            e.Ignore(x => x.IsScheduleH);
            e.Ignore(x => x.IsNarcotic);
            e.HasMany(x => x.PrescriptionRecords).WithOne(r => r.Batch)
                .HasForeignKey(r => r.DrugBatchId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<PrescriptionRecord>(e =>
        {
            e.ToTable("PrescriptionRecord", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DoctorName).HasMaxLength(300).IsRequired();
            e.Property(x => x.DoctorRegistrationNumber).HasMaxLength(100);
            e.Property(x => x.QuantityDispensed).HasPrecision(18, 4);
            e.Property(x => x.FileId).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => new { x.ShopId, x.DrugBatchId });
            e.HasIndex(x => new { x.ShopId, x.InvoiceId });
        });
    }
}
