using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Marketplace.Entities;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Marketplace.Services;

public sealed class MarketplaceAccountService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), IMarketplaceAccountService
{
    public async Task<IReadOnlyList<MarketplaceAccountDto>> ListAsync(CancellationToken ct = default)
        => await _db.Set<MarketplaceAccount>()
            .OrderBy(a => a.AccountName)
            .Select(a => Map(a))
            .ToListAsync(ct);

    public async Task<Result<long>> CreateAsync(CreateMarketplaceAccountDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Marketplace.Account.Create", async () =>
        {
            var entity = new MarketplaceAccount
            {
                ShopId = tenant.ShopId,
                MarketplaceCode = dto.MarketplaceCode,
                AccountName = dto.AccountName,
                SellerId = dto.SellerId,
                CredentialsJsonEncrypted = dto.CredentialsJson,
                SyncInventory = dto.SyncInventory,
                SyncPricing = dto.SyncPricing,
                SyncOrders = dto.SyncOrders,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<MarketplaceAccount>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> UpdateAsync(long id, UpdateMarketplaceAccountDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Marketplace.Account.Update", async () =>
        {
            var entity = await _db.Set<MarketplaceAccount>().FirstOrDefaultAsync(a => a.Id == id, ct);
            if (entity is null) return Result<bool>.NotFound(Errors.Marketplace.AccountNotFound);

            if (dto.AccountName is not null) entity.AccountName = dto.AccountName;
            if (dto.CredentialsJson is not null) entity.CredentialsJsonEncrypted = dto.CredentialsJson;
            if (dto.SyncInventory.HasValue) entity.SyncInventory = dto.SyncInventory.Value;
            if (dto.SyncPricing.HasValue) entity.SyncPricing = dto.SyncPricing.Value;
            if (dto.SyncOrders.HasValue) entity.SyncOrders = dto.SyncOrders.Value;
            if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }

    public async Task<Result<bool>> TestConnectionAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("Marketplace.Account.TestConnection", async () =>
        {
            var entity = await _db.Set<MarketplaceAccount>().FirstOrDefaultAsync(a => a.Id == id, ct);
            if (entity is null) return Result<bool>.NotFound(Errors.Marketplace.AccountNotFound);

            // Stub: real connector delegates to IMarketplaceConnector.TestConnectionAsync
            // Each marketplace connector verifies credentials via ThirdPartyApiClientBase
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }

    private static MarketplaceAccountDto Map(MarketplaceAccount a) => new(
        a.Id, a.MarketplaceCode, a.AccountName, a.SellerId,
        a.SyncInventory, a.SyncPricing, a.SyncOrders,
        a.LastSyncUtc, a.IsActive);
}
