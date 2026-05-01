using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record CreateBankStatementDto(
    long BankAccountId,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal OpeningBalance,
    decimal ClosingBalance,
    string? Notes = null);

public record BankStatementListDto(
    long Id,
    long BankAccountId,
    string BankName,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal OpeningBalance,
    decimal ClosingBalance,
    bool IsCompleted,
    int TotalLines,
    int UnmatchedLines);

public record BankStatementLineDto(
    long Id,
    DateTime TransactionDate,
    string Description,
    string? Reference,
    decimal CreditAmount,
    decimal DebitAmount,
    BankStatementLineStatus MatchStatus,
    long? MatchedVoucherId,
    DateTime? MatchedAtUtc);

public record BankStatementDetailDto(
    long Id,
    long BankAccountId,
    string BankName,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal OpeningBalance,
    decimal ClosingBalance,
    bool IsCompleted,
    string? Notes,
    IReadOnlyList<BankStatementLineDto> Lines);

public record ImportBankStatementLineDto(
    DateTime TransactionDate,
    string Description,
    string? Reference,
    decimal CreditAmount,
    decimal DebitAmount);

public record ManualMatchLineDto(long VoucherId);

public record PostAdjustmentDto(
    long BankStatementLineId,
    long AccountId,
    string Narration);

public record CreateReconciliationRuleDto(
    string Name,
    string PatternContains,
    long AccountId,
    VoucherType VoucherType);

public record ReconciliationRuleDto(
    long Id,
    string Name,
    string PatternContains,
    long AccountId,
    string AccountName,
    VoucherType VoucherType,
    bool IsActive);

public record AutoMatchResultDto(int MatchedCount, int UnmatchedCount);

// ── Interface ──────────────────────────────────────────────────────────────────

public interface IBankReconciliationService
{
    Task<PagedResult<BankStatementListDto>> ListBankStatementsAsync(long? bankAccountId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<long>> CreateBankStatementAsync(CreateBankStatementDto dto, CancellationToken ct = default);
    Task<BankStatementDetailDto?> GetBankStatementAsync(long id, CancellationToken ct = default);
    Task<Result<int>> ImportLinesAsync(long statementId, IReadOnlyList<ImportBankStatementLineDto> lines, CancellationToken ct = default);
    Task<Result<AutoMatchResultDto>> AutoMatchAsync(long statementId, CancellationToken ct = default);
    Task<Result<bool>> ManualMatchLineAsync(long lineId, ManualMatchLineDto dto, CancellationToken ct = default);
    Task<Result<bool>> IgnoreLineAsync(long lineId, CancellationToken ct = default);
    Task<Result<long>> PostAdjustmentAsync(PostAdjustmentDto dto, CancellationToken ct = default);
    Task<Result<bool>> CompleteReconciliationAsync(long statementId, CancellationToken ct = default);
    Task<IReadOnlyList<ReconciliationRuleDto>> ListReconciliationRulesAsync(CancellationToken ct = default);
    Task<Result<long>> CreateReconciliationRuleAsync(CreateReconciliationRuleDto dto, CancellationToken ct = default);
    Task<Result<bool>> ToggleReconciliationRuleAsync(long ruleId, CancellationToken ct = default);
}
