using ErpSaas.Modules.Marketplace.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Marketplace.Controllers;

[Route("api/marketplace")]
[Authorize]
public sealed class MarketplaceController(
    IMarketplaceAccountService accountService,
    IMarketplaceOrderService orderService,
    IMarketplaceSyncService syncService) : BaseController
{
    // ── Accounts ──────────────────────────────────────────────────────────────

    [HttpGet("accounts")]
    [RequirePermission("Marketplace.View")]
    public async Task<IActionResult> ListAccounts(CancellationToken ct = default)
        => Ok(await accountService.ListAsync(ct));

    [HttpPost("accounts")]
    [RequirePermission("Marketplace.Manage")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateMarketplaceAccountDto dto, CancellationToken ct = default)
        => Ok(await accountService.CreateAsync(dto, ct));

    [HttpPatch("accounts/{id:long}")]
    [RequirePermission("Marketplace.Manage")]
    public async Task<IActionResult> UpdateAccount(long id, [FromBody] UpdateMarketplaceAccountDto dto, CancellationToken ct = default)
        => Ok(await accountService.UpdateAsync(id, dto, ct));

    [HttpPost("accounts/{id:long}/test-connection")]
    [RequirePermission("Marketplace.Manage")]
    public async Task<IActionResult> TestConnection(long id, CancellationToken ct = default)
        => Ok(await accountService.TestConnectionAsync(id, ct));

    // ── Product Mappings ──────────────────────────────────────────────────────

    [HttpGet("products")]
    [RequirePermission("Marketplace.View")]
    public async Task<IActionResult> ListProductMappings(CancellationToken ct = default)
        => Ok(await syncService.ListProductMappingsAsync(ct));

    [HttpPost("products/link")]
    [RequirePermission("Marketplace.Manage")]
    public async Task<IActionResult> LinkProduct([FromBody] LinkProductDto dto, CancellationToken ct = default)
        => Ok(await syncService.LinkProductAsync(dto, ct));

    // ── Sync ──────────────────────────────────────────────────────────────────

    [HttpPost("sync/inventory")]
    [RequirePermission("Marketplace.Sync")]
    public async Task<IActionResult> SyncInventory(CancellationToken ct = default)
        => Ok(await syncService.SyncInventoryAsync(ct));

    [HttpPost("sync/prices")]
    [RequirePermission("Marketplace.Sync")]
    public async Task<IActionResult> SyncPrices(CancellationToken ct = default)
        => Ok(await syncService.SyncPricesAsync(ct));

    [HttpPost("sync/orders")]
    [RequirePermission("Marketplace.Sync")]
    public async Task<IActionResult> SyncOrders(CancellationToken ct = default)
        => Ok(await syncService.SyncOrdersAsync(ct));

    // ── Orders ────────────────────────────────────────────────────────────────

    [HttpGet("orders")]
    [RequirePermission("Marketplace.View")]
    public async Task<IActionResult> ListOrders([FromQuery] MarketplaceOrderListRequest request, CancellationToken ct = default)
        => Ok(await orderService.ListAsync(request, ct));

    [HttpPost("orders/{id:long}/convert-to-invoice")]
    [RequirePermission("Marketplace.ConvertOrder")]
    public async Task<IActionResult> ConvertToInvoice(long id, CancellationToken ct = default)
        => Ok(await orderService.ConvertToInvoiceAsync(id, ct));
}
