using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Marketplace.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record MarketplaceAccountDto(
    long Id, string MarketplaceCode, string AccountName, string SellerId,
    bool SyncInventory, bool SyncPricing, bool SyncOrders,
    DateTime? LastSyncUtc, bool IsActive);

public record CreateMarketplaceAccountDto(
    string MarketplaceCode, string AccountName, string SellerId,
    string CredentialsJson,
    bool SyncInventory, bool SyncPricing, bool SyncOrders);

public record UpdateMarketplaceAccountDto(
    string? AccountName, string? CredentialsJson,
    bool? SyncInventory, bool? SyncPricing, bool? SyncOrders, bool? IsActive);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IMarketplaceAccountService
{
    Task<IReadOnlyList<MarketplaceAccountDto>> ListAsync(CancellationToken ct = default);
    Task<Result<long>> CreateAsync(CreateMarketplaceAccountDto dto, CancellationToken ct = default);
    Task<Result<bool>> UpdateAsync(long id, UpdateMarketplaceAccountDto dto, CancellationToken ct = default);
    Task<Result<bool>> TestConnectionAsync(long id, CancellationToken ct = default);
}
