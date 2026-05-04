using ErpSaas.Infrastructure.Http;
using ErpSaas.Modules.Marketplace.Entities;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Marketplace.Connectors;

public sealed class AmazonSpApiConnector(
    HttpClient httpClient,
    ILogger<AmazonSpApiConnector> logger)
    : ThirdPartyApiClientBase(httpClient), IMarketplaceConnector
{
    public string MarketplaceCode => "Amazon";

    public async Task<int> FetchOrdersAsync(MarketplaceAccount account, CancellationToken ct = default)
    {
        // Stub: real impl calls SP-API GET /orders/v0/orders with OAuth token
        // Each call logged automatically by ThirdPartyApiClientBase via ThirdPartyApiLog
        logger.LogInformation("Amazon SP-API: fetching orders for account {Id}", account.Id);
        await Task.CompletedTask;
        return 0;
    }

    public async Task<int> PushInventoryAsync(MarketplaceAccount account, CancellationToken ct = default)
    {
        logger.LogInformation("Amazon SP-API: pushing inventory for account {Id}", account.Id);
        await Task.CompletedTask;
        return 0;
    }

    public async Task<int> PushPricesAsync(MarketplaceAccount account, CancellationToken ct = default)
    {
        logger.LogInformation("Amazon SP-API: pushing prices for account {Id}", account.Id);
        await Task.CompletedTask;
        return 0;
    }
}
