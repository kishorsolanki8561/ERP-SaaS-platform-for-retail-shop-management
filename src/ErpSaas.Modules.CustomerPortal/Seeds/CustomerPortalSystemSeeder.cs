using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Sequence;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.CustomerPortal.Seeds;

public sealed class CustomerPortalSystemSeeder(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    ILogger<CustomerPortalSystemSeeder> logger) : IDataSeeder
{
    public int Order => 95;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedMenuAsync(ct);
        await SeedSequencesAsync(ct);
        await SeedFeaturesAsync(ct);
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var perms = new[]
            {
                ("OnlineOrder.View",   "View online orders from the customer portal"),
                ("OnlineOrder.Manage", "Accept, reject, dispatch and cancel online orders"),
                ("Inquiry.View",       "View customer inquiries from the portal"),
                ("Inquiry.Manage",     "Reply to, assign, and close customer inquiries"),
                ("Portal.Config",      "Configure customer portal settings per shop"),
            };
            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code,
                        Module = "CustomerPortal",
                        Label = label,
                        CreatedAtUtc = DateTime.UtcNow,
                    });
                    logger.LogInformation("Seeding permission: {Code}", code);
                }
            }
            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "CustomerPortalSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "portal", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "portal", Label = "Customer Portal",
                    Kind = MenuItemKind.Group, Icon = "pi pi-globe",
                    SortOrder = 90, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
            }

            var pages = new[]
            {
                ("portal.orders",    "Online Orders",       "pi pi-shopping-bag", "/portal/orders",    "OnlineOrder.View",  "customer.online_orders",  10),
                ("portal.inquiries", "Customer Inquiries",  "pi pi-comments",     "/portal/inquiries", "Inquiry.View",      "customer.portal",         20),
                ("portal.config",    "Portal Settings",     "pi pi-cog",          "/portal/config",    "Portal.Config",     "customer.portal",         30),
            };

            foreach (var (code, label, icon, route, perm, feature, sort) in pages)
            {
                if (!await platformDb.MenuItems.AnyAsync(m => m.Code == code, ct))
                {
                    platformDb.MenuItems.Add(new MenuItem
                    {
                        Code = code, Label = label, Kind = MenuItemKind.Page,
                        Icon = icon, Route = route, ParentId = group.Id,
                        SortOrder = sort, RequiredPermission = perm,
                        RequiredFeature = feature,
                        IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                    });
                }
            }
            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "CustomerPortalSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }

    private async Task SeedSequencesAsync(CancellationToken ct)
    {
        await using var tx = await tenantDb.Database.BeginTransactionAsync(ct);
        try
        {
            const long platformShopId = 0L;
            var sequences = new[]
            {
                (Constants.SequenceCodes.OnlineOrder,     Constants.SequencePrefixes.OnlineOrder),
                (Constants.SequenceCodes.CustomerInquiry, Constants.SequencePrefixes.CustomerInquiry),
            };

            foreach (var (code, prefix) in sequences)
            {
                if (!await tenantDb.SequenceDefinitions
                        .IgnoreQueryFilters()
                        .AnyAsync(s => s.Code == code && s.ShopId == platformShopId, ct))
                {
                    tenantDb.SequenceDefinitions.Add(new SequenceDefinition
                    {
                        ShopId = platformShopId,
                        Code = code,
                        Prefix = prefix,
                        PadLength = 6, LastNumber = 0, CreatedAtUtc = DateTime.UtcNow,
                    });
                    logger.LogInformation("Seeding sequence: {Code}", code);
                }
            }
            await tenantDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "CustomerPortalSystemSeeder.SeedSequencesAsync failed");
            throw;
        }
    }

    private async Task SeedFeaturesAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var featuresByPlan = new[]
            {
                ("customer.portal",         new[] { Constants.Plans.Starter, Constants.Plans.Growth, Constants.Plans.Enterprise }),
                ("customer.online_orders",  new[] { Constants.Plans.Growth, Constants.Plans.Enterprise }),
                ("customer.smart_shopping", new[] { Constants.Plans.Enterprise }),
            };

            foreach (var (featureCode, planCodes) in featuresByPlan)
            {
                if (await platformDb.SubscriptionPlanFeatures.AnyAsync(f => f.FeatureCode == featureCode, ct))
                    continue;

                var plans = await platformDb.SubscriptionPlans
                    .Where(p => planCodes.Contains(p.Code))
                    .ToListAsync(ct);

                foreach (var plan in plans)
                {
                    platformDb.SubscriptionPlanFeatures.Add(new SubscriptionPlanFeature
                    {
                        PlanId = plan.Id,
                        FeatureCode = featureCode,
                        CreatedAtUtc = DateTime.UtcNow,
                    });
                }
                logger.LogInformation("Seeding feature flag: {Code}", featureCode);
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "CustomerPortalSystemSeeder.SeedFeaturesAsync failed");
            throw;
        }
    }
}
