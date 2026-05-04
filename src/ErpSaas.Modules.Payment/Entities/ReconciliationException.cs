using ErpSaas.Modules.Payment.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Payment.Entities;

[Auditable("Payment.ReconciliationException")]
public class ReconciliationException : TenantEntity
{
    public string GatewayCode { get; set; } = default!;
    public string? GatewayTxnId { get; set; }
    public string? OurReferenceNumber { get; set; }
    public long? PaymentGatewayTransactionId { get; set; }
    public ReconciliationExceptionType ExceptionType { get; set; }
    public ReconciliationExceptionStatus Status { get; set; } = ReconciliationExceptionStatus.Open;
    public decimal? OurAmount { get; set; }
    public decimal? GatewayAmount { get; set; }
    public decimal? OurFee { get; set; }
    public decimal? GatewayFee { get; set; }
    public string? Notes { get; set; }
    public DateTime DetectedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public long? ResolvedByUserId { get; set; }
    public string? ResolutionNotes { get; set; }
}
