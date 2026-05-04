using ErpSaas.Infrastructure.Data;
using ErpSaas.Modules.Payment.Connectors.Results;
using ErpSaas.Modules.Payment.Enums;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Payment.Connectors.Simulated;

/// <summary>
/// Default connector used when no real PaymentGatewayAccount is configured.
/// All flows succeed immediately — reports, reconciliation, and refunds all
/// work in full fidelity without any real gateway credentials.
/// </summary>
public sealed class SimulatedGatewayConnector(TenantDbContext db) : IPaymentGatewayConnector
{
    public string GatewayCode => "Simulated";

    public Task<GatewayInitiateResult> InitiateAsync(GatewayInitiateRequest req, CancellationToken ct)
    {
        var fakeId = $"sim_pay_{Guid.NewGuid():N}";
        var fakeUrl = $"https://simulated-gateway.local/pay/{fakeId}";
        return Task.FromResult(GatewayInitiateResult.Success(fakeId, fakeUrl));
    }

    public Task<GatewayRefundResult> RefundAsync(GatewayRefundRequest req, CancellationToken ct)
    {
        var fakeRefundId = $"sim_rfnd_{Guid.NewGuid():N}";
        return Task.FromResult(GatewayRefundResult.Success(fakeRefundId, req.RefundAmount));
    }

    public async Task<SettlementReport> FetchSettlementReportAsync(DateTime settlementDate, CancellationToken ct)
    {
        // Synthetic settlement report: one line per Success transaction on the given date.
        var txns = await db.Set<Entities.PaymentGatewayTransaction>()
            .Where(t => t.GatewayCode == GatewayCode
                     && t.Status == PaymentGatewayStatus.Success
                     && t.InitiatedAtUtc.Date == settlementDate.Date
                     && t.SettledAtUtc == null)
            .ToListAsync(ct);

        var lines = txns.Select(t =>
        {
            var fee = Math.Round(t.Amount * 0.02m, 4);   // simulate 2% gateway fee
            var gst = Math.Round(fee * 0.18m, 4);         // 18% GST on fee
            return new SettlementLine(
                t.GatewayTxnId,
                t.OurReferenceNumber,
                t.Amount,
                fee,
                gst,
                t.Amount - fee - gst,
                settlementDate.Date.AddHours(2));          // simulate T+1 settlement at 02:00
        }).ToList();

        return new SettlementReport(GatewayCode, settlementDate, lines);
    }

    public bool VerifyWebhookSignature(string payload, string signature, string secret)
        => true; // Simulated — always valid

    public WebhookEvent? ParseWebhookEvent(string rawPayload)
    {
        // Simulated webhook events are not externally triggered.
        // Return null so the controller skips fan-out — test via direct API calls.
        return null;
    }
}
