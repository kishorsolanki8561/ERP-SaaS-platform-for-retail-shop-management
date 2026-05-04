using ErpSaas.Infrastructure.Http;
using ErpSaas.Modules.Payment.Connectors.Results;

namespace ErpSaas.Modules.Payment.Connectors.PhonePe;

/// <summary>
/// PhonePe connector stub. Credentials JSON format:
/// {"MerchantId":"...","SaltKey":"...","SaltIndex":1}
/// </summary>
public sealed class PhonePeConnector(HttpClient httpClient)
    : ThirdPartyApiClientBase(httpClient), IPaymentGatewayConnector
{
    public string GatewayCode => "PhonePe";

    public Task<GatewayInitiateResult> InitiateAsync(GatewayInitiateRequest req, CancellationToken ct)
        => throw new NotSupportedException("PhonePe credentials not configured. Save credentials via /api/payment/gateways.");

    public Task<GatewayRefundResult> RefundAsync(GatewayRefundRequest req, CancellationToken ct)
        => throw new NotSupportedException("PhonePe credentials not configured.");

    public Task<SettlementReport> FetchSettlementReportAsync(DateTime settlementDate, CancellationToken ct)
        => Task.FromResult(new SettlementReport(GatewayCode, settlementDate, []));

    public bool VerifyWebhookSignature(string payload, string signature, string secret)
        => false; // Not implemented until credentials are available

    public WebhookEvent? ParseWebhookEvent(string rawPayload) => null;
}
