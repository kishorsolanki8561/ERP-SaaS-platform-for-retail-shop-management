using ErpSaas.Modules.Wallet.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Wallet.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record InitiateTopUpDto(
    long CustomerId,
    string CustomerName,
    decimal Amount,
    string PaymentModeCode,
    string? Notes,
    string? CustomerPhone = null);

public record CompleteTopUpDto(
    long? PaymentGatewayTransactionId,
    string? Notes);

public record WalletTopUpDto(
    long Id,
    long CustomerId,
    string CustomerName,
    decimal Amount,
    string PaymentModeCode,
    WalletTopUpStatus Status,
    string? ReceiptNumber,
    string? Notes,
    DateTime InitiatedAtUtc,
    DateTime? CompletedAtUtc,
    string? FailureReason);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IWalletTopUpService
{
    Task<Result<long>> InitiateAsync(InitiateTopUpDto dto, CancellationToken ct = default);

    Task<Result<bool>> CompleteAsync(long topUpId, CompleteTopUpDto dto, CancellationToken ct = default);

    Task<Result<bool>> FailAsync(long topUpId, string reason, CancellationToken ct = default);

    Task<WalletTopUpDto?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<WalletTopUpDto>> ListAsync(long customerId, int page, int pageSize, CancellationToken ct = default);
}
