using ErpSaas.Modules.Payment.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Payment.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record ReconciliationExceptionDto(
    long Id,
    string GatewayCode,
    string? GatewayTxnId,
    string? OurReferenceNumber,
    long? PaymentGatewayTransactionId,
    ReconciliationExceptionType ExceptionType,
    ReconciliationExceptionStatus Status,
    decimal? OurAmount,
    decimal? GatewayAmount,
    decimal? OurFee,
    decimal? GatewayFee,
    string? Notes,
    DateTime DetectedAtUtc,
    DateTime? ResolvedAtUtc,
    string? ResolutionNotes);

public record ResolveExceptionDto(string ResolutionNotes);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IPaymentReconciliationService
{
    Task<Result<int>> RunReconciliationAsync(string gatewayCode, DateTime settlementDate, CancellationToken ct = default);
    Task<IReadOnlyList<ReconciliationExceptionDto>> ListExceptionsAsync(ReconciliationExceptionStatus? status, CancellationToken ct = default);
    Task<Result<bool>> ResolveExceptionAsync(long exceptionId, ResolveExceptionDto dto, CancellationToken ct = default);
}
