using ErpSaas.Infrastructure.Http;
using ErpSaas.Modules.Marketplace.Entities;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Marketplace.Connectors;

public sealed class FlipkartConnector(
    HttpClient httpClient,
    ILogger<FlipkartConnector> logger)
    : ThirdPartyApiClientBase(httpClient), IMarketplaceConnector
{
    public string MarketplaceCode => "Flipkart";

    public async Task<int> FetchOrdersAsync(MarketplaceAccount account, CancellationToken ct = default)
    {
        // Stub: real impl calls Flipkart Seller API GET /orders/list with FSN-based auth
        // Each call logged automatically by ThirdPartyApiClientBase via ThirdPartyApiLog
        logger.LogInformation("Flipkart API: fetching orders for account {Id}", account.Id);
        await Task.CompletedTask;
        return 0;
    }

    public async Task<int> PushInventoryAsync(MarketplaceAccount account, CancellationToken ct = default)
    {
        logger.LogInformation("Flipkart API: pushing inventory for account {Id}", account.Id);
        await Task.CompletedTask;
        return 0;
    }

    public async Task<int> PushPricesAsync(MarketplaceAccount account, CancellationToken ct = default)
    {
        logger.LogInformation("Flipkart API: pushing prices for account {Id}", account.Id);
        await Task.CompletedTask;
        return 0;
    }
}
