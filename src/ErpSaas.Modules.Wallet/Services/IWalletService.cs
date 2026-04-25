using ErpSaas.Modules.Wallet.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Wallet.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record WalletBalanceDto(
    long CustomerId,
    string CustomerName,
    decimal Balance,
    DateTime? LastTransactionAtUtc);

public record WalletTransactionDto(
    long Id,
    WalletTransactionType TransactionType,
    decimal Amount,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string ReferenceType,
    string? ReferenceNumber,
    string? ReceiptNumber,
    string? Notes,
    DateTime CreatedAtUtc);

public record WalletCreditDto(
    long CustomerId,
    string CustomerName,
    decimal Amount,
    string? ReferenceType,
    long? ReferenceId,
    string? ReferenceNumber,
    string? Notes);

public record WalletDebitDto(
    long CustomerId,
    decimal Amount,
    string? ReferenceType,
    long? ReferenceId,
    string? ReferenceNumber,
    string? Notes);

public record WalletCreditResultDto(
    string ReceiptNumber,
    decimal NewBalance);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IWalletService
{
    Task<PagedResult<WalletBalanceDto>> ListBalancesAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken ct = default);

    Task<WalletBalanceDto?> GetBalanceAsync(long customerId, CancellationToken ct = default);

    Task<PagedResult<WalletTransactionDto>> ListTransactionsAsync(
        long customerId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Result<WalletCreditResultDto>> CreditAsync(WalletCreditDto dto, CancellationToken ct = default);

    Task<Result<bool>> DebitAsync(WalletDebitDto dto, CancellationToken ct = default);

    /// <summary>Called by BillingService when wallet is used to settle an invoice.</summary>
    Task<Result<bool>> DebitForInvoiceAsync(
        long customerId,
        long invoiceId,
        string invoiceNumber,
        decimal amount,
        CancellationToken ct = default);
}
