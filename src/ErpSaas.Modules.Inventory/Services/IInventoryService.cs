using ErpSaas.Modules.Inventory.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Inventory.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record ProductListDto(
    long Id,
    string ProductCode,
    string Name,
    string CategoryCode,
    string BaseUnitCode,
    decimal SalePrice,
    bool IsActive);

public record ProductDetailDto(
    long Id,
    string ProductCode,
    string Name,
    string? Description,
    string CategoryCode,
    string? HsnSacCode,
    decimal GstRate,
    string BaseUnitCode,
    decimal SalePrice,
    decimal PurchasePrice,
    decimal? MrpPrice,
    decimal MinStockLevel,
    bool IsActive,
    string? BarcodeEan);

public record CreateProductDto(
    string Name,
    string CategoryCode,
    string? HsnSacCode,
    decimal GstRate,
    string BaseUnitCode,
    decimal SalePrice,
    decimal PurchasePrice,
    decimal? MrpPrice,
    decimal MinStockLevel,
    string? BarcodeEan);

public record UpdateProductDto(
    string Name,
    string? Description,
    string CategoryCode,
    decimal GstRate,
    decimal SalePrice,
    decimal PurchasePrice,
    decimal? MrpPrice,
    decimal MinStockLevel);

public record WarehouseDto(
    long Id,
    string Code,
    string Name,
    bool IsDefault,
    bool IsActive);

public record AdjustStockDto(
    long ProductId,
    long WarehouseId,
    long ProductUnitId,
    decimal QuantityInBilledUnit,
    StockMovementType MovementType,
    string? Remarks);

// ── Service interface ─────────────────────────────────────────────────────────

public interface IInventoryService
{
    Task<PagedResult<ProductListDto>> ListProductsAsync(
        int page, int pageSize, string? search, CancellationToken ct = default);

    Task<ProductDetailDto?> GetProductAsync(long id, CancellationToken ct = default);

    Task<Result<long>> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default);

    Task<Result<bool>> UpdateProductAsync(long id, UpdateProductDto dto, CancellationToken ct = default);

    Task<Result<bool>> DeactivateProductAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<WarehouseDto>> ListWarehousesAsync(CancellationToken ct = default);

    Task<Result<long>> CreateWarehouseAsync(
        string code, string name, bool isDefault, CancellationToken ct = default);

    Task<decimal> GetStockLevelAsync(long productId, long warehouseId, CancellationToken ct = default);

    Task<Result<bool>> AdjustStockAsync(AdjustStockDto dto, CancellationToken ct = default);
}
