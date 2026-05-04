using ErpSaas.Modules.Payment.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Payment.Entities;

[Auditable("Payment.PaymentGatewayTransaction")]
public class PaymentGatewayTransaction : TenantEntity
{
    public string GatewayCode { get; set; } = default!;           // DDL PAYMENT_GATEWAY
    public string GatewayTxnId { get; set; } = default!;          // unique per gateway
    public string OurReferenceNumber { get; set; } = default!;    // what we sent to the gateway
    public PaymentPurpose Purpose { get; set; }
    public long? SourceInvoiceId { get; set; }
    public long? SourceWalletTopUpId { get; set; }
    public long? SourceSubscriptionInvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string? Method { get; set; }                            // UPI, Card, NetBanking, Wallet-Provider
    public string? Vpa { get; set; }
    public string? CardLast4 { get; set; }
    public PaymentGatewayStatus Status { get; set; } = PaymentGatewayStatus.Initiated;
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime InitiatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public decimal GatewayFee { get; set; }
    public decimal GatewayGst { get; set; }
    public decimal NetSettled { get; set; }
    public DateTime? SettledAtUtc { get; set; }
    public string? SettlementReference { get; set; }
    public long? ThirdPartyApiLogId { get; set; }
    public string? PaymentUrl { get; set; }           // redirect URL returned by gateway on initiation
    public string? RefundGatewayTxnId { get; set; }  // populated after successful gateway refund call
}
