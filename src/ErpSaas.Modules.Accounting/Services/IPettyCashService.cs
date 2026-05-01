using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record PettyCashTopUpDto(
    decimal Amount,
    long FromBankAccountId,
    string? Narration = null);

public record PettyCashExpenseDto(
    decimal Amount,
    long ExpenseAccountId,
    string Description);

public record PettyCashClosureDto(
    DateTime ClosureDate,
    decimal CountedBalance,
    string Narration);

public record PettyCashClosureListDto(
    long Id,
    DateTime ClosureDate,
    decimal ExpectedBalance,
    decimal CountedBalance,
    decimal Variance);

// ── Interface ──────────────────────────────────────────────────────────────────

public interface IPettyCashService
{
    Task<Result<long>> TopUpAsync(PettyCashTopUpDto dto, CancellationToken ct = default);
    Task<Result<long>> RecordExpenseAsync(PettyCashExpenseDto dto, CancellationToken ct = default);
    Task<Result<long>> ClosePeriodAsync(PettyCashClosureDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<PettyCashClosureListDto>> ListClosuresAsync(CancellationToken ct = default);
}
