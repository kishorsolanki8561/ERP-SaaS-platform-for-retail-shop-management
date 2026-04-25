using ErpSaas.Modules.Inventory.Entities;
using ErpSaas.Modules.Inventory.Enums;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Inventory.Infrastructure;

public static class InventoryModelConfiguration
{
    public static void Configure(ModelBuilder b)
    {
        b.Entity<Product>(e =>
        {
            e.ToTable("Product", schema: "inventory");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.ProductCode }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(500).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.CategoryCode).HasMaxLength(100).IsRequired();
            e.Property(x => x.HsnSacCode).HasMaxLength(8);
            e.Property(x => x.GstRate).HasPrecision(5, 2);
            e.Property(x => x.BaseUnitCode).HasMaxLength(20).IsRequired();
            e.Property(x => x.SalePrice).HasPrecision(18, 4);
            e.Property(x => x.PurchasePrice).HasPrecision(18, 4);
            e.Property(x => x.MrpPrice).HasPrecision(18, 4);
            e.Property(x => x.MinStockLevel).HasPrecision(18, 4);
            e.Property(x => x.BarcodeEan).HasMaxLength(50);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
        });

        b.Entity<ProductUnit>(e =>
        {
            e.ToTable("ProductUnit", schema: "inventory");
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitCode).HasMaxLength(20).IsRequired();
            e.Property(x => x.UnitLabel).HasMaxLength(50).IsRequired();
            e.Property(x => x.ConversionFactor).HasPrecision(18, 6);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Warehouse>(e =>
        {
            e.ToTable("Warehouse", schema: "inventory");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.Code }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
        });

        b.Entity<StockMovement>(e =>
        {
            e.ToTable("StockMovement", schema: "inventory");
            e.HasKey(x => x.Id);
            // Enum stored as string for readability and safe migrations
            e.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.ReferenceType).HasMaxLength(100);
            e.Property(x => x.UnitCodeSnapshot).HasMaxLength(20).IsRequired();
            e.Property(x => x.ConversionFactorSnapshot).HasPrecision(18, 6);
            e.Property(x => x.QuantityInBilledUnit).HasPrecision(18, 4);
            e.Property(x => x.QuantityInBaseUnit).HasPrecision(18, 4);
            e.Property(x => x.Remarks).HasMaxLength(500);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
        });
    }
}
