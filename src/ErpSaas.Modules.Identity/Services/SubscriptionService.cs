using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Identity.Services;

public sealed class SubscriptionService(
    PlatformDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant,
    ILogger<SubscriptionService> logger)
    : BaseService<PlatformDbContext>(db, errorLogger), ISubscriptionService
{
    public async Task<IReadOnlyList<SubscriptionPlanDto>> ListPlansAsync(CancellationToken ct = default)
    {
        var plans = await db.SubscriptionPlans
            .Where(p => p.IsActive)
            .Include(p => p.Features)
            .ToListAsync(ct);

        return plans.OrderBy(p => p.MonthlyPrice).Select(MapPlan).ToList();
    }

    public async Task<CurrentSubscriptionDto?> GetCurrentAsync(CancellationToken ct = default)
    {
        var sub = await db.ShopSubscriptions
            .Where(s => s.ShopId == tenant.ShopId && s.IsActive)
            .Include(s => s.Plan)
            .ThenInclude(p => p.Features)
            .OrderByDescending(s => s.StartsAtUtc)
            .FirstOrDefaultAsync(ct);

        if (sub is null) return null;

        return new CurrentSubscriptionDto(
            sub.Id,
            sub.Plan.Code,
            sub.Plan.Label,
            sub.Plan.MonthlyPrice,
            sub.Plan.AnnualPrice,
            sub.BillingCycle.ToString(),
            sub.StartsAtUtc,
            sub.EndsAtUtc,
            sub.IsActive,
            sub.Plan.MaxUsers,
            sub.Plan.MaxProducts,
            sub.Plan.MaxInvoicesPerMonth,
            sub.Plan.Features.Select(f => f.FeatureCode).ToList());
    }

    public async Task<Result<bool>> ChangePlanAsync(ChangePlanDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Subscription.ChangePlan", async () =>
        {
            if (!Enum.TryParse<BillingCycle>(dto.BillingCycle, ignoreCase: true, out var cycle))
                return Result<bool>.Failure(Errors.Subscription.InvalidBillingCycle);

            var newPlan = await db.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Code == dto.PlanCode && p.IsActive, ct);
            if (newPlan is null)
                return Result<bool>.NotFound(Errors.Subscription.PlanNotFound);

            // Deactivate existing active subscription
            var current = await db.ShopSubscriptions
                .Where(s => s.ShopId == tenant.ShopId && s.IsActive)
                .FirstOrDefaultAsync(ct);

            if (current is not null)
            {
                if (current.PlanId == newPlan.Id && current.BillingCycle == cycle)
                    return Result<bool>.Conflict(Errors.Subscription.AlreadyOnPlan);

                current.IsActive = false;
                current.EndsAtUtc = DateTime.UtcNow;
            }

            db.ShopSubscriptions.Add(new ShopSubscription
            {
                ShopId       = tenant.ShopId,
                PlanId       = newPlan.Id,
                BillingCycle = cycle,
                StartsAtUtc  = DateTime.UtcNow,
                IsActive     = true,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Shop {ShopId} changed subscription to plan {PlanCode} ({Cycle})",
                tenant.ShopId, dto.PlanCode, dto.BillingCycle);

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> CancelAsync(CancellationToken ct = default)
        => await ExecuteAsync<bool>("Subscription.Cancel", async () =>
        {
            var sub = await db.ShopSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.ShopId == tenant.ShopId && s.IsActive)
                .FirstOrDefaultAsync(ct);

            if (sub is null)
                return Result<bool>.NotFound(Errors.Subscription.NoActiveSubscription);

            if (sub.Plan?.Code == "Starter")
                return Result<bool>.Conflict(Errors.Subscription.CannotCancelFree);

            sub.IsActive = false;
            sub.EndsAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Shop {ShopId} cancelled subscription (plan {PlanCode})",
                tenant.ShopId, sub.Plan?.Code);

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    // ── Private helpers ───────────────────────────────────────────────────────

    private static SubscriptionPlanDto MapPlan(SubscriptionPlan p) => new(
        p.Id,
        p.Code,
        p.Label,
        p.MonthlyPrice,
        p.AnnualPrice,
        p.MaxUsers,
        p.MaxProducts,
        p.MaxInvoicesPerMonth,
        p.StorageQuotaMb,
        p.Features.Select(f => f.FeatureCode).ToList());
}
