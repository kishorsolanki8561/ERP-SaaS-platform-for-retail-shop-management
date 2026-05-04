using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Payment.Enums;
using ErpSaas.Modules.Payment.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Payment.Controllers;

[Route("api/payment")]
[Authorize]
public sealed class PaymentGatewayController(
    IPaymentGatewayService gatewayService,
    IPaymentReconciliationService reconciliationService,
    IQrCodeGenerator qrCodeGenerator) : BaseController
{
    // ── Gateway Accounts ──────────────────────────────────────────────────────

    [HttpGet("gateways")]
    [RequirePermission("Payment.View")]
    [RequireFeature("Payment.OnlineGateway")]
    public async Task<IActionResult> ListGatewayAccounts(CancellationToken ct = default)
        => Ok(await gatewayService.ListAccountsAsync(ct));

    [HttpPost("gateways")]
    [RequirePermission("Payment.Configure")]
    [RequireFeature("Payment.OnlineGateway")]
    public async Task<IActionResult> UpsertGatewayAccount([FromBody] UpsertGatewayAccountDto dto, CancellationToken ct = default)
        => Ok(await gatewayService.UpsertAccountAsync(dto, ct));

    // ── Transactions ──────────────────────────────────────────────────────────

    [HttpGet("transactions")]
    [RequirePermission("Payment.View")]
    public async Task<IActionResult> ListTransactions(
        [FromQuery] PaymentGatewayStatus? status,
        [FromQuery] string? gateway,
        [FromQuery] PaymentPurpose? purpose,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var filter = new PaymentTransactionListFilter(status, gateway, purpose, from, to, page, pageSize);
        return Ok(await gatewayService.ListAsync(filter, ct));
    }

    [HttpGet("transactions/{id:long}")]
    [RequirePermission("Payment.View")]
    public async Task<IActionResult> GetTransaction(long id, CancellationToken ct = default)
    {
        var result = await gatewayService.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("transactions")]
    [RequirePermission("Payment.Initiate")]
    [RequireFeature("Payment.OnlineGateway")]
    public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentDto dto, CancellationToken ct = default)
        => Ok(await gatewayService.InitiateAsync(dto, ct));

    [HttpPost("transactions/{id:long}/confirm")]
    [RequirePermission("Payment.Manage")]
    public async Task<IActionResult> ConfirmPayment(long id, [FromBody] ConfirmPaymentDto dto, CancellationToken ct = default)
        => Ok(await gatewayService.ConfirmAsync(id, dto, ct));

    [HttpPost("transactions/{id:long}/fail")]
    [RequirePermission("Payment.Manage")]
    public async Task<IActionResult> FailPayment(long id, [FromBody] FailPaymentDto dto, CancellationToken ct = default)
        => Ok(await gatewayService.FailAsync(id, dto, ct));

    [HttpPost("transactions/{id:long}/cancel")]
    [RequirePermission("Payment.Manage")]
    public async Task<IActionResult> CancelPayment(long id, CancellationToken ct = default)
        => Ok(await gatewayService.CancelAsync(id, ct));

    [HttpPost("transactions/{id:long}/refund")]
    [RequirePermission("Payment.Refund")]
    public async Task<IActionResult> RefundPayment(long id, [FromBody] RefundPaymentDto dto, CancellationToken ct = default)
        => Ok(await gatewayService.RefundAsync(id, dto, ct));

    // ── Reconciliation ────────────────────────────────────────────────────────

    [HttpPost("transactions/reconcile")]
    [RequirePermission("Payment.Reconcile")]
    public async Task<IActionResult> TriggerReconciliation(
        [FromQuery] string gatewayCode,
        [FromQuery] DateTime? settlementDate,
        CancellationToken ct = default)
    {
        var date = settlementDate ?? DateTime.UtcNow.Date.AddDays(-1);
        return Ok(await reconciliationService.RunReconciliationAsync(gatewayCode, date, ct));
    }

    [HttpGet("transactions/exceptions")]
    [RequirePermission("Payment.View")]
    public async Task<IActionResult> ListExceptions(
        [FromQuery] ReconciliationExceptionStatus? status,
        CancellationToken ct = default)
        => Ok(await reconciliationService.ListExceptionsAsync(status, ct));

    [HttpPost("transactions/exceptions/{id:long}/resolve")]
    [RequirePermission("Payment.Reconcile")]
    public async Task<IActionResult> ResolveException(long id, [FromBody] ResolveExceptionDto dto, CancellationToken ct = default)
        => Ok(await reconciliationService.ResolveExceptionAsync(id, dto, ct));

    // ── QR Code ───────────────────────────────────────────────────────────────

    [HttpGet("invoices/{invoiceId:long}/qr")]
    [RequirePermission("Payment.View")]
    public async Task<IActionResult> GetInvoiceQr(
        long invoiceId,
        [FromQuery] string vpa,
        [FromQuery] string payeeName,
        [FromQuery] decimal amount,
        [FromQuery] string? note = null,
        CancellationToken ct = default)
    {
        var result = await qrCodeGenerator.GenerateUpiQrAsync(
            new UpiQrRequest(vpa, payeeName, amount, $"INV-{invoiceId}", note), ct);
        return Ok(result);
    }

    // ── Test connection ───────────────────────────────────────────────────────

    [HttpPost("gateways/test")]
    [RequirePermission("Payment.Configure")]
    [RequireFeature("Payment.OnlineGateway")]
    public IActionResult TestConnection([FromBody] UpsertGatewayAccountDto dto)
    {
        // Connectivity test: just validates that the gateway code is a known one.
        // Real ping is gateway-specific and done lazily on first transaction.
        var known = new[] { "Razorpay", "Stripe", "PhonePe", "Paytm", "Simulated" };
        return known.Contains(dto.GatewayCode)
            ? Ok(new { status = "ok", message = $"{dto.GatewayCode} connector is registered." })
            : BadRequest(new { status = "error", message = $"Unknown gateway code: {dto.GatewayCode}" });
    }
}
