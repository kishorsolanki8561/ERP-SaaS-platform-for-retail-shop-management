using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Inventory.Entities;
using ErpSaas.Modules.Inventory.Seeds;
using ErpSaas.Modules.Inventory.Services;
using ErpSaas.Shared.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Inventory.Extensions;

public static class InventoryServiceExtensions
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddDataSeeder<InventorySystemSeeder>();
        services.AddSingleton(new ServiceDescriptorEntry(
            "Inventory",
            "Product catalogue and stock management",
            "1.0"));
        services.AddSingleton<IEntityModelConfigurator, InventoryModelConfigurator>();
        return services;
    }
}

internal sealed class InventoryModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder b)
    {
        b.Entity<Product>(e =>
        {
            e.ToTable("Product", schema: "inventory");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductCode).HasMaxLength(20).IsRequired();
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
            e.Property(x => x.BarcodeEan).HasMaxLength(20);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasMany(x => x.Units)
                .WithOne(u => u.Product)
                .HasForeignKey(u => u.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.StockMovements)
                .WithOne(m => m.Product)
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ProductUnit>(e =>
        {
            e.ToTable("ProductUnit", schema: "inventory");
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitCode).HasMaxLength(20).IsRequired();
            e.Property(x => x.UnitLabel).HasMaxLength(100).IsRequired();
            e.Property(x => x.ConversionFactor).HasPrecision(18, 6);
            e.HasIndex(x => new { x.ProductId, x.UnitCode }).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<Warehouse>(e =>
        {
            e.ToTable("Warehouse", schema: "inventory");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.Code }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<StockMovement>(e =>
        {
            e.ToTable("StockMovement", schema: "inventory");
            e.HasKey(x => x.Id);
            e.Property(x => x.MovementType).HasMaxLength(30).IsRequired();
            e.Property(x => x.ReferenceType).HasMaxLength(50);
            e.Property(x => x.UnitCodeSnapshot).HasMaxLength(20).IsRequired();
            e.Property(x => x.ConversionFactorSnapshot).HasPrecision(18, 6);
            e.Property(x => x.QuantityInBilledUnit).HasPrecision(18, 4);
            e.Property(x => x.QuantityInBaseUnit).HasPrecision(18, 4);
            e.Property(x => x.Remarks).HasMaxLength(500);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne(x => x.Warehouse)
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
