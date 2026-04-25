using ErpSaas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ErpSaas.Modules.Identity.Services;

public sealed class MenuService(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    IPermissionService permissionService,
    IMemoryCache cache) : IMenuService
{
    public async Task<IReadOnlyList<MenuItemDto>> GetTreeAsync(
        long userId, long shopId, bool isPlatformAdmin = false, CancellationToken ct = default)
    {
        var cacheKey = $"menu:{userId}:{shopId}:{isPlatformAdmin}";
        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            return await BuildTreeAsync(userId, shopId, isPlatformAdmin, ct);
        }) ?? [];
    }

    private async Task<IReadOnlyList<MenuItemDto>> BuildTreeAsync(
        long userId, long shopId, bool isPlatformAdmin, CancellationToken ct)
    {
        var allItems = await platformDb.MenuItems
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(ct);

        var overrides = await tenantDb.MenuItemTenantOverrides
            .ToListAsync(ct);

        // Platform admins bypass all permission/feature gates and see every item.
        var perms = isPlatformAdmin
            ? (IReadOnlyList<string>)[]
            : await permissionService.GetPermissionCodesAsync(userId, shopId, ct);
        var feats = isPlatformAdmin
            ? (IReadOnlyList<string>)[]
            : await permissionService.GetFeatureCodesAsync(shopId, ct);

        var overrideMap = overrides.ToDictionary(o => o.MenuItemCode);

        bool IsVisible(Infrastructure.Data.Entities.Menu.MenuItem item)
        {
            if (overrideMap.TryGetValue(item.Code, out var ov) && ov.IsHidden)
                return false;

            // Platform admins see everything — skip permission/feature checks.
            if (isPlatformAdmin)
                return true;

            if (item.RequiredPermission is not null && !perms.Contains(item.RequiredPermission))
                return false;

            if (item.RequiredFeature is not null && !feats.Contains(item.RequiredFeature))
                return false;

            return true;
        }

        string GetLabel(Infrastructure.Data.Entities.Menu.MenuItem item) =>
            overrideMap.TryGetValue(item.Code, out var ov) && ov.LabelOverride is not null
                ? ov.LabelOverride
                : item.Label;

        int GetSort(Infrastructure.Data.Entities.Menu.MenuItem item) =>
            overrideMap.TryGetValue(item.Code, out var ov) && ov.SortOrderOverride.HasValue
                ? ov.SortOrderOverride.Value
                : item.SortOrder;

        var roots = allItems.Where(m => m.ParentId is null).ToList();

        IReadOnlyList<MenuItemDto> BuildChildren(long parentId)
        {
            var children = allItems
                .Where(m => m.ParentId == parentId && IsVisible(m))
                .OrderBy(GetSort)
                .Select(m => new MenuItemDto(
                    m.Code,
                    GetLabel(m),
                    m.Kind.ToString(),
                    m.Icon,
                    m.Route,
                    GetSort(m),
                    BuildChildren(m.Id)))
                .Where(dto => dto.Kind == "Page" || dto.Children.Count > 0)
                .ToList();

            return children;
        }

        var tree = roots
            .Where(IsVisible)
            .OrderBy(GetSort)
            .Select(m => new MenuItemDto(
                m.Code,
                GetLabel(m),
                m.Kind.ToString(),
                m.Icon,
                m.Route,
                GetSort(m),
                BuildChildren(m.Id)))
            .Where(dto => dto.Kind == "Page" || dto.Children.Count > 0)
            .ToList();

        return tree;
    }
}
