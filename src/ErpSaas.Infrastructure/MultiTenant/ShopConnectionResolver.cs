using ErpSaas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace ErpSaas.Infrastructure.MultiTenant;

public sealed class ShopConnectionResolver(
    PlatformDbContext platform,
    IConfiguration configuration,
    IMemoryCache cache) : IShopConnectionResolver
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<string> ResolveAsync(long shopId, CancellationToken ct = default)
    {
        var cacheKey = $"shop_conn_{shopId}";
        if (cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
            return cached;

        var subscription = await platform.ShopSubscriptions
            .Where(s => s.ShopId == shopId && s.IsActive)
            .OrderByDescending(s => s.StartsAtUtc)
            .Select(s => new { s.Plan.Code })
            .FirstOrDefaultAsync(ct);

        var connString = subscription?.Code is "Enterprise"
            ? configuration.GetConnectionString($"TenantDb_Shop_{shopId}")
              ?? configuration.GetConnectionString("TenantDb")!
            : configuration.GetConnectionString("TenantDb")!;

        cache.Set(cacheKey, connString, CacheTtl);
        return connString;
    }
}
