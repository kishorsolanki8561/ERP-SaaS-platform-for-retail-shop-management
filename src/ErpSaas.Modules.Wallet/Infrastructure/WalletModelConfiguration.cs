using ErpSaas.Modules.Wallet.Entities;
using ErpSaas.Modules.Wallet.Enums;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Wallet.Infrastructure;

public static class WalletModelConfiguration
{
    public static void Configure(ModelBuilder b)
    {
        b.Entity<WalletBalance>(e =>
        {
            e.ToTable("WalletBalance", schema: "wallet");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ShopId, x.CustomerId }).IsUnique();
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.Balance).HasPrecision(18, 2);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
        });

        b.Entity<WalletTransaction>(e =>
        {
            e.ToTable("WalletTransaction", schema: "wallet");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ShopId, x.CustomerId });
            e.HasIndex(x => new { x.ShopId, x.ReceiptNumber })
                .IsUnique()
                .HasFilter("[ReceiptNumber] IS NOT NULL");
            e.Property(x => x.CustomerNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.TransactionType).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.BalanceBefore).HasPrecision(18, 2);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            e.Property(x => x.ReferenceType).HasMaxLength(50).IsRequired();
            e.Property(x => x.ReferenceNumber).HasMaxLength(100);
            e.Property(x => x.ReceiptNumber).HasMaxLength(50);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
        });
    }
}
