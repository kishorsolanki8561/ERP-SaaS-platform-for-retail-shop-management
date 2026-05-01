using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record RegisterFixedAssetDto(
    string Name,
    string CategoryCode,
    DateTime PurchaseDate,
    decimal PurchaseCost,
    DepreciationMethod Method,
    decimal UsefulLifeYears,
    decimal SalvageValue,
    long? SupplierId = null,
    string? LocationNotes = null,
    long? AssignedToEmployeeId = null);

public record FixedAssetListDto(
    long Id,
    string AssetCode,
    string Name,
    string CategoryCode,
    DateTime PurchaseDate,
    decimal PurchaseCost,
    decimal AccumulatedDepreciation,
    decimal NetBookValue,
    FixedAssetStatus Status);

public record DepreciationScheduleEntryDto(
    DateTime PeriodDate,
    decimal Amount,
    decimal AccumulatedAfter,
    decimal NetBookValueAfter,
    long VoucherId);

public record DisposeFixedAssetDto(
    DateTime DisposalDate,
    decimal DisposalValue,
    long ProceedsAccountId);

// ── Interface ──────────────────────────────────────────────────────────────────

public interface IFixedAssetService
{
    Task<PagedResult<FixedAssetListDto>> ListAsync(FixedAssetStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<Result<long>> RegisterAsync(RegisterFixedAssetDto dto, CancellationToken ct = default);
    Task<Result<bool>> RetireAsync(long id, CancellationToken ct = default);
    Task<Result<bool>> DisposeAsync(long id, DisposeFixedAssetDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<DepreciationScheduleEntryDto>> GetDepreciationScheduleAsync(long id, CancellationToken ct = default);
    Task<Result<int>> RunDepreciationAsync(DateTime periodDate, CancellationToken ct = default);
}
