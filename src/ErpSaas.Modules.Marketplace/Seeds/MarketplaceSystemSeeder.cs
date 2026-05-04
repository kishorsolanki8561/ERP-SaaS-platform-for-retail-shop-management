using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Marketplace.Seeds;

public sealed class MarketplaceSystemSeeder(
    PlatformDbContext platformDb,
    ILogger<MarketplaceSystemSeeder> logger) : IDataSeeder
{
    public int Order => 95;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedMenuAsync(ct);
        await SeedDdlAsync(ct);
        await SeedFeaturesAsync(ct);
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var perms = new[]
            {
                ("Marketplace.View",         "View marketplace accounts, orders and product mappings"),
                ("Marketplace.Manage",       "Add and configure marketplace accounts and product links"),
                ("Marketplace.Sync",         "Trigger inventory, price and order sync jobs"),
                ("Marketplace.ConvertOrder", "Convert marketplace orders to invoices"),
            };
            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "Marketplace", Label = label,
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
            logger.LogError(ex, "MarketplaceSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "marketplace", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "marketplace", Label = "Marketplace",
                    Kind = MenuItemKind.Group, Icon = "pi pi-shopping-bag",
                    SortOrder = 70, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
            }

            var pages = new[]
            {
                ("marketplace.accounts",       "Accounts",        "pi pi-link",        "/marketplace/accounts",        "Marketplace.View",   10),
                ("marketplace.product-mapping","Product Mapping",  "pi pi-tag",         "/marketplace/product-mapping", "Marketplace.View",   20),
                ("marketplace.orders",         "Orders",          "pi pi-inbox",       "/marketplace/orders",          "Marketplace.View",   30),
                ("marketplace.sync-logs",      "Sync Logs",       "pi pi-history",     "/marketplace/sync-logs",       "Marketplace.View",   40),
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
            logger.LogError(ex, "MarketplaceSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }

    private async Task SeedDdlAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            await EnsureDdl(Constants.DdlKeys.Marketplace, "Marketplace",
            [
                ("Amazon",    "Amazon",     10),
                ("Flipkart",  "Flipkart",   20),
                ("Meesho",    "Meesho",     30),
                ("Shopify",   "Shopify",    40),
                ("WooCommerce","WooCommerce",50),
            ], ct);

            await EnsureDdl(Constants.DdlKeys.MarketplaceOrderStatus, "Marketplace Order Status",
            [
                ("New",       "New",       10),
                ("Processing","Processing",20),
                ("Shipped",   "Shipped",   30),
                ("Cancelled", "Cancelled", 40),
                ("Failed",    "Failed",    50),
                ("Converted", "Converted", 60),
            ], ct);

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "MarketplaceSystemSeeder.SeedDdlAsync failed");
            throw;
        }
    }

    private async Task EnsureDdl(string key, string label, (string code, string lbl, int sort)[] items, CancellationToken ct)
    {
        var catalog = await platformDb.DdlCatalogs
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Key == key, ct);

        if (catalog is null)
        {
            catalog = new DdlCatalog { Key = key, Label = label, IsActive = true };
            platformDb.DdlCatalogs.Add(catalog);
            await platformDb.SaveChangesAsync(ct);
        }

        foreach (var (code, lbl, sort) in items)
        {
            if (!catalog.Items.Any(i => i.Code == code))
            {
                catalog.Items.Add(new DdlItem { Code = code, Label = lbl, SortOrder = sort, IsActive = true });
                logger.LogInformation("Seeding DDL {Key}/{Code}", key, code);
            }
        }
    }

    private async Task SeedFeaturesAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var features = new[]
            {
                "Marketplace.Amazon",
                "Marketplace.Flipkart",
                "Marketplace.Shopify",
                "Marketplace.WooCommerce",
                "Marketplace.OwnEcommerce",
            };

            var plans = await platformDb.SubscriptionPlans
                .Where(p => p.Code == Constants.Plans.Growth || p.Code == Constants.Plans.Enterprise)
                .ToListAsync(ct);

            foreach (var featureCode in features)
            {
                if (!await platformDb.SubscriptionPlanFeatures.AnyAsync(f => f.FeatureCode == featureCode, ct))
                {
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
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "MarketplaceSystemSeeder.SeedFeaturesAsync failed");
            throw;
        }
    }
}
