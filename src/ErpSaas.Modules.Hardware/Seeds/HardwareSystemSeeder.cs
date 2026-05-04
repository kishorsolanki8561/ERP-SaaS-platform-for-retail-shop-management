using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Hardware.Seeds;

/// <summary>
/// Seeds permissions, menu items, and feature flags for the Hardware module.
/// Order = 46 — runs after IdentityDataSeeder (20) and WalletSystemSeeder (44).
/// </summary>
public sealed class HardwareSystemSeeder(
    PlatformDbContext platformDb,
    ILogger<HardwareSystemSeeder> logger) : IDataSeeder
{
    public int Order => 46;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedMenuAsync(ct);
        await SeedFeaturesAsync(ct);
    }

    // ── Menu ──────────────────────────────────────────────────────────────────

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems
                .FirstOrDefaultAsync(m => m.Code == "hardware", ct);

            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "hardware",
                    Label = "Hardware",
                    Kind = MenuItemKind.Group,
                    Icon = "pi pi-server",
                    SortOrder = 90,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
                logger.LogInformation("Seeded menu group: hardware");
            }

            var pages = new[]
            {
                ("hardware.devices",          "Devices",          "pi pi-desktop",    "/hardware/devices",          10, "Device.Configure"),
                ("hardware.label-templates",  "Label Templates",  "pi pi-tag",        "/hardware/label-templates",  20, "Template.Label.Manage"),
                ("hardware.receipt-templates","Receipt Templates", "pi pi-file-edit",  "/hardware/receipt-templates",30, "Template.Receipt.Manage"),
            };

            foreach (var (code, label, icon, route, sort, perm) in pages)
            {
                if (!await platformDb.MenuItems.AnyAsync(m => m.Code == code, ct))
                {
                    platformDb.MenuItems.Add(new MenuItem
                    {
                        Code = code,
                        Label = label,
                        Kind = MenuItemKind.Page,
                        Icon = icon,
                        Route = route,
                        ParentId = group.Id,
                        SortOrder = sort,
                        RequiredPermission = perm,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow,
                    });
                    logger.LogInformation("Seeded menu item: {Code}", code);
                }
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "HardwareSystemSeeder.SeedMenuAsync failed — rolled back");
            throw;
        }
    }

    // ── Feature flags ─────────────────────────────────────────────────────────

    private async Task SeedFeaturesAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var features = new[]
            {
                "hardware.thermal_receipt",
                "hardware.label_printer",
            };

            foreach (var code in features)
            {
                if (!await platformDb.SubscriptionPlanFeatures.AnyAsync(f => f.FeatureCode == code, ct))
                {
                    // Available on Growth and Enterprise plans
                    var plans = await platformDb.SubscriptionPlans
                        .Where(p => p.Code == Constants.Plans.Growth || p.Code == Constants.Plans.Enterprise)
                        .ToListAsync(ct);

                    foreach (var plan in plans)
                    {
                        platformDb.SubscriptionPlanFeatures.Add(new SubscriptionPlanFeature
                        {
                            PlanId = plan.Id,
                            FeatureCode = code,
                            CreatedAtUtc = DateTime.UtcNow,
                        });
                    }
                    logger.LogInformation("Seeded feature flag: {Code}", code);
                }
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "HardwareSystemSeeder.SeedFeaturesAsync failed — rolled back");
            throw;
        }
    }
}
