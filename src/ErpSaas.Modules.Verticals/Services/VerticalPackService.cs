using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Verticals;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Verticals.Entities;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Verticals.Services;

public sealed class VerticalPackService(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(tenantDb, errorLogger), IVerticalPackService
{
    public async Task<IReadOnlyList<VerticalPackDto>> ListPacksAsync(CancellationToken ct = default)
    {
        return await platformDb.VerticalPacks
            .Where(v => v.IsActive && !v.IsDeleted)
            .OrderBy(v => v.SortOrder)
            .Select(v => new VerticalPackDto(v.Id, v.Code, v.Name, v.Description, v.FeatureFlagsCsv, v.IconClass, v.SortOrder, v.IsActive))
            .ToListAsync(ct);
    }

    public async Task<VerticalPackDto?> GetPackAsync(string code, CancellationToken ct = default)
    {
        var v = await platformDb.VerticalPacks
            .FirstOrDefaultAsync(x => x.Code == code && !x.IsDeleted, ct);
        return v is null ? null
            : new VerticalPackDto(v.Id, v.Code, v.Name, v.Description, v.FeatureFlagsCsv, v.IconClass, v.SortOrder, v.IsActive);
    }

    public async Task<ShopVerticalDto?> GetShopVerticalAsync(CancellationToken ct = default)
    {
        var sv = await _db.Set<ShopVertical>()
            .FirstOrDefaultAsync(ct);
        if (sv is null) return null;

        var pack = await platformDb.VerticalPacks
            .FirstOrDefaultAsync(p => p.Id == sv.VerticalPackId, ct);

        return new ShopVerticalDto(sv.Id, sv.VerticalPackId, sv.VerticalPackCode,
            pack?.Name ?? sv.VerticalPackCode, sv.AppliedAtUtc);
    }

    public async Task<Result<long>> InstallForShopAsync(string packCode, CancellationToken ct = default)
    {
        return await ExecuteAsync("Verticals.Install", async () =>
        {
            var pack = await platformDb.VerticalPacks
                .FirstOrDefaultAsync(p => p.Code == packCode && p.IsActive && !p.IsDeleted, ct);
            if (pack is null) return Result<long>.NotFound(Errors.Verticals.PackNotFound);

            var existing = await _db.Set<ShopVertical>().FirstOrDefaultAsync(ct);
            if (existing is not null)
            {
                existing.VerticalPackId = pack.Id;
                existing.VerticalPackCode = pack.Code;
                existing.AppliedAtUtc = DateTime.UtcNow;
                existing.AppliedByUserId = tenant.CurrentUserId;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                var sv = new ShopVertical
                {
                    ShopId = tenant.ShopId,
                    VerticalPackId = pack.Id,
                    VerticalPackCode = pack.Code,
                    AppliedAtUtc = DateTime.UtcNow,
                    AppliedByUserId = tenant.CurrentUserId,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                _db.Set<ShopVertical>().Add(sv);
                existing = sv;
            }

            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(existing.Id);
        }, ct, useTransaction: true);
    }
}
