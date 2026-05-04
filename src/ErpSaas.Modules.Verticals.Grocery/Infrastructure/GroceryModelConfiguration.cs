using ErpSaas.Modules.Verticals.Grocery.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Verticals.Grocery.Infrastructure;

public static class GroceryModelConfiguration
{
    public const string Schema = "verticals_grocery";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<LoyaltyProgram>(e =>
        {
            e.ToTable("LoyaltyProgram", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.PointsPerRupee).HasPrecision(10, 4);
            e.Property(x => x.RupeeValuePerPoint).HasPrecision(10, 4);
            e.Property(x => x.MinimumRedemptionPoints).HasPrecision(18, 2);
            e.Property(x => x.MaxRedemptionPercentPerBill).HasPrecision(5, 2);
            e.HasIndex(x => x.ShopId).IsUnique();
        });

        b.Entity<LoyaltyTransaction>(e =>
        {
            e.ToTable("LoyaltyTransaction", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.TransactionType).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Points).HasPrecision(18, 4);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 4);
            e.Property(x => x.Reference).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasIndex(x => new { x.ShopId, x.CustomerId });
            e.HasIndex(x => new { x.ShopId, x.InvoiceId });
        });
    }
}
