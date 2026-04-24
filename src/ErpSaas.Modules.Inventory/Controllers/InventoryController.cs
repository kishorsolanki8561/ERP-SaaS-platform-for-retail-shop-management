using ErpSaas.Modules.Inventory.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Inventory.Controllers;

[Route("api/inventory")]
[Authorize]
public sealed class InventoryController(IInventoryService inventoryService) : BaseController
{
    // ── Products ──────────────────────────────────────────────────────────────

    [HttpGet("products")]
    [RequirePermission("Inventory.View")]
    public async Task<IActionResult> ListProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await inventoryService.ListProductsAsync(page, pageSize, search, ct);
        return new OkObjectResult(result);
    }

    [HttpGet("products/{id:long}")]
    [RequirePermission("Inventory.View")]
    public async Task<IActionResult> GetProduct(long id, CancellationToken ct)
    {
        var result = await inventoryService.GetProductAsync(id, ct);
        if (result is null) return NotFound();
        return new OkObjectResult(result);
    }

    [HttpPost("products")]
    [RequirePermission("Inventory.Manage")]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductDto dto, CancellationToken ct)
        => Ok(await inventoryService.CreateProductAsync(dto, ct));

    [HttpPut("products/{id:long}")]
    [RequirePermission("Inventory.Manage")]
    public async Task<IActionResult> UpdateProduct(
        long id, [FromBody] UpdateProductDto dto, CancellationToken ct)
        => Ok(await inventoryService.UpdateProductAsync(id, dto, ct));

    [HttpDelete("products/{id:long}")]
    [RequirePermission("Inventory.Manage")]
    public async Task<IActionResult> DeactivateProduct(long id, CancellationToken ct)
        => Ok(await inventoryService.DeactivateProductAsync(id, ct));

    // ── Warehouses ────────────────────────────────────────────────────────────

    [HttpGet("warehouses")]
    [RequirePermission("Inventory.View")]
    public async Task<IActionResult> ListWarehouses(CancellationToken ct)
    {
        var result = await inventoryService.ListWarehousesAsync(ct);
        return new OkObjectResult(result);
    }

    [HttpPost("warehouses")]
    [RequirePermission("Inventory.Manage")]
    public async Task<IActionResult> CreateWarehouse(
        [FromBody] CreateWarehouseRequest request, CancellationToken ct)
        => Ok(await inventoryService.CreateWarehouseAsync(
            request.Code, request.Name, request.IsDefault, ct));

    // ── Stock ─────────────────────────────────────────────────────────────────

    [HttpGet("stock/{productId:long}/{warehouseId:long}")]
    [RequirePermission("Inventory.View")]
    public async Task<IActionResult> GetStockLevel(
        long productId, long warehouseId, CancellationToken ct)
    {
        var level = await inventoryService.GetStockLevelAsync(productId, warehouseId, ct);
        return new OkObjectResult(new { productId, warehouseId, stockLevel = level });
    }

    [HttpPost("stock/adjust")]
    [RequirePermission("Inventory.Manage")]
    public async Task<IActionResult> AdjustStock(
        [FromBody] AdjustStockDto dto, CancellationToken ct)
        => Ok(await inventoryService.AdjustStockAsync(dto, ct));
}

public record CreateWarehouseRequest(string Code, string Name, bool IsDefault);
