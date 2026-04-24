using ErpSaas.Modules.Billing.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using ErpSaas.Shared.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Billing.Controllers;

[Route("api/billing")]
[Authorize]
public sealed class BillingController(IBillingService billingService) : BaseController
{
    // ── Queries ───────────────────────────────────────────────────────────────

    [HttpGet("invoices")]
    [RequirePermission("Billing.View")]
    public async Task<IActionResult> ListInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Constants.Pagination.DefaultPageSize,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await billingService.ListInvoicesAsync(page, pageSize, search, ct);
        return new OkObjectResult(result);
    }

    [HttpGet("invoices/{id:long}")]
    [RequirePermission("Billing.View")]
    public async Task<IActionResult> GetInvoice(long id, CancellationToken ct = default)
    {
        var result = await billingService.GetInvoiceAsync(id, ct);
        return result is null ? NotFound() : new OkObjectResult(result);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [HttpPost("invoices")]
    [RequirePermission("Billing.Create")]
    public async Task<IActionResult> CreateDraftInvoice(
        [FromBody] CreateInvoiceDto dto,
        CancellationToken ct = default)
        => Ok(await billingService.CreateDraftInvoiceAsync(dto, ct));

    [HttpPost("invoices/{id:long}/lines")]
    [RequirePermission("Billing.Create")]
    public async Task<IActionResult> AddLine(
        long id,
        [FromBody] AddInvoiceLineDto dto,
        CancellationToken ct = default)
        => Ok(await billingService.AddLineAsync(id, dto, ct));

    [HttpPost("invoices/{id:long}/finalize")]
    [RequirePermission("Billing.Edit")]
    public async Task<IActionResult> FinalizeInvoice(long id, CancellationToken ct = default)
        => Ok(await billingService.FinalizeInvoiceAsync(id, ct));

    [HttpPost("invoices/{id:long}/cancel")]
    [RequirePermission("Billing.Cancel")]
    public async Task<IActionResult> CancelInvoice(
        long id,
        [FromBody] CancelInvoiceRequest request,
        CancellationToken ct = default)
        => Ok(await billingService.CancelInvoiceAsync(id, request.Reason, ct));
}

public record CancelInvoiceRequest(string Reason);
