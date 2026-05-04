using ErpSaas.Modules.Marketplace.Connectors;
using ErpSaas.Modules.Marketplace.Entities;
using ErpSaas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Marketplace.Jobs;

public sealed class MarketplaceOrderPollingJob(
    TenantDbContext db,
    IEnumerable<IMarketplaceConnector> connectors,
    ILogger<MarketplaceOrderPollingJob> logger)
{
    public const string JobId = "marketplace-order-polling";

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var accounts = await db.Set<MarketplaceAccount>()
            .IgnoreQueryFilters()
            .Where(a => a.IsActive && a.SyncOrders)
            .ToListAsync(ct);

        foreach (var account in accounts)
        {
            var connector = connectors.FirstOrDefault(c => c.MarketplaceCode == account.MarketplaceCode);
            if (connector is null)
            {
                logger.LogWarning("No connector registered for marketplace {Code}", account.MarketplaceCode);
                continue;
            }

            try
            {
                var fetched = await connector.FetchOrdersAsync(account, ct);
                account.LastSyncUtc = DateTime.UtcNow;
                logger.LogInformation("Polled {Count} orders from {Code} account {Id}", fetched, account.MarketplaceCode, account.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Order polling failed for account {Id} ({Code})", account.Id, account.MarketplaceCode);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
