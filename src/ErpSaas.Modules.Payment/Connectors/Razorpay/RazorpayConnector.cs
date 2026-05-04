using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Http;
using ErpSaas.Modules.Payment.Connectors.Results;
using ErpSaas.Modules.Payment.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Payment.Connectors.Razorpay;

/// <summary>
/// Razorpay connector. Activates automatically once a shop saves Razorpay
/// credentials via /api/payment/gateways. Until then, the registry falls
/// back to SimulatedGatewayConnector.
/// </summary>
public sealed class RazorpayConnector(HttpClient httpClient, TenantDbContext db)
    : ThirdPartyApiClientBase(httpClient), IPaymentGatewayConnector
{
    private const string BaseUrl = "https://api.razorpay.com/v1";

    public string GatewayCode => "Razorpay";

    public async Task<GatewayInitiateResult> InitiateAsync(GatewayInitiateRequest req, CancellationToken ct)
    {
        await ConfigureAuthAsync(ct);
        var body = new
        {
            amount = (long)(req.Amount * 100),   // paise
            currency = req.Currency,
            receipt = req.OurReferenceNumber,
            notes = req.Metadata
        };
        try
        {
            var result = await PostAsync<object, JsonElement>($"{BaseUrl}/orders", body, ct);
            var orderId = result.GetProperty("id").GetString() ?? string.Empty;
            return GatewayInitiateResult.Success(orderId, paymentUrl: null);
        }
        catch (Exception ex)
        {
            return GatewayInitiateResult.Failure("RAZORPAY_ERROR", ex.Message);
        }
    }

    public async Task<GatewayRefundResult> RefundAsync(GatewayRefundRequest req, CancellationToken ct)
    {
        await ConfigureAuthAsync(ct);
        var body = new
        {
            amount = (long)(req.RefundAmount * 100),
            notes = new { reason = req.Reason }
        };
        try
        {
            var result = await PostAsync<object, JsonElement>(
                $"{BaseUrl}/payments/{req.GatewayTxnId}/refund", body, ct);
            var refundId = result.GetProperty("id").GetString() ?? string.Empty;
            return GatewayRefundResult.Success(refundId, req.RefundAmount);
        }
        catch (Exception ex)
        {
            return GatewayRefundResult.Failure("RAZORPAY_REFUND_ERROR", ex.Message);
        }
    }

    public async Task<SettlementReport> FetchSettlementReportAsync(DateTime settlementDate, CancellationToken ct)
    {
        await ConfigureAuthAsync(ct);
        var from = new DateTimeOffset(settlementDate.Date, TimeSpan.Zero).ToUnixTimeSeconds();
        var to   = new DateTimeOffset(settlementDate.Date.AddDays(1), TimeSpan.Zero).ToUnixTimeSeconds();
        try
        {
            var result = await GetAsync<JsonElement>(
                $"{BaseUrl}/settlements/recon/combined?from={from}&to={to}&count=500", ct);

            var lines = new List<SettlementLine>();
            if (result.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var txnId   = item.GetProperty("payment_id").GetString() ?? string.Empty;
                    var ourRef  = item.TryGetProperty("description", out var desc) ? desc.GetString() ?? string.Empty : string.Empty;
                    var settled = item.GetProperty("settlement_utr").GetString() ?? string.Empty;
                    var amount  = item.GetProperty("debit").GetDecimal() / 100m;
                    var fee     = item.GetProperty("fee").GetDecimal() / 100m;
                    var gst     = item.GetProperty("tax").GetDecimal() / 100m;
                    lines.Add(new SettlementLine(txnId, ourRef, amount, fee, gst, amount - fee - gst,
                        settlementDate.Date));
                }
            }
            return new SettlementReport(GatewayCode, settlementDate, lines);
        }
        catch
        {
            return new SettlementReport(GatewayCode, settlementDate, []);
        }
    }

    public bool VerifyWebhookSignature(string payload, string signature, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        var computed = Convert.ToHexString(hmac.ComputeHash(payloadBytes)).ToLower();
        return computed == signature;
    }

    public WebhookEvent? ParseWebhookEvent(string rawPayload)
    {
        var wrapper = JsonSerializer.Deserialize<RazorpayWebhookPayload>(rawPayload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (wrapper is null) return null;

        return wrapper.Event switch
        {
            "payment.captured" when wrapper.Payload?.Payment?.Entity is { } p =>
                new WebhookEvent("payment.captured", p.Id, p.Amount / 100m, null, null, null),

            "payment.failed" when wrapper.Payload?.Payment?.Entity is { } p =>
                new WebhookEvent("payment.failed", p.Id, null, p.ErrorCode, p.ErrorDescription, null),

            "refund.processed" when wrapper.Payload?.Refund?.Entity is { } r =>
                new WebhookEvent("refund.processed", r.PaymentId, r.Amount / 100m, null, null, r.Id),

            _ => null
        };
    }

    private async Task ConfigureAuthAsync(CancellationToken ct)
    {
        var account = await db.Set<PaymentGatewayAccount>()
            .FirstOrDefaultAsync(a => a.GatewayCode == GatewayCode && a.IsActive, ct)
            ?? throw new InvalidOperationException("Razorpay account not configured.");

        // CredentialsJsonEncrypted stores JSON: {"KeyId":"...","KeySecret":"..."}
        var creds = JsonSerializer.Deserialize<RazorpayCreds>(account.CredentialsJsonEncrypted)
            ?? throw new InvalidOperationException("Invalid Razorpay credentials format.");

        var encoded = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{creds.KeyId}:{creds.KeySecret}"));
        Http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);
    }

    private sealed record RazorpayCreds(string KeyId, string KeySecret);
}
