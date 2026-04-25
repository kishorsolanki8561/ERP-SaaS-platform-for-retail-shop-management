using ErpSaas.Modules.Shift.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Shift.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record DenominationDto(int Denomination, int Count);

public record OpenShiftDto(
    long BranchId,
    decimal OpeningCash,
    IReadOnlyList<DenominationDto>? Denominations,
    string CashierName,
    string? CashierPhone = null);

public record CloseShiftDto(
    decimal ClosingCashCounted,
    IReadOnlyList<DenominationDto>? Denominations,
    string? Notes);

public record CashMovementDto(
    decimal Amount,
    string? ReasonCode,
    string? Notes,
    long? AuthorizedByUserId = null);

public record ShiftSummaryDto(
    long Id,
    long BranchId,
    string CashierName,
    DateTime OpenedAtUtc,
    decimal OpeningCash,
    ShiftStatus Status,
    int TransactionCount,
    decimal TotalSales,
    decimal TotalCashSales,
    decimal TotalCardSales,
    decimal TotalUpiSales,
    decimal TotalWalletDebits,
    decimal TotalCashRefunds,
    DateTime? ClosedAtUtc,
    decimal? ClosingCashCounted,
    decimal? SystemComputedCash,
    decimal? CashVariance,
    string? ClosingNotes);

public record ShiftListItemDto(
    long Id,
    string CashierName,
    DateTime OpenedAtUtc,
    ShiftStatus Status,
    decimal TotalSales,
    int TransactionCount,
    decimal? CashVariance);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IShiftService
{
    Task<Result<long>> OpenShiftAsync(OpenShiftDto dto, CancellationToken ct = default);
    Task<Result<bool>> CloseShiftAsync(long shiftId, CloseShiftDto dto, CancellationToken ct = default);
    Task<Result<bool>> ForceCloseAsync(long shiftId, string reason, CancellationToken ct = default);
    Task<Result<bool>> RecordCashInAsync(long shiftId, CashMovementDto dto, CancellationToken ct = default);
    Task<Result<bool>> RecordCashOutAsync(long shiftId, CashMovementDto dto, CancellationToken ct = default);
    Task<ShiftSummaryDto?> GetOpenShiftForCashierAsync(long userId, long branchId, CancellationToken ct = default);
    Task<ShiftSummaryDto?> GetShiftSummaryAsync(long shiftId, CancellationToken ct = default);
    Task<PagedResult<ShiftListItemDto>> ListShiftsAsync(int page, int pageSize, long? branchId, CancellationToken ct = default);
}
