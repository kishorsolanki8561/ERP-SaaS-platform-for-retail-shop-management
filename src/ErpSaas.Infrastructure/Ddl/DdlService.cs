using ErpSaas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ErpSaas.Infrastructure.Ddl;

public sealed class DdlService(PlatformDbContext platformDb, TenantDbContext tenantDb, IMemoryCache cache)
    : IDdlService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<DdlItemDto>> GetItemsAsync(
        string key,
        long shopId,
        string? parentCode,
        CancellationToken ct)
    {
        var cacheKey = $"ddl:{key}:{shopId}:{parentCode ?? "__all__"}";

        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            var baseItems = await platformDb.DdlItems
                .Include(i => i.Catalog)
                .Where(i => i.Catalog.Key == key
                            && i.IsActive
                            && (parentCode == null || i.ParentCode == parentCode))
                .OrderBy(i => i.SortOrder)
                .Select(i => new DdlItemDto(i.Code, i.Label, i.SortOrder, i.ParentCode))
                .ToListAsync(ct);

            var overrides = await tenantDb.DdlItemsTenant
                .Where(o => o.CatalogKey == key && o.IsActive)
                .ToListAsync(ct);

            if (overrides.Count == 0)
                return (IReadOnlyList<DdlItemDto>)baseItems;

            var overrideMap = overrides.ToDictionary(o => o.Code, o => o.LabelOverride);

            return baseItems
                .Select(i => overrideMap.TryGetValue(i.Code, out var label)
                    ? i with { Label = label }
                    : i)
                .ToList();
        }) ?? [];
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<DdlItemDto>>> GetBatchAsync(
        IEnumerable<string> keys,
        long shopId,
        CancellationToken ct)
    {
        var result = new Dictionary<string, IReadOnlyList<DdlItemDto>>();
        foreach (var key in keys)
            result[key] = await GetItemsAsync(key, shopId, null, ct);
        return result;
    }
}
