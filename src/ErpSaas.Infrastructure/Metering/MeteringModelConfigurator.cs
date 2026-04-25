using ErpSaas.Infrastructure.Data.Entities.Metering;
using ErpSaas.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Metering;

public sealed class MeteringModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UsageMeter>(e =>
        {
            e.ToTable("UsageMeter", schema: "metering");
            e.HasKey(x => x.Id);
            e.Property(x => x.MeterCode).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.MeterCode, x.PeriodStartUtc }).IsUnique();
            e.Property(x => x.OverageChargeRate).HasColumnType("decimal(18,4)");
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<UsageEvent>(e =>
        {
            e.ToTable("UsageEvent", schema: "metering");
            e.HasKey(x => x.Id);
            e.Property(x => x.MeterCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.SourceEntityType).HasMaxLength(100);
            e.HasIndex(x => new { x.ShopId, x.MeterCode, x.OccurredAtUtc });
            e.Property(x => x.RowVersion).IsRowVersion();
        });
    }
}
