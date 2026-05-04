using ErpSaas.Infrastructure.Data;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ErpSaas.Modules.Identity.Controllers;

[Route("api/catalog")]
[AllowAnonymous]
public sealed class CatalogController(
    PlatformDbContext db,
    IMemoryCache cache) : BaseController
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
    {
        var data = await cache.GetOrCreateAsync("catalog:plans", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await db.SubscriptionPlans
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Include(p => p.Features)
                .OrderBy(p => p.MonthlyPrice)
                .Select(p => new
                {
                    p.Id,
                    p.Code,
                    p.Label,
                    p.MonthlyPrice,
                    p.AnnualPrice,
                    p.MaxUsers,
                    p.MaxProducts,
                    p.MaxInvoicesPerMonth,
                    p.StorageQuotaMb,
                    Features = p.Features.Select(f => f.FeatureCode).ToList()
                })
                .ToListAsync(ct);
        });
        return Ok(data);
    }

    [HttpGet("modules")]
    public async Task<IActionResult> GetModules(CancellationToken ct)
    {
        var data = await cache.GetOrCreateAsync("catalog:modules", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            // Return distinct module names from the permissions catalog
            return await db.Permissions
                .AsNoTracking()
                .Select(p => p.Module)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync(ct);
        });
        return Ok(data);
    }

    [HttpGet("features")]
    public async Task<IActionResult> GetFeatures(CancellationToken ct)
    {
        var data = await cache.GetOrCreateAsync("catalog:features", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await db.SubscriptionPlanFeatures
                .AsNoTracking()
                .Select(f => f.FeatureCode)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync(ct);
        });
        return Ok(data);
    }

    [HttpGet("verticals")]
    public async Task<IActionResult> GetVerticals(CancellationToken ct)
    {
        var data = await cache.GetOrCreateAsync("catalog:verticals", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var catalog = await db.DdlCatalogs
                .AsNoTracking()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Key == "VERTICAL", ct);

            return catalog?.Items
                .Where(i => i.IsActive)
                .OrderBy(i => i.SortOrder)
                .Select(i => new { i.Code, i.Label })
                .ToList();
        });
        return Ok(data ?? []);
    }

    [HttpGet("integrations")]
    public async Task<IActionResult> GetIntegrations(CancellationToken ct)
    {
        var data = await cache.GetOrCreateAsync("catalog:integrations", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var catalog = await db.DdlCatalogs
                .AsNoTracking()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Key == "MARKETPLACE", ct);

            return catalog?.Items
                .Where(i => i.IsActive)
                .OrderBy(i => i.SortOrder)
                .Select(i => new { i.Code, i.Label })
                .ToList();
        });
        return Ok(data ?? []);
    }
}
