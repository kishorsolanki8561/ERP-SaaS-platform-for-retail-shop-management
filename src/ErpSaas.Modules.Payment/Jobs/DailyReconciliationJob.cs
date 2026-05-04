using ErpSaas.Infrastructure.Data;
using ErpSaas.Modules.Payment.Entities;
using ErpSaas.Modules.Payment.Services;
using ErpSaas.Shared.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Payment.Jobs;

public sealed class DailyReconciliationJob(
    TenantDbContext db,
    IPaymentReconciliationService reconciliation,
    ILogger<DailyReconciliationJob> logger)
{
    // Hangfire cron: runs at 02:00 UTC daily.
    // Registered by PaymentServiceExtensions with RecurringJob.AddOrUpdate.
    public static string JobId => "payment.daily-reconciliation";

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var settlementDate = DateTime.UtcNow.Date.AddDays(-1);

        var gateways = await db.Set<PaymentGatewayAccount>()
            .IgnoreQueryFilters()
            .Where(a => a.IsActive)
            .Select(a => new { a.ShopId, a.GatewayCode })
            .Distinct()
            .ToListAsync(ct);

        foreach (var g in gateways)
        {
            try
            {
                var result = await reconciliation.RunReconciliationAsync(g.GatewayCode, settlementDate, ct);
                if (result.IsSuccess)
                    logger.LogInformation("Reconciliation shop={ShopId} gateway={Gateway} date={Date}: {Count} exceptions",
                        g.ShopId, g.GatewayCode, settlementDate, result.Value);
                else
                    logger.LogWarning("Reconciliation shop={ShopId} gateway={Gateway} failed: {Errors}",
                        g.ShopId, g.GatewayCode, string.Join(", ", result.Errors));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reconciliation shop={ShopId} gateway={Gateway} threw", g.ShopId, g.GatewayCode);
            }
        }
    }
}
