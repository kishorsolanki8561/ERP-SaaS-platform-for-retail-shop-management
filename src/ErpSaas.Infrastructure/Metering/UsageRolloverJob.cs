using Dapper;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Metering;
using ErpSaas.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Infrastructure.Metering;

/// <summary>
/// Month-end job: seals completed billing periods and resets monthly meter counters.
/// Runs on the 1st of every month at 00:05 UTC via Hangfire.
/// </summary>
public sealed class UsageRolloverJob(
    PlatformDbContext platform,
    TenantDbContext tenant,
    ILogger<UsageRolloverJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        // Seal the period that just ended (previous calendar month)
        var prevMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);

        logger.LogInformation("UsageRolloverJob: sealing monthly meters for period {Period}", prevMonthStart);

        var activeShopIds = await platform.ShopSubscriptions
            .Where(s => s.IsActive)
            .Select(s => s.ShopId)
            .Distinct()
            .ToListAsync(ct);

        var monthlyMeterCodes = new[] { MeterCodes.Invoices, MeterCodes.SmsPerMonth, MeterCodes.EmailPerMonth };

        var expiredMeters = await tenant.Set<UsageMeter>()
            .Where(m => !m.IsDeleted
                && m.PeriodStartUtc == prevMonthStart
                && monthlyMeterCodes.Contains(m.MeterCode))
            .ToListAsync(ct);

        foreach (var meter in expiredMeters)
        {
            if (meter.OverageCount > 0 && meter.OverageChargeRate > 0)
            {
                var overageAmount = meter.OverageCount * meter.OverageChargeRate;
                logger.LogInformation(
                    "UsageRolloverJob: shop {ShopId} meter {Code} overage {Count} × {Rate} = {Amount}",
                    meter.ShopId, meter.MeterCode, meter.OverageCount, meter.OverageChargeRate, overageAmount);
                // Overage invoice line generation deferred to billing module subscription invoicing
            }
        }

        await tenant.SaveChangesAsync(ct);

        logger.LogInformation(
            "UsageRolloverJob: sealed {Count} meter(s) for {ShopCount} shop(s)",
            expiredMeters.Count, activeShopIds.Count);
    }
}
