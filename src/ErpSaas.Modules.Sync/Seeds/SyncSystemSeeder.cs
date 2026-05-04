using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Sync.Seeds;

public sealed class SyncSystemSeeder(
    PlatformDbContext platformDb,
    ILogger<SyncSystemSeeder> logger) : IDataSeeder
{
    public int Order => 130;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedFeatureFlagsAsync(ct);
        await SeedMenuAsync(ct);
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        var permissions = new[]
        {
            ("Device.Register",       "Sync",   "Register Device"),
            ("Device.Manage",         "Sync",   "Manage Devices"),
            ("Sync.View",             "Sync",   "View Sync Queue"),
            ("Sync.ResolveException", "Sync",   "Resolve Sync Exception"),
        };

        foreach (var (code, module, label) in permissions)
        {
            if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
            {
                platformDb.Permissions.Add(new Permission
                {
                    Code = code, Module = module, Label = label, CreatedAtUtc = DateTime.UtcNow,
                });
            }
        }

        await platformDb.SaveChangesAsync(ct);
        logger.LogInformation("Sync permissions seeded");
    }

    private async Task SeedFeatureFlagsAsync(CancellationToken ct)
    {
        if (!await platformDb.SubscriptionPlanFeatures.AnyAsync(f => f.FeatureCode == "offline_mode", ct))
        {
            var growthPlan = await platformDb.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Code == "GROWTH", ct);

            if (growthPlan is not null)
            {
                platformDb.SubscriptionPlanFeatures.Add(new SubscriptionPlanFeature
                {
                    PlanId = growthPlan.Id, FeatureCode = "offline_mode", CreatedAtUtc = DateTime.UtcNow,
                });
                await platformDb.SaveChangesAsync(ct);
            }
        }

        logger.LogInformation("Sync feature flags seeded");
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var adminGroup = await platformDb.MenuItems
                .FirstOrDefaultAsync(m => m.Code == "admin", ct);

            if (adminGroup is null)
            {
                await tx.RollbackAsync(ct);
                return;
            }

            var pages = new[]
            {
                ("admin.sync-devices",    "Devices",         "pi pi-mobile",            "/admin/sync/devices",    75, "Device.Manage"),
                ("admin.sync-exceptions", "Sync Exceptions", "pi pi-exclamation-circle", "/admin/sync/exceptions", 76, "Sync.ResolveException"),
            };

            foreach (var (code, label, icon, route, sort, perm) in pages)
            {
                if (!await platformDb.MenuItems.AnyAsync(m => m.Code == code, ct))
                {
                    platformDb.MenuItems.Add(new MenuItem
                    {
                        Code = code, Label = label,
                        Kind = MenuItemKind.Page, Icon = icon,
                        Route = route, SortOrder = sort,
                        ParentId = adminGroup.Id, IsActive = true,
                        RequiredPermission = perm,
                        CreatedAtUtc = DateTime.UtcNow,
                    });
                }
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            logger.LogInformation("Sync menu items seeded");
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
