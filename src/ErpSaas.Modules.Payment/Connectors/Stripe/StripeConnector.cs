using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ErpSaas.Infrastructure.Http;
using ErpSaas.Modules.Payment.Connectors.Results;

namespace ErpSaas.Modules.Payment.Connectors.Stripe;

/// <summary>
/// Stripe connector stub. Wire up credentials via /api/payment/gateways
/// with GatewayCode="Stripe" to activate. Credentials JSON format:
/// {"SecretKey":"sk_live_...","PublishableKey":"pk_live_..."}
/// </summary>
public sealed class StripeConnector(HttpClient httpClient)
    : ThirdPartyApiClientBase(httpClient), IPaymentGatewayConnector
{
    public string GatewayCode => "Stripe";

    public Task<GatewayInitiateResult> InitiateAsync(GatewayInitiateRequest req, CancellationToken ct)
        => throw new NotSupportedException("Stripe credentials not configured. Save credentials via /api/payment/gateways.");

    public Task<GatewayRefundResult> RefundAsync(GatewayRefundRequest req, CancellationToken ct)
        => throw new NotSupportedException("Stripe credentials not configured.");

    public Task<SettlementReport> FetchSettlementReportAsync(DateTime settlementDate, CancellationToken ct)
        => Task.FromResult(new SettlementReport(GatewayCode, settlementDate, []));

    public bool VerifyWebhookSignature(string payload, string signature, string secret)
    {
        // Stripe uses "t=timestamp,v1=hmac" format
        var parts = signature.Split(',');
        var timestampPart = parts.FirstOrDefault(p => p.StartsWith("t="))?.Substring(2) ?? string.Empty;
        var v1Part = parts.FirstOrDefault(p => p.StartsWith("v1="))?.Substring(3) ?? string.Empty;

        var signedPayload = $"{timestampPart}.{payload}";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(keyBytes);
        var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLower();
        return computed == v1Part;
    }

    public WebhookEvent? ParseWebhookEvent(string rawPayload)
    {
        using var doc = JsonDocument.Parse(rawPayload);
        var root = doc.RootElement;
        if (!root.TryGetProperty("type", out var typeEl)) return null;
        var type = typeEl.GetString() ?? string.Empty;

        return type switch
        {
            "payment_intent.succeeded" => ParseStripePaymentIntent(root, "payment.captured"),
            "payment_intent.payment_failed" => ParseStripePaymentIntent(root, "payment.failed"),
            "charge.refunded" => ParseStripeRefund(root),
            _ => null
        };
    }

    private static WebhookEvent? ParseStripePaymentIntent(JsonElement root, string mappedEvent)
    {
        if (!root.TryGetProperty("data", out var data)) return null;
        if (!data.TryGetProperty("object", out var obj)) return null;
        var id = obj.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty;
        var amount = obj.TryGetProperty("amount", out var amtEl) ? amtEl.GetDecimal() / 100m : (decimal?)null;
        return new WebhookEvent(mappedEvent, id, amount, null, null, null);
    }

    private static WebhookEvent? ParseStripeRefund(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data)) return null;
        if (!data.TryGetProperty("object", out var obj)) return null;
        var paymentId = obj.TryGetProperty("payment_intent", out var piEl) ? piEl.GetString() ?? string.Empty : string.Empty;
        var refundId = obj.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty;
        var amount = obj.TryGetProperty("amount_refunded", out var amtEl) ? amtEl.GetDecimal() / 100m : (decimal?)null;
        return new WebhookEvent("refund.processed", paymentId, amount, null, null, refundId);
    }
}
