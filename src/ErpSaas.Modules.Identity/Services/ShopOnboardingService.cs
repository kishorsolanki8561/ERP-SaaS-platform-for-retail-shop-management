#pragma warning disable CS9107
using BCrypt.Net;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Identity.Services;

public sealed class ShopOnboardingService(
    PlatformDbContext db,
    IErrorLogger errorLogger)
    : BaseService<PlatformDbContext>(db, errorLogger), IShopOnboardingService
{
    public async Task<Result<long>> OnboardAsync(OnboardShopRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<long>("Identity.OnboardShop", async () =>
        {
            if (await db.Shops.AnyAsync(s => s.ShopCode == request.ShopCode, ct))
                return Result<long>.Conflict(Errors.Shop.CodeConflict(request.ShopCode));

            var starterPlan = await db.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Code == "Starter" && p.IsActive, ct);

            if (starterPlan is null)
                return Result<long>.Failure(Errors.Shop.StarterPlanMissing);

            var shop = new Shop
            {
                ShopCode = request.ShopCode,
                LegalName = request.LegalName,
                TradeName = request.TradeName,
                GstNumber = request.GstNumber,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Shops.Add(shop);
            await db.SaveChangesAsync(ct);

            var user = new User
            {
                Email = request.AdminEmail,
                DisplayName = request.AdminDisplayName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword, workFactor: 12),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);

            db.UserShops.Add(new UserShop
            {
                UserId = user.Id,
                ShopId = shop.Id,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });

            db.ShopSubscriptions.Add(new ShopSubscription
            {
                ShopId = shop.Id,
                PlanId = starterPlan.Id,
                StartsAtUtc = DateTime.UtcNow,
                IsActive = true,
                BillingCycle = BillingCycle.Monthly,
                CreatedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);
            return Result<long>.Success(shop.Id);
        }, ct, useTransaction: true);
    }
}
