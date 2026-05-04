using ErpSaas.Modules.Marketplace.Connectors;
using ErpSaas.Modules.Marketplace.Entities;
using ErpSaas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Marketplace.Jobs;

public sealed class MarketplacePriceSyncJob(
    TenantDbContext db,
    IEnumerable<IMarketplaceConnector> connectors,
    ILogger<MarketplacePriceSyncJob> logger)
{
    public const string JobId = "marketplace-price-sync";

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var accounts = await db.Set<MarketplaceAccount>()
            .IgnoreQueryFilters()
            .Where(a => a.IsActive && a.SyncPricing)
            .ToListAsync(ct);

        foreach (var account in accounts)
        {
            var connector = connectors.FirstOrDefault(c => c.MarketplaceCode == account.MarketplaceCode);
            if (connector is null) continue;

            try
            {
                var synced = await connector.PushPricesAsync(account, ct);
                logger.LogInformation("Pushed {Count} price updates to {Code} account {Id}", synced, account.MarketplaceCode, account.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Price sync failed for account {Id} ({Code})", account.Id, account.MarketplaceCode);
            }
        }
    }
}
