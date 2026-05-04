using ErpSaas.Modules.Payment.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Payment.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record InitiatePaymentDto(
    PaymentPurpose Purpose,
    string GatewayCode,
    decimal Amount,
    string Currency,
    long? SourceInvoiceId,
    long? SourceWalletTopUpId,
    long? SourceSubscriptionInvoiceId);

public record PaymentGatewayTransactionDto(
    long Id,
    string GatewayCode,
    string GatewayTxnId,
    string OurReferenceNumber,
    PaymentPurpose Purpose,
    decimal Amount,
    string Currency,
    string? Method,
    string? Vpa,
    string? CardLast4,
    PaymentGatewayStatus Status,
    string? FailureCode,
    string? FailureMessage,
    DateTime InitiatedAtUtc,
    DateTime? CompletedAtUtc,
    decimal GatewayFee,
    decimal GatewayGst,
    decimal NetSettled,
    DateTime? SettledAtUtc,
    string? SettlementReference,
    string? RefundGatewayTxnId,
    string? PaymentUrl);

public record ConfirmPaymentDto(
    string GatewayTxnId,
    string? Method,
    string? Vpa,
    string? CardLast4,
    decimal GatewayFee,
    decimal GatewayGst,
    decimal NetSettled);

public record FailPaymentDto(
    string GatewayTxnId,
    string? FailureCode,
    string? FailureMessage);

public record RefundPaymentDto(
    decimal RefundAmount,
    string? Reason);

public record PaymentGatewayAccountDto(
    long Id,
    string GatewayCode,
    bool IsActive,
    bool IsDefault);

public record UpsertGatewayAccountDto(
    string GatewayCode,
    string CredentialsJson,
    string? WebhookSecret,
    bool IsActive,
    bool IsDefault);

public record PaymentTransactionListFilter(
    PaymentGatewayStatus? Status,
    string? GatewayCode,
    PaymentPurpose? Purpose,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page,
    int PageSize);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IPaymentGatewayService
{
    Task<Result<long>> InitiateAsync(InitiatePaymentDto dto, CancellationToken ct = default);
    Task<Result<bool>> ConfirmAsync(long transactionId, ConfirmPaymentDto dto, CancellationToken ct = default);
    Task<Result<bool>> FailAsync(long transactionId, FailPaymentDto dto, CancellationToken ct = default);
    Task<Result<bool>> RefundAsync(long transactionId, RefundPaymentDto dto, CancellationToken ct = default);
    Task<Result<bool>> CancelAsync(long transactionId, CancellationToken ct = default);

    Task<PaymentGatewayTransactionDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentGatewayTransactionDto>> ListAsync(PaymentTransactionListFilter filter, CancellationToken ct = default);

    Task<Result<bool>> HandleWebhookAsync(string gatewayCode, string rawPayload, string signature, CancellationToken ct = default);

    Task<IReadOnlyList<PaymentGatewayAccountDto>> ListAccountsAsync(CancellationToken ct = default);
    Task<Result<long>> UpsertAccountAsync(UpsertGatewayAccountDto dto, CancellationToken ct = default);
}
