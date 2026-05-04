using ErpSaas.Modules.ServiceJobs.Entities;
using ErpSaas.Modules.ServiceJobs.Enums;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.ServiceJobs.Infrastructure;

public static class ServiceJobModelConfiguration
{
    public const string Schema = "service";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<ServiceJob>(e =>
        {
            e.ToTable("ServiceJob", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.JobNumber).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.JobNumber }).IsUnique();
            e.Property(x => x.ItemDescription).HasMaxLength(500).IsRequired();
            e.Property(x => x.SerialNumber).HasMaxLength(100);
            e.Property(x => x.ReportedIssue).HasMaxLength(2000).IsRequired();
            e.Property(x => x.DiagnosisNotes).HasMaxLength(4000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.EstimatedCost).HasPrecision(18, 2);
            e.Property(x => x.ActualPartsCost).HasPrecision(18, 2);
            e.Property(x => x.ActualLaborCost).HasPrecision(18, 2);
            e.Property(x => x.TotalCost).HasPrecision(18, 2);
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(300);
            e.Property(x => x.CustomerPhoneSnapshot).HasMaxLength(20);
            e.HasIndex(x => new { x.ShopId, x.CustomerId });
            e.HasIndex(x => new { x.ShopId, x.Status });
            e.HasMany(x => x.Parts).WithOne(p => p.Job)
                .HasForeignKey(p => p.ServiceJobId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.LaborEntries).WithOne(l => l.Job)
                .HasForeignKey(l => l.ServiceJobId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ServiceJobPart>(e =>
        {
            e.ToTable("ServiceJobPart", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.UnitCost).HasPrecision(18, 2);
            e.Property(x => x.LineCost).HasPrecision(18, 2);
            e.HasIndex(x => new { x.ShopId, x.ServiceJobId });
        });

        b.Entity<ServiceJobLabor>(e =>
        {
            e.ToTable("ServiceJobLabor", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.TechnicianNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.Hours).HasPrecision(10, 2);
            e.Property(x => x.HourlyRate).HasPrecision(18, 2);
            e.Property(x => x.LaborCost).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasIndex(x => new { x.ShopId, x.ServiceJobId });
        });
    }
}
