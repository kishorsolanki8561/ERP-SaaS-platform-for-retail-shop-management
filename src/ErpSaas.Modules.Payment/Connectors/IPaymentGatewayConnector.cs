using ErpSaas.Modules.Payment.Connectors.Results;

namespace ErpSaas.Modules.Payment.Connectors;

public sealed record GatewayInitiateRequest(
    string OurReferenceNumber,
    decimal Amount,
    string Currency,
    string? Description,
    string? CustomerEmail,
    string? CustomerPhone,
    IDictionary<string, string>? Metadata);

public sealed record GatewayRefundRequest(
    string GatewayTxnId,
    string OurReferenceNumber,
    decimal RefundAmount,
    string? Reason);

/// <summary>
/// Abstraction over a payment gateway (Razorpay, Stripe, PhonePe, etc.).
/// The connector is selected at runtime by IGatewayConnectorRegistry based on
/// the GatewayCode stored in PaymentGatewayAccount. When no real account is
/// configured, SimulatedGatewayConnector is used — all flows work without
/// real credentials.
/// </summary>
public interface IPaymentGatewayConnector
{
    string GatewayCode { get; }

    Task<GatewayInitiateResult> InitiateAsync(GatewayInitiateRequest req, CancellationToken ct);

    Task<GatewayRefundResult> RefundAsync(GatewayRefundRequest req, CancellationToken ct);

    Task<SettlementReport> FetchSettlementReportAsync(DateTime settlementDate, CancellationToken ct);

    bool VerifyWebhookSignature(string payload, string signature, string secret);

    /// <summary>
    /// Parses a raw webhook payload into a normalized event type + gateway txn ID.
    /// Returns null if the event type is unknown or not actionable.
    /// </summary>
    WebhookEvent? ParseWebhookEvent(string rawPayload);
}

public sealed record WebhookEvent(
    string EventType,     // "payment.captured" | "payment.failed" | "refund.processed"
    string GatewayTxnId,
    decimal? Amount,
    string? FailureCode,
    string? FailureMessage,
    string? GatewayRefundId);
