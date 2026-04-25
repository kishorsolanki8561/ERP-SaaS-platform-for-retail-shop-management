using ErpSaas.Modules.Billing.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Billing.Controllers;

[Route("api/billing")]
[Authorize]
public sealed class BillingController(
    IBillingService billingService,
    IInvoicePdfGenerator pdfService,
    IShopInfoProvider shopInfoProvider,
    ITenantContext tenant) : BaseController
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

    [HttpGet("invoices/{id:long}/pdf")]
    [RequirePermission("Billing.View")]
    public async Task<IActionResult> GetInvoicePdf(
        long id,
        [FromQuery] PdfFormat format = PdfFormat.A4,
        CancellationToken ct = default)
    {
        var invoice = await billingService.GetInvoiceAsync(id, ct);
        if (invoice is null) return NotFound();

        var shop = await shopInfoProvider.GetAsync(tenant.ShopId, ct);
        var bytes = pdfService.Generate(invoice, shop, format);
        var filename = $"{invoice.InvoiceNumber}_{format}.pdf";
        return File(bytes, "application/pdf", filename);
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

    [HttpPost("invoices/{id:long}/payment-terms")]
    [RequirePermission("Billing.Edit")]
    public async Task<IActionResult> SetPaymentTerms(
        long id,
        [FromBody] SetPaymentTermsDto dto,
        CancellationToken ct = default)
        => Ok(await billingService.SetPaymentTermsAsync(id, dto, ct));

    [HttpPost("invoices/{id:long}/pay")]
    [RequirePermission("Billing.Edit")]
    public async Task<IActionResult> PayInvoice(
        long id,
        [FromBody] PayInvoiceDto dto,
        CancellationToken ct = default)
        => Ok(await billingService.PayInvoiceAsync(id, dto, ct));

    [HttpPost("invoices/{id:long}/cancel")]
    [RequirePermission("Billing.Cancel")]
    public async Task<IActionResult> CancelInvoice(
        long id,
        [FromBody] CancelInvoiceRequest request,
        CancellationToken ct = default)
        => Ok(await billingService.CancelInvoiceAsync(id, request.Reason, ct));
}

public record CancelInvoiceRequest(string Reason);
