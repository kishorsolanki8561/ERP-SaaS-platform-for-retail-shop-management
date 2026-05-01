using ErpSaas.Modules.Purchasing.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Purchasing.Controllers;

[Route("api/purchasing")]
[Authorize]
public sealed class PurchasingController(IPurchasingService purchasingService) : BaseController
{
    // ── Suppliers ─────────────────────────────────────────────────────────────

    [HttpGet("suppliers")]
    [RequirePermission("Purchasing.View")]
    public async Task<IActionResult> ListSuppliers(CancellationToken ct = default)
        => Ok(await purchasingService.ListSuppliersAsync(ct));

    [HttpPost("suppliers")]
    [RequirePermission("Purchasing.ManageSuppliers")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDto dto, CancellationToken ct = default)
        => Ok(await purchasingService.CreateSupplierAsync(dto, ct));

    [HttpPut("suppliers/{id:long}")]
    [RequirePermission("Purchasing.ManageSuppliers")]
    public async Task<IActionResult> UpdateSupplier(long id, [FromBody] UpdateSupplierDto dto, CancellationToken ct = default)
        => Ok(await purchasingService.UpdateSupplierAsync(id, dto, ct));

    [HttpDelete("suppliers/{id:long}")]
    [RequirePermission("Purchasing.ManageSuppliers")]
    public async Task<IActionResult> DeleteSupplier(long id, CancellationToken ct = default)
        => Ok(await purchasingService.DeleteSupplierAsync(id, ct));

    // ── Purchase Orders ───────────────────────────────────────────────────────

    [HttpPost("purchase-orders")]
    [RequirePermission("Purchasing.CreatePurchaseOrder")]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderDto dto, CancellationToken ct = default)
        => Ok(await purchasingService.CreatePurchaseOrderAsync(dto, ct));

    [HttpPost("purchase-orders/{id:long}/send")]
    [RequirePermission("Purchasing.CreatePurchaseOrder")]
    public async Task<IActionResult> SendPurchaseOrder(long id, CancellationToken ct = default)
        => Ok(await purchasingService.SendPurchaseOrderAsync(id, ct));

    [HttpPost("purchase-orders/receive")]
    [RequirePermission("Purchasing.ReceiveGoods")]
    public async Task<IActionResult> ReceivePurchaseOrder([FromBody] ReceivePoDto dto, CancellationToken ct = default)
        => Ok(await purchasingService.ReceivePurchaseOrderAsync(dto, ct));

    [HttpPost("purchase-orders/{id:long}/cancel")]
    [RequirePermission("Purchasing.CreatePurchaseOrder")]
    public async Task<IActionResult> CancelPurchaseOrder(long id, CancellationToken ct = default)
        => Ok(await purchasingService.CancelPurchaseOrderAsync(id, ct));

    // ── Bills ─────────────────────────────────────────────────────────────────

    [HttpPost("bills")]
    [RequirePermission("Purchasing.ManageBills")]
    public async Task<IActionResult> CreateBill([FromBody] CreateBillDto dto, CancellationToken ct = default)
        => Ok(await purchasingService.CreateBillAsync(dto, ct));

    [HttpPost("bills/{id:long}/approve")]
    [RequirePermission("Purchasing.ManageBills")]
    public async Task<IActionResult> ApproveBill(long id, CancellationToken ct = default)
        => Ok(await purchasingService.ApproveBillAsync(id, ct));

    [HttpPost("bills/pay")]
    [RequirePermission("Purchasing.ManageBills")]
    public async Task<IActionResult> PayBill([FromBody] PayBillDto dto, CancellationToken ct = default)
        => Ok(await purchasingService.PayBillAsync(dto, ct));

    [HttpPost("bills/{id:long}/cancel")]
    [RequirePermission("Purchasing.ManageBills")]
    public async Task<IActionResult> CancelBill(long id, CancellationToken ct = default)
        => Ok(await purchasingService.CancelBillAsync(id, ct));

    // ── Purchase Returns ──────────────────────────────────────────────────────

    [HttpPost("purchase-returns")]
    [RequirePermission("Purchasing.ManagePurchaseReturns")]
    public async Task<IActionResult> CreatePurchaseReturn([FromBody] CreatePurchaseReturnDto dto, CancellationToken ct = default)
        => Ok(await purchasingService.CreatePurchaseReturnAsync(dto, ct));

    [HttpPost("purchase-returns/{id:long}/approve")]
    [RequirePermission("Purchasing.ManagePurchaseReturns")]
    public async Task<IActionResult> ApprovePurchaseReturn(long id, CancellationToken ct = default)
        => Ok(await purchasingService.ApprovePurchaseReturnAsync(id, ct));

    [HttpPost("purchase-returns/{id:long}/cancel")]
    [RequirePermission("Purchasing.ManagePurchaseReturns")]
    public async Task<IActionResult> CancelPurchaseReturn(long id, CancellationToken ct = default)
        => Ok(await purchasingService.CancelPurchaseReturnAsync(id, ct));

    [HttpPost("debit-notes/issue")]
    [RequirePermission("Purchasing.ManagePurchaseReturns")]
    public async Task<IActionResult> IssueDebitNote([FromBody] IssueDebitNoteDto dto, CancellationToken ct = default)
        => Ok(await purchasingService.IssueDebitNoteAsync(dto, ct));
}
