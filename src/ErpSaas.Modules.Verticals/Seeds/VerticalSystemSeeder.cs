using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Verticals;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Verticals.Seeds;

public sealed class VerticalSystemSeeder(
    PlatformDbContext platformDb,
    ILogger<VerticalSystemSeeder> logger) : IDataSeeder
{
    public int Order => 110;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedMenuAsync(ct);
        await SeedVerticalPacksAsync(ct);
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var perms = new[]
            {
                ("Vertical.View",   "View the shop's active vertical pack"),
                ("Vertical.Manage", "Install or change the shop's vertical pack"),
            };

            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "Verticals", Label = label,
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
            logger.LogError(ex, "VerticalSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var pages = new[]
            {
                ("settings.vertical", "Industry Vertical", "pi pi-th-large",
                 "/admin/settings/vertical", "Vertical.View", 90),
            };

            foreach (var (code, label, icon, route, perm, sort) in pages)
            {
                if (!await platformDb.MenuItems.AnyAsync(m => m.Code == code, ct))
                {
                    platformDb.MenuItems.Add(new MenuItem
                    {
                        Code = code, Label = label, Kind = MenuItemKind.Page,
                        Icon = icon, Route = route,
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
            logger.LogError(ex, "VerticalSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }

    private async Task SeedVerticalPacksAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var packs = new[]
            {
                ("ELECTRICAL",
                 "Electrical & Electronics",
                 "Pre-configured for electrical shops: wire, switchgear, fans, UPS, inverters, CCTV, power tools.",
                 "Billing.BarcodePos,Warranty.Module",
                 "pi pi-bolt", 1),
                ("MEDICAL",
                 "Medical & Pharmacy",
                 "Schedule-H drug tracking, batch/expiry management, prescription uploads, narcotic register.",
                 "Verticals.MedicalBatchExpiry,Billing.BarcodePos",
                 "pi pi-heart", 2),
                ("GROCERY",
                 "Grocery & FMCG",
                 "Barcode-heavy POS, loose vs packed UOM, FIFO costing, loyalty points programme.",
                 "Verticals.GroceryLoyaltyPoints,Billing.BarcodePos",
                 "pi pi-shopping-cart", 3),
            };

            foreach (var (code, name, description, featureFlags, icon, sort) in packs)
            {
                if (!await platformDb.VerticalPacks.AnyAsync(v => v.Code == code && !v.IsDeleted, ct))
                {
                    platformDb.VerticalPacks.Add(new VerticalPack
                    {
                        Code = code,
                        Name = name,
                        Description = description,
                        FeatureFlagsCsv = featureFlags,
                        IconClass = icon,
                        SortOrder = sort,
                        IsActive = true,
                        SeedManifestJson = "{}",
                        CreatedAtUtc = DateTime.UtcNow,
                    });
                    logger.LogInformation("Seeding vertical pack: {Code}", code);
                }
            }
            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "VerticalSystemSeeder.SeedVerticalPacksAsync failed");
            throw;
        }
    }
}
