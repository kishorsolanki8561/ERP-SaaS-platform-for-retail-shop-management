using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Marketplace.Connectors;
using ErpSaas.Modules.Marketplace.Entities;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Marketplace.Services;

public sealed class MarketplaceSyncService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant,
    IEnumerable<IMarketplaceConnector> connectors,
    ILogger<MarketplaceSyncService> logger)
    : BaseService<TenantDbContext>(db, errorLogger), IMarketplaceSyncService
{
    public async Task<IReadOnlyList<MarketplaceProductMappingDto>> ListProductMappingsAsync(CancellationToken ct = default)
        => await _db.Set<MarketplaceProductMapping>()
            .OrderBy(m => m.MarketplaceSku)
            .Select(m => new MarketplaceProductMappingDto(
                m.Id, m.MarketplaceAccountId, m.ProductId, m.ProductVariantId,
                m.MarketplaceSku, m.MarketplaceListingId, m.PriceOverride, m.IsActive))
            .ToListAsync(ct);

    public async Task<Result<long>> LinkProductAsync(LinkProductDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Marketplace.Product.Link", async () =>
        {
            var accountExists = await _db.Set<MarketplaceAccount>()
                .AnyAsync(a => a.Id == dto.MarketplaceAccountId, ct);
            if (!accountExists) return Result<long>.NotFound(Errors.Marketplace.AccountNotFound);

            var duplicate = await _db.Set<MarketplaceProductMapping>()
                .AnyAsync(m => m.MarketplaceAccountId == dto.MarketplaceAccountId
                               && m.MarketplaceSku == dto.MarketplaceSku, ct);
            if (duplicate) return Result<long>.Conflict(Errors.Marketplace.MappingAlreadyExists);

            var mapping = new MarketplaceProductMapping
            {
                ShopId = tenant.ShopId,
                MarketplaceAccountId = dto.MarketplaceAccountId,
                ProductId = dto.ProductId,
                ProductVariantId = dto.ProductVariantId,
                MarketplaceSku = dto.MarketplaceSku,
                MarketplaceListingId = dto.MarketplaceListingId,
                PriceOverride = dto.PriceOverride,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<MarketplaceProductMapping>().Add(mapping);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(mapping.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<SyncResultDto>> SyncOrdersAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync("Marketplace.Sync.Orders", async () =>
        {
            var accounts = await _db.Set<MarketplaceAccount>()
                .Where(a => a.IsActive && a.SyncOrders)
                .ToListAsync(ct);

            var totalFetched = 0;
            var errors = new List<string>();

            foreach (var account in accounts)
            {
                var connector = connectors.FirstOrDefault(c => c.MarketplaceCode == account.MarketplaceCode);
                if (connector is null)
                {
                    logger.LogWarning("No connector found for marketplace {Code}", account.MarketplaceCode);
                    continue;
                }

                try
                {
                    var count = await connector.FetchOrdersAsync(account, ct);
                    totalFetched += count;
                    account.LastSyncUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Order sync failed for account {Id}", account.Id);
                    errors.Add($"Account {account.Id}: {ex.Message}");
                }
            }

            await _db.SaveChangesAsync(ct);
            return Result<SyncResultDto>.Success(new SyncResultDto(accounts.Count, totalFetched, 0, errors));
        }, ct, useTransaction: false);
    }

    public async Task<Result<SyncResultDto>> SyncInventoryAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync("Marketplace.Sync.Inventory", async () =>
        {
            var accounts = await _db.Set<MarketplaceAccount>()
                .Where(a => a.IsActive && a.SyncInventory)
                .ToListAsync(ct);

            var totalSynced = 0;
            var errors = new List<string>();

            foreach (var account in accounts)
            {
                var connector = connectors.FirstOrDefault(c => c.MarketplaceCode == account.MarketplaceCode);
                if (connector is null) continue;

                try
                {
                    var count = await connector.PushInventoryAsync(account, ct);
                    totalSynced += count;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Inventory sync failed for account {Id}", account.Id);
                    errors.Add($"Account {account.Id}: {ex.Message}");
                }
            }

            return Result<SyncResultDto>.Success(new SyncResultDto(accounts.Count, 0, totalSynced, errors));
        }, ct, useTransaction: false);
    }

    public async Task<Result<SyncResultDto>> SyncPricesAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync("Marketplace.Sync.Prices", async () =>
        {
            var accounts = await _db.Set<MarketplaceAccount>()
                .Where(a => a.IsActive && a.SyncPricing)
                .ToListAsync(ct);

            var totalSynced = 0;
            var errors = new List<string>();

            foreach (var account in accounts)
            {
                var connector = connectors.FirstOrDefault(c => c.MarketplaceCode == account.MarketplaceCode);
                if (connector is null) continue;

                try
                {
                    var count = await connector.PushPricesAsync(account, ct);
                    totalSynced += count;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Price sync failed for account {Id}", account.Id);
                    errors.Add($"Account {account.Id}: {ex.Message}");
                }
            }

            return Result<SyncResultDto>.Success(new SyncResultDto(accounts.Count, 0, totalSynced, errors));
        }, ct, useTransaction: false);
    }
}
