using ErpSaas.Modules.Accounting.Services;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Accounting.Jobs;

public sealed class DepreciationJob(
    IFixedAssetService fixedAssetService,
    ILogger<DepreciationJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        // Run for the first day of the current month so re-runs within the same month are idempotent
        var periodDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        logger.LogInformation("DepreciationJob: running monthly depreciation for {Period}", periodDate);
        await fixedAssetService.RunDepreciationAsync(periodDate, ct);
    }
}
