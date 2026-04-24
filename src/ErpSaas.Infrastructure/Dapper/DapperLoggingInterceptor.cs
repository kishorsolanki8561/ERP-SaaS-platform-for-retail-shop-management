using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Log;
using ErpSaas.Shared.Data;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Infrastructure.Dapper;

/// <summary>
/// Wraps Dapper calls to record any query slower than SlowQueryThresholdMs into SlowQueryLog.
/// Usage: await interceptor.QueryAsync(() => conn.QueryAsync(...), sql, ct: ct);
/// </summary>
public sealed class DapperLoggingInterceptor(
    LogDbContext logDb,
    ITenantContext tenantContext,
    ILogger<DapperLoggingInterceptor> logger)
{
    private const int SlowQueryThresholdMs = 500;

    public async Task<IEnumerable<T>> QueryAsync<T>(
        Func<Task<IEnumerable<T>>> query,
        string sql,
        CancellationToken ct = default,
        [CallerMemberName] string? callerName = null)
    {
        var sw = Stopwatch.StartNew();
        try { return await query(); }
        finally
        {
            sw.Stop();
            if (sw.ElapsedMilliseconds >= SlowQueryThresholdMs)
                await LogSlowAsync(sql, sw.ElapsedMilliseconds, callerName, ct);
        }
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(
        Func<Task<T?>> query,
        string sql,
        CancellationToken ct = default,
        [CallerMemberName] string? callerName = null)
    {
        var sw = Stopwatch.StartNew();
        try { return await query(); }
        finally
        {
            sw.Stop();
            if (sw.ElapsedMilliseconds >= SlowQueryThresholdMs)
                await LogSlowAsync(sql, sw.ElapsedMilliseconds, callerName, ct);
        }
    }

    private async Task LogSlowAsync(string sql, long elapsedMs, string? callerName, CancellationToken ct)
    {
        try
        {
            logDb.SlowQueryLogs.Add(new SlowQueryLog
            {
                Sql = sql.Length > 4000 ? sql[..4000] : sql,
                ElapsedMs = elapsedMs,
                ShopId = tenantContext.ShopId == 0 ? null : tenantContext.ShopId,
                CallerName = callerName,
                OccurredAtUtc = DateTime.UtcNow,
            });
            await logDb.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record slow query log");
        }
    }
}
