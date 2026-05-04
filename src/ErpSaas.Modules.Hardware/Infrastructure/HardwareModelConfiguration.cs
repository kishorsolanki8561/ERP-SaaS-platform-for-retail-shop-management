using ErpSaas.Modules.Hardware.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Hardware.Infrastructure;

public static class HardwareModelConfiguration
{
    public const string Schema = "hardware";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<DeviceProfile>(e =>
        {
            e.ToTable("DeviceProfile", schema: Schema);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ShopId, x.DeviceId }).IsUnique();
            e.Property(x => x.DeviceId).HasMaxLength(200).IsRequired();
            e.Property(x => x.Class).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.VendorCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.ModelCode).HasMaxLength(100).IsRequired();
            e.Property(x => x.ConnectionJson).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Role).HasMaxLength(100).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
        });

        b.Entity<LabelTemplate>(e =>
        {
            e.ToTable("LabelTemplate", schema: Schema);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ShopId, x.Name });
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.LabelType).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.ZplTemplate).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
        });

        b.Entity<ReceiptTemplate>(e =>
        {
            e.ToTable("ReceiptTemplate", schema: Schema);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ShopId, x.Name });
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.TemplateType).HasMaxLength(50).IsRequired();
            e.Property(x => x.HeaderJson).IsRequired();
            e.Property(x => x.FooterJson).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
        });
    }
}
