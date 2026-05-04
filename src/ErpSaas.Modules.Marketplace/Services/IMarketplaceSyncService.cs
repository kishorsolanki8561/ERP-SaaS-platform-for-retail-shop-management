using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Marketplace.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record MarketplaceProductMappingDto(
    long Id, long MarketplaceAccountId, long ProductId, long? ProductVariantId,
    string MarketplaceSku, string MarketplaceListingId, decimal? PriceOverride, bool IsActive);

public record LinkProductDto(
    long MarketplaceAccountId, long ProductId, long? ProductVariantId,
    string MarketplaceSku, string MarketplaceListingId, decimal? PriceOverride);

public record SyncResultDto(int AccountsProcessed, int OrdersFetched, int ItemsSynced, IReadOnlyList<string> Errors);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IMarketplaceSyncService
{
    Task<IReadOnlyList<MarketplaceProductMappingDto>> ListProductMappingsAsync(CancellationToken ct = default);
    Task<Result<long>> LinkProductAsync(LinkProductDto dto, CancellationToken ct = default);
    Task<Result<SyncResultDto>> SyncOrdersAsync(CancellationToken ct = default);
    Task<Result<SyncResultDto>> SyncInventoryAsync(CancellationToken ct = default);
    Task<Result<SyncResultDto>> SyncPricesAsync(CancellationToken ct = default);
}
