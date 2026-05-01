using ErpSaas.Modules.Accounting.Services;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Accounting.Jobs;

public sealed class StaleChequeJob(
    IChequeService chequeService,
    ILogger<StaleChequeJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        logger.LogInformation("StaleChequeJob: scanning for stale-dated cheques");
        await chequeService.MarkStaleDatedAsync(ct);
    }
}
