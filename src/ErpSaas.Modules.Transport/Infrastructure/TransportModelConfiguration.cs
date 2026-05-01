using ErpSaas.Modules.Transport.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Transport.Infrastructure;

public static class TransportModelConfiguration
{
    public const string Schema = "transport";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<TransportProvider>(e =>
        {
            e.ToTable("TransportProvider", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ContactName).HasMaxLength(100);
            e.Property(x => x.ContactPhone).HasMaxLength(20);
            e.Property(x => x.GstNumber).HasMaxLength(15);
            e.HasIndex(x => new { x.ShopId, x.IsActive });
        });

        b.Entity<Vehicle>(e =>
        {
            e.ToTable("Vehicle", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.LicensePlate).HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.LicensePlate }).IsUnique();
            e.Property(x => x.Model).HasMaxLength(100).IsRequired();
            e.Property(x => x.MaxLoadKg).HasPrecision(10, 2);
            e.Property(x => x.DriverName).HasMaxLength(100);
            e.Property(x => x.DriverPhone).HasMaxLength(20);
            e.HasOne(x => x.TransportProvider).WithMany()
                .HasForeignKey(x => x.TransportProviderId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Delivery>(e =>
        {
            e.ToTable("Delivery", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DeliveryNumber).HasMaxLength(30).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.DeliveryNumber }).IsUnique();
            e.Property(x => x.ReferenceType).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.ReferenceNumberSnapshot).HasMaxLength(50).IsRequired();
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.DeliveryAddress).HasMaxLength(500).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasIndex(x => new { x.ShopId, x.Status, x.ScheduledDate });
            e.HasOne(x => x.Vehicle).WithMany(v => v.Deliveries)
                .HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.TransportProvider).WithMany()
                .HasForeignKey(x => x.TransportProviderId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<DeliveryLog>(e =>
        {
            e.ToTable("DeliveryLog", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasOne(x => x.Delivery).WithMany(d => d.Logs)
                .HasForeignKey(x => x.DeliveryId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
