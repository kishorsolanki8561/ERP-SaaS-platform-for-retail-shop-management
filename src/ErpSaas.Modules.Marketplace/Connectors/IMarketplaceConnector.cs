using ErpSaas.Modules.Marketplace.Entities;

namespace ErpSaas.Modules.Marketplace.Connectors;

public interface IMarketplaceConnector
{
    string MarketplaceCode { get; }
    Task<int> FetchOrdersAsync(MarketplaceAccount account, CancellationToken ct = default);
    Task<int> PushInventoryAsync(MarketplaceAccount account, CancellationToken ct = default);
    Task<int> PushPricesAsync(MarketplaceAccount account, CancellationToken ct = default);
}
