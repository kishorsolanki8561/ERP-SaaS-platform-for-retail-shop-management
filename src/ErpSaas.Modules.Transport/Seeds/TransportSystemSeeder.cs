using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Sequence;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Transport.Seeds;

public sealed class TransportSystemSeeder(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    ILogger<TransportSystemSeeder> logger) : IDataSeeder
{
    public int Order => 80;

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
                ("Transport.View",   "View transport providers, vehicles and deliveries"),
                ("Transport.Manage", "Create and manage transport providers, vehicles and deliveries"),
            };
            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "Transport", Label = label,
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
            logger.LogError(ex, "TransportSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "transport", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "transport", Label = "Transport",
                    Kind = MenuItemKind.Group, Icon = "pi pi-truck",
                    SortOrder = 50, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
            }

            var pages = new[]
            {
                ("transport.providers", "Transport Providers", "pi pi-building",  "/transport/providers",  "Transport.View", 10),
                ("transport.vehicles",  "Vehicles",            "pi pi-car",        "/transport/vehicles",   "Transport.View", 20),
                ("transport.deliveries","Deliveries",          "pi pi-send",       "/transport/deliveries", "Transport.View", 30),
            };
            foreach (var (code, label, icon, route, perm, sort) in pages)
            {
                if (!await platformDb.MenuItems.AnyAsync(m => m.Code == code, ct))
                {
                    platformDb.MenuItems.Add(new MenuItem
                    {
                        Code = code, Label = label, Kind = MenuItemKind.Page,
                        Icon = icon, Route = route, ParentId = group.Id,
                        SortOrder = sort, RequiredPermission = perm,
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
            logger.LogError(ex, "TransportSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }

    private async Task SeedFeaturesAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            const string featureCode = "Transport.VehicleTracking";
            if (!await platformDb.SubscriptionPlanFeatures.AnyAsync(f => f.FeatureCode == featureCode, ct))
            {
                var plans = await platformDb.SubscriptionPlans
                    .Where(p => p.Code == Constants.Plans.Growth || p.Code == Constants.Plans.Enterprise)
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
            logger.LogError(ex, "TransportSystemSeeder.SeedFeaturesAsync failed");
            throw;
        }
    }

    private async Task SeedSequencesAsync(CancellationToken ct)
    {
        await using var tx = await tenantDb.Database.BeginTransactionAsync(ct);
        try
        {
            const long platformShopId = 0L;
            if (!await tenantDb.SequenceDefinitions
                    .IgnoreQueryFilters()
                    .AnyAsync(s => s.Code == Constants.SequenceCodes.Delivery && s.ShopId == platformShopId, ct))
            {
                tenantDb.SequenceDefinitions.Add(new SequenceDefinition
                {
                    ShopId = platformShopId,
                    Code = Constants.SequenceCodes.Delivery,
                    Prefix = Constants.SequencePrefixes.Delivery,
                    PadLength = 6, LastNumber = 0, CreatedAtUtc = DateTime.UtcNow,
                });
                logger.LogInformation("Seeding sequence: {Code}", Constants.SequenceCodes.Delivery);
            }
            await tenantDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "TransportSystemSeeder.SeedSequencesAsync failed");
            throw;
        }
    }
}
