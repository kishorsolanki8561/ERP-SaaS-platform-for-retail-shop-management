using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Verticals.Grocery.Seeds;

public sealed class GrocerySystemSeeder(
    PlatformDbContext platformDb,
    ILogger<GrocerySystemSeeder> logger) : IDataSeeder
{
    public int Order => 125;

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
                ("Loyalty.View",   "View customer loyalty balances and history"),
                ("Loyalty.Manage", "Configure the loyalty programme"),
                ("Loyalty.Earn",   "Credit points when a sale is made"),
                ("Loyalty.Redeem", "Redeem points against a sale"),
            };

            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "Grocery", Label = label,
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
            logger.LogError(ex, "GrocerySystemSeeder.SeedPermissionsAsync failed");
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
                ("loyalty.program",   "Loyalty Programme",  "pi pi-gift",       "/loyalty/program",   "Loyalty.Manage", 10),
                ("loyalty.customers", "Customer Points",    "pi pi-users",      "/loyalty/customers", "Loyalty.View",   20),
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
                        RequiredFeature = "Verticals.GroceryLoyaltyPoints",
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
            logger.LogError(ex, "GrocerySystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }
}
