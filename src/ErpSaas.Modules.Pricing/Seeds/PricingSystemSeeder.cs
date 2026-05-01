using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Pricing.Seeds;

public sealed class PricingSystemSeeder(
    PlatformDbContext platformDb,
    ILogger<PricingSystemSeeder> logger) : IDataSeeder
{
    public int Order => 75;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedMenuAsync(ct);
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var perms = new[]
            {
                ("Pricing.View",   "View discount rules, extra charges, offers and run pricing calculation"),
                ("Pricing.Manage", "Create and toggle discount rules, extra charges and offers"),
            };

            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "Pricing", Label = label,
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
            logger.LogError(ex, "PricingSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "pricing", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "pricing", Label = "Pricing",
                    Kind = MenuItemKind.Group, Icon = "pi pi-tag",
                    SortOrder = 45, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
            }

            var pages = new[]
            {
                ("pricing.discounts",     "Discount Rules",  "pi pi-percentage", "/pricing/discounts",     "Pricing.View", 10),
                ("pricing.extra-charges", "Extra Charges",   "pi pi-plus-circle","/pricing/extra-charges", "Pricing.View", 20),
                ("pricing.offers",        "Offers & Combos", "pi pi-gift",       "/pricing/offers",        "Pricing.View", 30),
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
            logger.LogError(ex, "PricingSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }
}
