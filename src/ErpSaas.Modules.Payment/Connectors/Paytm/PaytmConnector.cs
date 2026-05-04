using ErpSaas.Infrastructure.Http;
using ErpSaas.Modules.Payment.Connectors.Results;

namespace ErpSaas.Modules.Payment.Connectors.Paytm;

/// <summary>
/// Paytm connector stub. Credentials JSON format:
/// {"MerchantId":"...","MerchantKey":"...","Environment":"Production"}
/// </summary>
public sealed class PaytmConnector(HttpClient httpClient)
    : ThirdPartyApiClientBase(httpClient), IPaymentGatewayConnector
{
    public string GatewayCode => "Paytm";

    public Task<GatewayInitiateResult> InitiateAsync(GatewayInitiateRequest req, CancellationToken ct)
        => throw new NotSupportedException("Paytm credentials not configured. Save credentials via /api/payment/gateways.");

    public Task<GatewayRefundResult> RefundAsync(GatewayRefundRequest req, CancellationToken ct)
        => throw new NotSupportedException("Paytm credentials not configured.");

    public Task<SettlementReport> FetchSettlementReportAsync(DateTime settlementDate, CancellationToken ct)
        => Task.FromResult(new SettlementReport(GatewayCode, settlementDate, []));

    public bool VerifyWebhookSignature(string payload, string signature, string secret)
        => false;

    public WebhookEvent? ParseWebhookEvent(string rawPayload) => null;
}
