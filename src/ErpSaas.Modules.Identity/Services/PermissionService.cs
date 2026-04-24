using ErpSaas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ErpSaas.Modules.Identity.Services;

public sealed class PermissionService(PlatformDbContext db, IMemoryCache cache) : IPermissionService
{
    private static string PermKey(long userId, long shopId) => $"perms:{userId}:{shopId}";
    private static string FeatKey(long shopId) => $"feats:{shopId}";

    public async Task<IReadOnlyList<string>> GetPermissionCodesAsync(
        long userId, long shopId, CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync(PermKey(userId, shopId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            var codes = await db.UserRoles
                .Where(ur => ur.UserId == userId && ur.ShopId == shopId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Code)
                .Distinct()
                .ToListAsync(ct);

            return (IReadOnlyList<string>)codes;
        }) ?? [];
    }

    public async Task<IReadOnlyList<string>> GetFeatureCodesAsync(
        long shopId, CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync(FeatKey(shopId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            var codes = await db.ShopSubscriptions
                .Where(ss => ss.ShopId == shopId && ss.IsActive)
                .SelectMany(ss => ss.Plan.Features)
                .Select(f => f.FeatureCode)
                .Distinct()
                .ToListAsync(ct);

            return (IReadOnlyList<string>)codes;
        }) ?? [];
    }
}
