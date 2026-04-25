#pragma warning disable CS9107
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Inventory.Entities;
using ErpSaas.Modules.Inventory.Enums;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Inventory.Services;

public sealed class InventoryService(
    TenantDbContext db,
    IErrorLogger errorLogger)
    : BaseService<TenantDbContext>(db, errorLogger), IInventoryService
{
    // ── DbSet accessors (named DbSets are registered in TenantDbContext separately) ─
    private DbSet<Product>       Products       => db.Set<Product>();
    private DbSet<ProductUnit>   ProductUnits   => db.Set<ProductUnit>();
    private DbSet<Warehouse>     Warehouses     => db.Set<Warehouse>();
    private DbSet<StockMovement> StockMovements => db.Set<StockMovement>();

    // ── Products ──────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<ProductListDto>> ListProductsAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
        => Products
            .Where(p => !p.IsDeleted
                && (search == null || p.Name.Contains(search) || p.ProductCode.Contains(search)))
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListDto(
                p.Id, p.ProductCode, p.Name,
                p.CategoryCode, p.BaseUnitCode, p.SalePrice, p.IsActive))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<ProductListDto>)t.Result, ct);

    public Task<ProductDetailDto?> GetProductAsync(long id, CancellationToken ct = default)
        => Products
            .Where(p => p.Id == id && !p.IsDeleted)
            .Select(p => (ProductDetailDto?)new ProductDetailDto(
                p.Id, p.ProductCode, p.Name, p.Description,
                p.CategoryCode, p.HsnSacCode, p.GstRate, p.BaseUnitCode,
                p.SalePrice, p.PurchasePrice, p.MrpPrice, p.MinStockLevel,
                p.IsActive, p.BarcodeEan))
            .FirstOrDefaultAsync(ct);

    public async Task<Result<long>> CreateProductAsync(
        CreateProductDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Inventory.CreateProduct", async () =>
        {
            var code = await GenerateProductCodeAsync(ct);
            var product = new Product
            {
                ProductCode = code,
                Name = dto.Name,
                CategoryCode = dto.CategoryCode,
                HsnSacCode = dto.HsnSacCode,
                GstRate = dto.GstRate,
                BaseUnitCode = dto.BaseUnitCode,
                SalePrice = dto.SalePrice,
                PurchasePrice = dto.PurchasePrice,
                MrpPrice = dto.MrpPrice,
                MinStockLevel = dto.MinStockLevel,
                BarcodeEan = dto.BarcodeEan,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            Products.Add(product);

            // SaveChanges first so product.Id is populated before ProductUnit references it.
            await db.SaveChangesAsync(ct);

            // Seed the default base unit row.
            ProductUnits.Add(new ProductUnit
            {
                ProductId = product.Id,
                UnitCode = dto.BaseUnitCode,
                UnitLabel = dto.BaseUnitCode,
                ConversionFactor = 1m,
                IsBaseUnit = true,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(ct);

            return Result<long>.Success(product.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> UpdateProductAsync(
        long id, UpdateProductDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Inventory.UpdateProduct", async () =>
        {
            var entity = await Products
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
            if (entity is null)
                return Result<bool>.NotFound(Errors.Inventory.ProductConflict(id));

            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.CategoryCode = dto.CategoryCode;
            entity.GstRate = dto.GstRate;
            entity.SalePrice = dto.SalePrice;
            entity.PurchasePrice = dto.PurchasePrice;
            entity.MrpPrice = dto.MrpPrice;
            entity.MinStockLevel = dto.MinStockLevel;

            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> DeactivateProductAsync(
        long id, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Inventory.DeactivateProduct", async () =>
        {
            var entity = await Products
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
            if (entity is null)
                return Result<bool>.NotFound(Errors.Inventory.ProductConflict(id));

            entity.IsActive = false;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    // ── Warehouses ────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<WarehouseDto>> ListWarehousesAsync(CancellationToken ct = default)
        => Warehouses
            .Where(w => w.IsActive && !w.IsDeleted)
            .OrderBy(w => w.Name)
            .Select(w => new WarehouseDto(w.Id, w.Code, w.Name, w.IsDefault, w.IsActive))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<WarehouseDto>)t.Result, ct);

    public async Task<Result<long>> CreateWarehouseAsync(
        string code, string name, bool isDefault, CancellationToken ct = default)
        => await ExecuteAsync<long>("Inventory.CreateWarehouse", async () =>
        {
            if (await Warehouses.AnyAsync(w => w.Code == code && !w.IsDeleted, ct))
                return Result<long>.Conflict(Errors.Inventory.WarehouseConflict(code));

            var entity = new Warehouse
            {
                Code = code,
                Name = name,
                IsDefault = isDefault,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            Warehouses.Add(entity);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);

    // ── Stock ─────────────────────────────────────────────────────────────────

    public async Task<decimal> GetStockLevelAsync(
        long productId, long warehouseId, CancellationToken ct = default)
    {
        var inbound = await StockMovements
            .Where(m => m.ProductId == productId
                && m.WarehouseId == warehouseId
                && (m.MovementType == StockMovementType.Purchase
                    || m.MovementType == StockMovementType.Adjustment
                    || m.MovementType == StockMovementType.Return
                    || m.MovementType == StockMovementType.Opening))
            .SumAsync(m => (decimal?)m.QuantityInBaseUnit, ct) ?? 0m;

        var outbound = await StockMovements
            .Where(m => m.ProductId == productId
                && m.WarehouseId == warehouseId
                && m.MovementType == StockMovementType.Sale)
            .SumAsync(m => (decimal?)m.QuantityInBaseUnit, ct) ?? 0m;

        return inbound - outbound;
    }

    public async Task<Result<bool>> AdjustStockAsync(
        AdjustStockDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Inventory.AdjustStock", async () =>
        {
            var unit = await ProductUnits
                .FirstOrDefaultAsync(u => u.Id == dto.ProductUnitId && !u.IsDeleted, ct);
            if (unit is null)
                return Result<bool>.NotFound(Errors.Inventory.UnitConflict(dto.ProductUnitId));

            var movement = new StockMovement
            {
                ProductId = dto.ProductId,
                WarehouseId = dto.WarehouseId,
                MovementType = dto.MovementType,
                ProductUnitId = dto.ProductUnitId,
                UnitCodeSnapshot = unit.UnitCode,
                ConversionFactorSnapshot = unit.ConversionFactor,
                QuantityInBilledUnit = dto.QuantityInBilledUnit,
                QuantityInBaseUnit = dto.QuantityInBilledUnit * unit.ConversionFactor,
                Remarks = dto.Remarks,
                MovedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
            };
            StockMovements.Add(movement);
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<string> GenerateProductCodeAsync(CancellationToken ct)
    {
        var count = await Products.CountAsync(ct);
        return $"PRD{(count + 1):D5}";
    }
}
