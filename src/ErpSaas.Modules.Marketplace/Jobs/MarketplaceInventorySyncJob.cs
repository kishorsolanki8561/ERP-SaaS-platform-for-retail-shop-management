using ErpSaas.Modules.Marketplace.Connectors;
using ErpSaas.Modules.Marketplace.Entities;
using ErpSaas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Marketplace.Jobs;

public sealed class MarketplaceInventorySyncJob(
    TenantDbContext db,
    IEnumerable<IMarketplaceConnector> connectors,
    ILogger<MarketplaceInventorySyncJob> logger)
{
    public const string JobId = "marketplace-inventory-sync";

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var accounts = await db.Set<MarketplaceAccount>()
            .IgnoreQueryFilters()
            .Where(a => a.IsActive && a.SyncInventory)
            .ToListAsync(ct);

        foreach (var account in accounts)
        {
            var connector = connectors.FirstOrDefault(c => c.MarketplaceCode == account.MarketplaceCode);
            if (connector is null) continue;

            try
            {
                var synced = await connector.PushInventoryAsync(account, ct);
                logger.LogInformation("Pushed {Count} inventory items to {Code} account {Id}", synced, account.MarketplaceCode, account.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Inventory sync failed for account {Id} ({Code})", account.Id, account.MarketplaceCode);
            }
        }
    }
}
