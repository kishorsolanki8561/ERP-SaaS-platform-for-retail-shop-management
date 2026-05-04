using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.ApiAccess.Seeds;

public sealed class ApiAccessSystemSeeder(
    PlatformDbContext platformDb,
    ILogger<ApiAccessSystemSeeder> logger) : IDataSeeder
{
    public int Order => 120;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedMenuAsync(ct);
        await SeedFeaturesAsync(ct);
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var perms = new[]
            {
                ("Integration.ManageApiKeys",    "Create, rotate and revoke API keys"),
                ("Integration.ManageWebhooks",   "Register and manage webhook endpoints"),
                ("Integration.ViewDeliveries",   "View webhook delivery history"),
            };

            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "ApiAccess", Label = label,
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
            logger.LogError(ex, "ApiAccessSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "integrations", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "integrations", Label = "Integrations",
                    Kind = MenuItemKind.Group, Icon = "pi pi-link",
                    SortOrder = 95, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
            }

            var pages = new[]
            {
                ("integrations.api-keys",  "API Keys",  "pi pi-key",      "/admin/integrations/api-keys",  "Integration.ManageApiKeys",  "integration.api_access", 10),
                ("integrations.webhooks",  "Webhooks",  "pi pi-bolt",     "/admin/integrations/webhooks",  "Integration.ManageWebhooks", "integration.webhooks",   20),
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
            logger.LogError(ex, "ApiAccessSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }

    private async Task SeedFeaturesAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var featuresToSeed = new[]
            {
                ("integration.api_access", new[] { Constants.Plans.Growth, Constants.Plans.Enterprise }),
                ("integration.webhooks",   new[] { Constants.Plans.Growth, Constants.Plans.Enterprise }),
            };

            foreach (var (featureCode, plans) in featuresToSeed)
            {
                if (!await platformDb.SubscriptionPlanFeatures.AnyAsync(f => f.FeatureCode == featureCode, ct))
                {
                    var planEntities = await platformDb.SubscriptionPlans
                        .Where(p => plans.Contains(p.Code))
                        .ToListAsync(ct);

                    foreach (var plan in planEntities)
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
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "ApiAccessSystemSeeder.SeedFeaturesAsync failed");
            throw;
        }
    }
}
