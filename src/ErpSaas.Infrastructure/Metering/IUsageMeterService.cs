using ErpSaas.Shared.Services;

namespace ErpSaas.Infrastructure.Metering;

public enum QuotaCheckStatus { Allow, Warn, Deny }
public enum QuotaStatus { Ok, Warning, OverQuota, HardCapReached }

public record QuotaCheckResult(
    QuotaCheckStatus Status,
    long Used,
    long Quota,
    string? Message = null)
{
    public bool IsDenied => Status == QuotaCheckStatus.Deny;
}

public record UsageMeterDto(
    string MeterCode,
    long Used,
    long Quota,
    bool HardCapEnforced,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc);

public record UsageForecastDto(
    string MeterCode,
    long CurrentUsed,
    long Quota,
    long ProjectedByMonthEnd,
    bool WillExceed);

public record UsageEventDto(
    long Id,
    string MeterCode,
    DateTime OccurredAtUtc,
    long Delta,
    string? SourceEntityType,
    long? SourceEntityId,
    DateTime CreatedAtUtc);

public interface IUsageMeterService
{
    /// <summary>Checks whether adding <paramref name="delta"/> would breach the quota. Does not persist anything.</summary>
    Task<QuotaCheckResult> CheckQuotaAsync(string meterCode, long delta = 1, CancellationToken ct = default);

    /// <summary>Records usage and returns the new quota status. Creates the meter row if needed.</summary>
    Task<Result<QuotaStatus>> IncrementAsync(
        string meterCode,
        long delta = 1,
        string? sourceEntityType = null,
        long? sourceEntityId = null,
        long? triggeredByUserId = null,
        CancellationToken ct = default);

    /// <summary>Returns all active meters for the current shop.</summary>
    Task<IReadOnlyList<UsageMeterDto>> GetCurrentUsageAsync(CancellationToken ct = default);

    /// <summary>Returns historical meter readings for the past <paramref name="months"/> billing periods.</summary>
    Task<IReadOnlyList<UsageMeterDto>> GetHistoryAsync(string? meterCode, int months = 6, CancellationToken ct = default);

    /// <summary>Projects end-of-month usage for every monthly meter based on the current daily rate.</summary>
    Task<IReadOnlyList<UsageForecastDto>> GetForecastAsync(CancellationToken ct = default);

    /// <summary>Returns paged usage events (audit trail) for the current shop.</summary>
    Task<(IReadOnlyList<UsageEventDto> Items, int TotalCount)> GetEventsAsync(
        string? meterCode,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
