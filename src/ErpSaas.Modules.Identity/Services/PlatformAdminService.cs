using Dapper;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Identity.Services;

public sealed class PlatformAdminService(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    LogDbContext logDb,
    IPermissionService permissionService,
    IErrorLogger errorLogger,
    ILogger<PlatformAdminService> logger)
    : BaseService<PlatformDbContext>(platformDb, errorLogger), IPlatformAdminService
{
    public async Task<(IReadOnlyList<ShopSummaryDto> Items, int TotalCount)> ListShopsAsync(
        int pageNumber, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = _db.Shops.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.LegalName.Contains(search) || s.ShopCode.Contains(search));

        var total = await query.CountAsync(ct);

        var shops = await query
            .OrderBy(s => s.LegalName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.ShopCode,
                s.LegalName,
                s.TradeName,
                s.IsActive,
                UserCount = s.UserShops.Count,
                PlanLabel = s.Subscriptions
                    .Where(sub => sub.IsActive)
                    .Select(sub => sub.Plan.Label)
                    .FirstOrDefault(),
                LastActivityAtUtc = (DateTime?)null
            })
            .ToListAsync(ct);

        var items = shops.Select(s => new ShopSummaryDto(
            s.Id, s.ShopCode, s.LegalName, s.TradeName,
            s.IsActive, s.PlanLabel, s.UserCount, s.LastActivityAtUtc))
            .ToList();

        return (items, total);
    }

    public async Task<ShopDetailDto?> GetShopDetailAsync(long shopId, CancellationToken ct = default)
    {
        var shop = await _db.Shops
            .AsNoTracking()
            .Where(s => s.Id == shopId)
            .Select(s => new
            {
                s.Id, s.ShopCode, s.LegalName, s.TradeName, s.GstNumber,
                s.City, s.StateCode, s.IsActive,
                UserCount = s.UserShops.Count,
                Subscription = s.Subscriptions
                    .Where(sub => sub.IsActive)
                    .Select(sub => new
                    {
                        PlanLabel = sub.Plan.Label,
                        PlanCode = sub.Plan.Code,
                        sub.StartsAtUtc,
                        sub.EndsAtUtc
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        if (shop is null) return null;

        var conn = tenantDb.Database.GetDbConnection();
        if (conn.State == System.Data.ConnectionState.Closed)
            await conn.OpenAsync(ct);

        var stats = await conn.QueryFirstAsync<ShopStatsRow>(
            """
            SELECT
                COUNT(*) AS InvoiceCount,
                ISNULL(SUM(GrandTotal), 0) AS RevenueCurrentMonth
            FROM sales.Invoice
            WHERE ShopId = @ShopId
              AND MONTH(InvoiceDate) = MONTH(GETUTCDATE())
              AND YEAR(InvoiceDate) = YEAR(GETUTCDATE())
              AND IsDeleted = 0
            """,
            new { ShopId = shopId });

        return new ShopDetailDto(
            shop.Id, shop.ShopCode, shop.LegalName, shop.TradeName,
            shop.GstNumber, shop.City, shop.StateCode, shop.IsActive,
            shop.Subscription?.PlanLabel, shop.Subscription?.PlanCode,
            shop.Subscription?.StartsAtUtc, shop.Subscription?.EndsAtUtc,
            shop.UserCount, stats.InvoiceCount, stats.RevenueCurrentMonth);
    }

    public async Task<IReadOnlyList<ShopUserDto>> ListShopUsersAsync(long shopId, CancellationToken ct = default)
    {
        var users = await _db.UserShops
            .AsNoTracking()
            .Where(us => us.ShopId == shopId)
            .Select(us => new ShopUserDto(
                us.User.Id,
                us.User.DisplayName,
                us.User.Email,
                us.User.Phone,
                us.User.IsActive,
                us.User.LastLoginAtUtc))
            .ToListAsync(ct);

        return users;
    }

    // ── Part 3: Subscription Dashboard ────────────────────────────────────────

    public async Task<SubscriptionDashboardDto> GetSubscriptionDashboardAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextWeek = now.AddDays(7);

        var subscriptions = await _db.ShopSubscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .ToListAsync(ct);

        var active  = subscriptions.Count(s => s.IsActive);
        var trial   = subscriptions.Count(s => s.IsActive && s.Plan.MonthlyPrice == 0);
        var expired = subscriptions.Count(s => !s.IsActive && s.EndsAtUtc >= monthStart);

        var mrr = subscriptions
            .Where(s => s.IsActive && s.Plan.MonthlyPrice > 0)
            .Sum(s => s.BillingCycle == BillingCycle.Annual
                ? s.Plan.AnnualPrice / 12m
                : s.Plan.MonthlyPrice);

        var totalLastMonth = subscriptions
            .Count(s => s.IsActive || (s.EndsAtUtc.HasValue && s.EndsAtUtc >= monthStart.AddMonths(-1)));
        var churnRate = totalLastMonth > 0 ? (decimal)expired / totalLastMonth * 100m : 0m;

        var upcoming = await _db.ShopSubscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Include(s => s.Shop)
            .Where(s => s.IsActive && s.EndsAtUtc.HasValue
                && s.EndsAtUtc >= now && s.EndsAtUtc <= nextWeek)
            .OrderBy(s => s.EndsAtUtc)
            .Take(20)
            .Select(s => new UpcomingRenewalDto(
                s.ShopId,
                s.Shop.LegalName,
                s.Plan.Label,
                s.BillingCycle == BillingCycle.Annual ? s.Plan.AnnualPrice : s.Plan.MonthlyPrice,
                s.EndsAtUtc!.Value))
            .ToListAsync(ct);

        return new SubscriptionDashboardDto(
            Math.Round(mrr, 2), Math.Round(mrr * 12, 2),
            active, trial, expired, Math.Round(churnRate, 1),
            upcoming);
    }

    // ── Part 3: System Health ─────────────────────────────────────────────────

    public async Task<SystemHealthDto> GetSystemHealthAsync(CancellationToken ct = default)
    {
        var dbOk = false;
        try
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
            dbOk = true;
        }
        catch { /* db unhealthy */ }

        var errorCount = 0;
        try
        {
            errorCount = await logDb.ErrorLogs
                .Where(e => e.OccurredAtUtc >= DateTime.UtcNow.AddHours(-24))
                .CountAsync(ct);
        }
        catch { /* log db unavailable */ }

        var queueDepth = 0;
        try
        {
            var logConn = logDb.Database.GetDbConnection();
            if (logConn.State == System.Data.ConnectionState.Closed)
                await logConn.OpenAsync(ct);
        }
        catch { /* Hangfire not queried */ }

        var version = typeof(PlatformAdminService).Assembly.GetName().Version?.ToString() ?? "1.0";

        return new SystemHealthDto(errorCount, queueDepth, dbOk, true, version);
    }

    // ── Part 3: Plan CRUD ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<PlatformSubscriptionPlanDto>> ListPlansAsync(CancellationToken ct = default)
    {
        var plans = await _db.SubscriptionPlans
            .AsNoTracking()
            .Include(p => p.Features)
            .OrderBy(p => p.MonthlyPrice)
            .ToListAsync(ct);

        return plans.Select(MapPlan).ToList();
    }

    public async Task<Result<long>> CreatePlanAsync(CreatePlanDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Platform.CreatePlan", async () =>
        {
            var exists = await _db.SubscriptionPlans
                .AnyAsync(p => p.Code == dto.Code, ct);
            if (exists) return Result<long>.Conflict(Errors.PlatformAdmin.PlanCodeExists);

            var plan = new SubscriptionPlan
            {
                Code                = dto.Code,
                Label               = dto.Label,
                MonthlyPrice        = dto.MonthlyPrice,
                AnnualPrice         = dto.AnnualPrice,
                MaxUsers            = dto.MaxUsers,
                MaxProducts         = dto.MaxProducts,
                MaxInvoicesPerMonth = dto.MaxInvoicesPerMonth,
                StorageQuotaMb      = dto.StorageQuotaMb,
                SmsQuotaPerMonth    = dto.SmsQuotaPerMonth,
                EmailQuotaPerMonth  = dto.EmailQuotaPerMonth,
                IsActive            = true,
                CreatedAtUtc        = DateTime.UtcNow,
            };

            foreach (var featureCode in dto.Features ?? [])
            {
                plan.Features.Add(new SubscriptionPlanFeature
                {
                    FeatureCode  = featureCode,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }

            _db.SubscriptionPlans.Add(plan);
            await _db.SaveChangesAsync(ct);

            logger.LogInformation("Created subscription plan: {Code}", dto.Code);

            return Result<long>.Success(plan.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> UpdatePlanAsync(long planId, UpdatePlanDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Platform.UpdatePlan", async () =>
        {
            var plan = await _db.SubscriptionPlans
                .Include(p => p.Features)
                .FirstOrDefaultAsync(p => p.Id == planId, ct);

            if (plan is null) return Result<bool>.NotFound(Errors.PlatformAdmin.PlanNotFound);

            if (dto.Label is not null)               plan.Label               = dto.Label;
            if (dto.MonthlyPrice.HasValue)           plan.MonthlyPrice        = dto.MonthlyPrice.Value;
            if (dto.AnnualPrice.HasValue)            plan.AnnualPrice         = dto.AnnualPrice.Value;
            if (dto.MaxUsers.HasValue)               plan.MaxUsers            = dto.MaxUsers.Value;
            if (dto.MaxProducts.HasValue)            plan.MaxProducts         = dto.MaxProducts.Value;
            if (dto.MaxInvoicesPerMonth.HasValue)    plan.MaxInvoicesPerMonth = dto.MaxInvoicesPerMonth.Value;
            if (dto.StorageQuotaMb.HasValue)         plan.StorageQuotaMb      = dto.StorageQuotaMb.Value;
            if (dto.SmsQuotaPerMonth.HasValue)       plan.SmsQuotaPerMonth    = dto.SmsQuotaPerMonth.Value;
            if (dto.EmailQuotaPerMonth.HasValue)     plan.EmailQuotaPerMonth  = dto.EmailQuotaPerMonth.Value;
            if (dto.IsActive.HasValue)               plan.IsActive            = dto.IsActive.Value;
            plan.UpdatedAtUtc = DateTime.UtcNow;

            if (dto.Features is not null)
            {
                _db.Set<SubscriptionPlanFeature>().RemoveRange(plan.Features);
                foreach (var code in dto.Features)
                {
                    _db.Set<SubscriptionPlanFeature>().Add(new SubscriptionPlanFeature
                    {
                        PlanId = plan.Id, FeatureCode = code, CreatedAtUtc = DateTime.UtcNow,
                    });
                }
            }

            await _db.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    // ── Part 3: Shop suspend / activate / feature toggle ──────────────────────

    public async Task<Result<bool>> ToggleShopFeatureAsync(long shopId, ToggleFeatureDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Platform.ToggleShopFeature", async () =>
        {
            var shopExists = await _db.Shops.AnyAsync(s => s.Id == shopId, ct);
            if (!shopExists) return Result<bool>.NotFound(Errors.PlatformAdmin.ShopNotFound);

            var existing = await _db.ShopFeatureOverrides
                .FirstOrDefaultAsync(f => f.ShopId == shopId && f.FeatureCode == dto.FeatureCode, ct);

            if (existing is null)
            {
                _db.ShopFeatureOverrides.Add(new ShopFeatureOverride
                {
                    ShopId = shopId, FeatureCode = dto.FeatureCode,
                    IsEnabled = dto.Enabled, CreatedAtUtc = DateTime.UtcNow,
                });
            }
            else
            {
                existing.IsEnabled  = dto.Enabled;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);

            permissionService.InvalidateShopFeatureCache(shopId);

            logger.LogInformation("Shop {ShopId} feature {Feature} toggled to {Enabled}",
                shopId, dto.FeatureCode, dto.Enabled);

            return Result<bool>.Success(true);
        }, ct);

    public async Task<Result<bool>> SuspendShopAsync(long shopId, SuspendShopDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Platform.SuspendShop", async () =>
        {
            var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Id == shopId, ct);
            if (shop is null) return Result<bool>.NotFound(Errors.PlatformAdmin.ShopNotFound);
            if (!shop.IsActive) return Result<bool>.Conflict(Errors.PlatformAdmin.ShopAlreadySuspended);

            shop.IsActive     = false;
            shop.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            logger.LogWarning("Shop {ShopId} suspended. Reason: {Reason}", shopId, dto.Reason);

            return Result<bool>.Success(true);
        }, ct);

    public async Task<Result<bool>> ActivateShopAsync(long shopId, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Platform.ActivateShop", async () =>
        {
            var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Id == shopId, ct);
            if (shop is null) return Result<bool>.NotFound(Errors.PlatformAdmin.ShopNotFound);
            if (shop.IsActive) return Result<bool>.Conflict(Errors.PlatformAdmin.ShopAlreadyActive);

            shop.IsActive     = true;
            shop.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            logger.LogInformation("Shop {ShopId} activated", shopId);

            return Result<bool>.Success(true);
        }, ct);

    // ── Private helpers ───────────────────────────────────────────────────────

    private static PlatformSubscriptionPlanDto MapPlan(SubscriptionPlan p) => new(
        p.Id, p.Code, p.Label,
        p.MonthlyPrice, p.AnnualPrice,
        p.MaxUsers, p.MaxProducts, p.MaxInvoicesPerMonth,
        p.StorageQuotaMb, p.SmsQuotaPerMonth, p.EmailQuotaPerMonth,
        p.IsActive,
        p.Features.Select(f => f.FeatureCode).ToList());

    private sealed record ShopStatsRow(long InvoiceCount, decimal RevenueCurrentMonth);
}
