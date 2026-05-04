using ErpSaas.Modules.Marketplace.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Marketplace.Infrastructure;

public static class MarketplaceModelConfiguration
{
    public const string Schema = "marketplace";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<MarketplaceAccount>(e =>
        {
            e.ToTable("MarketplaceAccounts", schema: Schema);
            e.Property(x => x.MarketplaceCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.AccountName).HasMaxLength(200).IsRequired();
            e.Property(x => x.SellerId).HasMaxLength(100).IsRequired();
            e.Property(x => x.CredentialsJsonEncrypted).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.MarketplaceCode, x.SellerId }).IsUnique();
            e.HasMany(x => x.ProductMappings).WithOne(x => x.Account)
                .HasForeignKey(x => x.MarketplaceAccountId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Orders).WithOne(x => x.Account)
                .HasForeignKey(x => x.MarketplaceAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<MarketplaceProductMapping>(e =>
        {
            e.ToTable("MarketplaceProductMappings", schema: Schema);
            e.Property(x => x.MarketplaceSku).HasMaxLength(100).IsRequired();
            e.Property(x => x.MarketplaceListingId).HasMaxLength(100).IsRequired();
            e.Property(x => x.PriceOverride).HasPrecision(18, 2);
            e.HasIndex(x => new { x.ShopId, x.MarketplaceAccountId, x.MarketplaceSku }).IsUnique();
        });

        b.Entity<MarketplaceOrder>(e =>
        {
            e.ToTable("MarketplaceOrders", schema: Schema);
            e.Property(x => x.MarketplaceOrderId).HasMaxLength(100).IsRequired();
            e.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
            e.Property(x => x.CustomerPhone).HasMaxLength(20);
            e.Property(x => x.OrderTotal).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.ShippingAddressJson).IsRequired();
            e.Property(x => x.RawPayloadJson).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.MarketplaceAccountId, x.MarketplaceOrderId }).IsUnique();
        });
    }
}
