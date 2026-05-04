using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Sync.Entities;
using ErpSaas.Modules.Sync.Enums;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Sync.Infrastructure;

public sealed class SyncModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
        => SyncModelConfiguration.Configure(modelBuilder);
}

public static class SyncModelConfiguration
{
    public const string Schema = "sync";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<DeviceRegistration>(e =>
        {
            e.ToTable("DeviceRegistration", schema: Schema);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ShopId, x.DeviceId }).IsUnique();
            e.Property(x => x.DeviceId).HasMaxLength(200).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.PlatformInfo).HasMaxLength(500).IsRequired();
            e.Property(x => x.AppVersion).HasMaxLength(50).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
        });

        b.Entity<OfflineCommand>(e =>
        {
            e.ToTable("OfflineCommand", schema: Schema);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ShopId, x.ClientCommandId }).IsUnique();
            e.HasIndex(x => new { x.ShopId, x.Status });
            e.Property(x => x.DeviceId).HasMaxLength(200).IsRequired();
            e.Property(x => x.CommandType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.RejectionReason).HasMaxLength(1000);
            e.Property(x => x.WarningNote).HasMaxLength(1000);
        });

        b.Entity<InvoiceNumberAllocation>(e =>
        {
            e.ToTable("InvoiceNumberAllocation", schema: Schema);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ShopId, x.DeviceId, x.FinancialYear, x.Status });
            e.Property(x => x.DeviceId).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        });
    }
}
