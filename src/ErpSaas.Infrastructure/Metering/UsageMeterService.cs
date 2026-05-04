#pragma warning disable CS9107
using Dapper;
using ErpSaas.Infrastructure.Dapper;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Metering;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Metering;

public sealed class UsageMeterService(
    TenantDbContext db,
    PlatformDbContext platform,
    ITenantContext tenantContext,
    IDapperContext dapper,
    IErrorLogger errorLogger)
    : BaseService<TenantDbContext>(db, errorLogger), IUsageMeterService
{
    // Sentinel dates used for non-resetting (persistent) meters.
    private static readonly DateTime PersistentStart = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PersistentEnd   = new(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public async Task<QuotaCheckResult> CheckQuotaAsync(
        string meterCode, long delta = 1, CancellationToken ct = default)
    {
        var shopId = tenantContext.ShopId;
        var (periodStart, _) = GetPeriod(meterCode);

        var meter = await db.Set<UsageMeter>()
            .FirstOrDefaultAsync(m => m.MeterCode == meterCode && m.PeriodStartUtc == periodStart, ct);

        long quota;
        bool hardCap;
        long used;

        if (meter is null)
        {
            (quota, hardCap, _) = await GetQuotaAsync(meterCode, shopId, ct);
            used = 0;
        }
        else
        {
            quota = meter.Quota;
            hardCap = meter.HardCapEnforced;
            used = meter.Used;
        }

        if (quota == 0)
            return new QuotaCheckResult(QuotaCheckStatus.Allow, used, quota);

        if (hardCap && used + delta > quota)
            return new QuotaCheckResult(QuotaCheckStatus.Deny, used, quota,
                $"Quota exceeded for '{meterCode}' ({used}/{quota}).");

        if ((double)(used + delta) / quota >= 0.8)
            return new QuotaCheckResult(QuotaCheckStatus.Warn, used, quota);

        return new QuotaCheckResult(QuotaCheckStatus.Allow, used, quota);
    }

    public async Task<Result<QuotaStatus>> IncrementAsync(
        string meterCode,
        long delta = 1,
        string? sourceEntityType = null,
        long? sourceEntityId = null,
        long? triggeredByUserId = null,
        CancellationToken ct = default)
        => await ExecuteAsync<QuotaStatus>("Metering.Increment", async () =>
        {
            var shopId = tenantContext.ShopId;
            var now = DateTime.UtcNow;
            var (periodStart, periodEnd) = GetPeriod(meterCode);

            var meter = await db.Set<UsageMeter>()
                .FirstOrDefaultAsync(m => m.MeterCode == meterCode && m.PeriodStartUtc == periodStart, ct);

            if (meter is null)
            {
                var (quota, hardCap, overageRate) = await GetQuotaAsync(meterCode, shopId, ct);
                meter = new UsageMeter
                {
                    ShopId = shopId,
                    MeterCode = meterCode,
                    PeriodStartUtc = periodStart,
                    PeriodEndUtc = periodEnd,
                    Used = 0,
                    Quota = quota,
                    HardCapEnforced = hardCap,
                    OverageCount = 0,
                    OverageChargeRate = overageRate,
                    CreatedAtUtc = now,
                };
                db.Set<UsageMeter>().Add(meter);
                await db.SaveChangesAsync(ct);
            }

            meter.Used += delta;
            meter.UpdatedAtUtc = now;
            if (meter.Used > meter.Quota && meter.Quota > 0)
                meter.OverageCount += delta;

            db.Set<UsageEvent>().Add(new UsageEvent
            {
                ShopId = shopId,
                MeterCode = meterCode,
                OccurredAtUtc = now,
                Delta = delta,
                SourceEntityType = sourceEntityType,
                SourceEntityId = sourceEntityId,
                TriggeredByUserId = triggeredByUserId,
                CreatedAtUtc = now,
            });

            await db.SaveChangesAsync(ct);

            var status = meter.Quota == 0
                ? QuotaStatus.Ok
                : meter.HardCapEnforced && meter.Used > meter.Quota
                    ? QuotaStatus.HardCapReached
                    : meter.Used > meter.Quota
                        ? QuotaStatus.OverQuota
                        : (double)meter.Used / meter.Quota >= 0.8
                            ? QuotaStatus.Warning
                            : QuotaStatus.Ok;

            return Result<QuotaStatus>.Success(status);
        }, ct, useTransaction: true);

    public async Task<IReadOnlyList<UsageMeterDto>> GetCurrentUsageAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var meters = await db.Set<UsageMeter>()
            .Where(m => !m.IsDeleted
                && (m.PeriodStartUtc == PersistentStart || m.PeriodStartUtc == monthStart))
            .OrderBy(m => m.MeterCode)
            .Select(m => new UsageMeterDto(
                m.MeterCode, m.Used, m.Quota,
                m.HardCapEnforced, m.PeriodStartUtc, m.PeriodEndUtc))
            .ToListAsync(ct);

        return meters;
    }

    public async Task<IReadOnlyList<UsageMeterDto>> GetHistoryAsync(
        string? meterCode, int months = 6, CancellationToken ct = default)
    {
        var cutoff = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-months);

        var query = db.Set<UsageMeter>()
            .Where(m => !m.IsDeleted && m.PeriodStartUtc >= cutoff);

        if (!string.IsNullOrWhiteSpace(meterCode))
            query = query.Where(m => m.MeterCode == meterCode);

        return await query
            .OrderBy(m => m.MeterCode).ThenBy(m => m.PeriodStartUtc)
            .Select(m => new UsageMeterDto(
                m.MeterCode, m.Used, m.Quota,
                m.HardCapEnforced, m.PeriodStartUtc, m.PeriodEndUtc))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<UsageForecastDto>> GetForecastAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var daysElapsed = (now - monthStart).TotalDays;
        var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);

        var monthlyCodes = MeterCodes.AllMonthly;
        var meters = await db.Set<UsageMeter>()
            .Where(m => !m.IsDeleted
                && monthlyCodes.Contains(m.MeterCode)
                && m.PeriodStartUtc == monthStart)
            .ToListAsync(ct);

        var forecasts = meters.Select(m =>
        {
            var dailyRate = daysElapsed > 0 ? m.Used / daysElapsed : 0;
            var projected = (long)Math.Ceiling(dailyRate * daysInMonth);
            return new UsageForecastDto(
                m.MeterCode, m.Used, m.Quota, projected,
                m.Quota > 0 && projected > m.Quota);
        }).ToList();

        return forecasts;
    }

    public async Task<(IReadOnlyList<UsageEventDto> Items, int TotalCount)> GetEventsAsync(
        string? meterCode,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var shopId = tenantContext.ShopId;
        var offset = (page - 1) * pageSize;

        var whereClause = "WHERE ShopId = @ShopId AND IsDeleted = 0";
        if (!string.IsNullOrWhiteSpace(meterCode)) whereClause += " AND MeterCode = @MeterCode";
        if (from.HasValue)  whereClause += " AND OccurredAtUtc >= @From";
        if (to.HasValue)    whereClause += " AND OccurredAtUtc <= @To";

        var countSql = $"SELECT COUNT(1) FROM [metering].[UsageEvent] {whereClause}";
        var dataSql  = $"""
            SELECT Id, MeterCode, OccurredAtUtc, Delta, SourceEntityType, SourceEntityId, CreatedAtUtc
            FROM [metering].[UsageEvent]
            {whereClause}
            ORDER BY OccurredAtUtc DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var p = new DynamicParameters();
        p.Add("ShopId",   shopId);
        p.Add("MeterCode", meterCode);
        p.Add("From",     from);
        p.Add("To",       to);
        p.Add("Offset",   offset);
        p.Add("PageSize", pageSize);

        var totalCount = await dapper.Connection.ExecuteScalarAsync<int>(
            countSql, p, dapper.CurrentTransaction);
        var rows = await dapper.Connection.QueryAsync<UsageEventDto>(
            dataSql, p, dapper.CurrentTransaction);

        return (rows.ToList(), totalCount);
    }

    private static (DateTime start, DateTime end) GetPeriod(string meterCode)
    {
        if (!MeterCodes.IsMonthly(meterCode))
            return (PersistentStart, PersistentEnd);

        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);
        return (start, end);
    }

    private async Task<(long quota, bool hardCap, decimal overageRate)> GetQuotaAsync(
        string meterCode, long shopId, CancellationToken ct)
    {
        var plan = await platform.ShopSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.ShopId == shopId && s.IsActive)
            .OrderByDescending(s => s.StartsAtUtc)
            .Select(s => s.Plan)
            .FirstOrDefaultAsync(ct);

        if (plan is null)
            return (0, false, 0m);

        return meterCode switch
        {
            MeterCodes.Invoices     => (plan.MaxInvoicesPerMonth, false, 0m),
            MeterCodes.Products     => (plan.MaxProducts, true, 0m),
            MeterCodes.ActiveUsers  => (plan.MaxUsers, true, 0m),
            MeterCodes.SmsPerMonth  => (plan.SmsQuotaPerMonth, false, 0m),
            MeterCodes.EmailPerMonth => (plan.EmailQuotaPerMonth, false, 0m),
            MeterCodes.StorageMb    => (plan.StorageQuotaMb, false, 0m),
            _                       => (0, false, 0m),
        };
    }
}
