using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Inventory.Seeds;

/// <summary>
/// Seeds Inventory module platform data: permissions, DDL catalog PRODUCT_CATEGORY, and menu items.
/// Order=41 runs after IdentityDataSeeder (20), MasterDataSeeder (15), MenuDataSeeder (30),
/// and DdlDataSeeder (10).
/// </summary>
public sealed class InventorySystemSeeder(
    PlatformDbContext db,
    ILogger<InventorySystemSeeder> logger) : IDataSeeder
{
    public int Order => 41;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await SeedPermissionsAsync(ct);
            await SeedDdlCatalogAsync(ct);
            await SeedMenuItemsAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "InventorySystemSeeder failed — rolled back");
            throw;
        }
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        var permissions = new[]
        {
            ("Inventory.View",   "Inventory", "View products, warehouses, and stock levels"),
            ("Inventory.Manage", "Inventory", "Create and update products, warehouses, and stock adjustments"),
        };

        foreach (var (code, module, label) in permissions)
        {
            if (!await db.Permissions.AnyAsync(p => p.Code == code, ct))
            {
                db.Permissions.Add(new Permission
                {
                    Code = code,
                    Module = module,
                    Label = label,
                    CreatedAtUtc = DateTime.UtcNow,
                });
                logger.LogInformation("Seeding permission: {Code}", code);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SeedDdlCatalogAsync(CancellationToken ct)
    {
        const string key = Constants.DdlKeys.ProductCategory;

        if (await db.DdlCatalogs.AnyAsync(c => c.Key == key, ct))
            return;

        var catalog = new DdlCatalog
        {
            Key = key,
            Label = "Product Category",
            IsActive = true,
        };

        var items = new[]
        {
            ("ELECTRICAL",   "Electrical"),
            ("ELECTRONICS",  "Electronics"),
            ("POWER_TOOLS",  "Power Tools"),
            ("HARDWARE",     "Hardware"),
            ("ACCESSORIES",  "Accessories"),
            ("CABLES",       "Cables"),
            ("BATTERIES",    "Batteries"),
            ("LIGHTING",     "Lighting"),
            ("SAFETY",       "Safety"),
        };

        for (int i = 0; i < items.Length; i++)
        {
            catalog.Items.Add(new DdlItem
            {
                Code = items[i].Item1,
                Label = items[i].Item2,
                SortOrder = (i + 1) * 10,
                IsActive = true,
            });
        }

        db.DdlCatalogs.Add(catalog);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded DDL catalog: {Key} with {Count} items", key, items.Length);
    }

    private async Task SeedMenuItemsAsync(CancellationToken ct)
    {
        // Ensure parent group exists first.
        const string parentCode = "inventory";

        var codeToId = new Dictionary<string, long>();

        var entries = new[]
        {
            // (code, label, kind, icon, route, sortOrder, parentCode, requiredPermission)
            (parentCode,         "Inventory", MenuItemKind.Group, "pi pi-box",       (string?)null,               30, (string?)null, (string?)null),
            ("inventory.products","Products", MenuItemKind.Page,  "pi pi-list",      "/inventory/products",       10, parentCode,    "Inventory.View"),
        };

        foreach (var (code, label, kind, icon, route, sort, _, req) in entries)
        {
            var existing = await db.MenuItems.FirstOrDefaultAsync(m => m.Code == code, ct);
            if (existing is not null)
            {
                codeToId[code] = existing.Id;
                continue;
            }

            var item = new MenuItem
            {
                Code = code,
                Label = label,
                Kind = kind,
                Icon = icon,
                Route = route,
                SortOrder = sort,
                RequiredPermission = req,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.MenuItems.Add(item);
            await db.SaveChangesAsync(ct);
            codeToId[code] = item.Id;
            logger.LogInformation("Seeded menu item: {Code}", code);
        }

        // Wire parent relationships.
        foreach (var (code, _, _, _, _, _, parentMenuCode, _) in entries)
        {
            if (parentMenuCode is null) continue;
            if (!codeToId.TryGetValue(code, out var childId)) continue;
            if (!codeToId.TryGetValue(parentMenuCode, out var parentId)) continue;

            var child = await db.MenuItems.FindAsync([childId], ct);
            if (child is not null && child.ParentId != parentId)
                child.ParentId = parentId;
        }

        await db.SaveChangesAsync(ct);
    }
}
