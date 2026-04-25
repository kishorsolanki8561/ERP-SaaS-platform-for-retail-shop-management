using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Shift.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Shift.Infrastructure;

public sealed class ShiftModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entities.Shift>(e =>
        {
            e.ToTable("Shift", schema: "shift");
            e.Property(x => x.CashierNameSnapshot).HasMaxLength(200).IsRequired();
            e.Property(x => x.CashierPhoneSnapshot).HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.ClosingNotes).HasMaxLength(1000);
            e.HasIndex(x => new { x.ShopId, x.CashierUserId, x.Status });
        });

        modelBuilder.Entity<ShiftCashMovement>(e =>
        {
            e.ToTable("ShiftCashMovement", schema: "shift");
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.ReasonCode).HasMaxLength(50);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasIndex(x => new { x.ShopId, x.ShiftId });
        });

        modelBuilder.Entity<ShiftDenominationCount>(e =>
        {
            e.ToTable("ShiftDenominationCount", schema: "shift");
            e.Property(x => x.Phase).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.ShiftId, x.Phase });
        });
    }
}
