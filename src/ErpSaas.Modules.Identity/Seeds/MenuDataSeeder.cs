using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Identity.Seeds;

public sealed class MenuDataSeeder(
    PlatformDbContext db,
    ILogger<MenuDataSeeder> logger) : IDataSeeder
{
    public int Order => 30;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await SeedItemsAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "MenuDataSeeder failed — rolled back");
            throw;
        }
    }

    private async Task SeedItemsAsync(CancellationToken ct)
    {
        // (code, label, kind, icon, route, sortOrder, parentCode, requiredPermission)
        var items = new[]
        {
            ("dashboard",            "Dashboard",    MenuItemKind.Group,   "pi pi-home",     null,                  10, (string?)null, (string?)null),
            ("dashboard.home",       "Home",         MenuItemKind.Page,    "pi pi-chart-bar","/dashboard",          10, "dashboard",   null),
            ("admin",                "Administration",MenuItemKind.Group,  "pi pi-cog",      null,                  20, null,          null),
            ("admin.users",          "Users",        MenuItemKind.Page,    "pi pi-users",    "/admin/users",        10, "admin",       "User.Manage"),
            ("admin.shop-profile",   "Shop Profile", MenuItemKind.Page,    "pi pi-building", "/admin/shop-profile", 20, "admin",       "Shop.Manage"),
            ("admin.master-data",    "Master Data",  MenuItemKind.Page,    "pi pi-database", "/admin/master-data",  30, "admin",       "MasterData.Manage"),
        };

        var codeToId = new Dictionary<string, long>();

        // Upsert items
        foreach (var (code, label, kind, icon, route, sort, _, req) in items)
        {
            var existing = await db.MenuItems.FirstOrDefaultAsync(m => m.Code == code, ct);
            if (existing is not null)
            {
                codeToId[code] = existing.Id;
                continue;
            }

            var item = new MenuItem
            {
                Code = code, Label = label, Kind = kind,
                Icon = icon, Route = route, SortOrder = sort,
                RequiredPermission = req, IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.MenuItems.Add(item);
            await db.SaveChangesAsync(ct);
            codeToId[code] = item.Id;
            logger.LogInformation("Seeded menu item: {Code}", code);
        }

        // Wire parents
        foreach (var (code, _, _, _, _, _, parentCode, _) in items)
        {
            if (parentCode is null) continue;
            if (!codeToId.TryGetValue(code, out var childId)) continue;
            if (!codeToId.TryGetValue(parentCode, out var parentId)) continue;

            var child = await db.MenuItems.FindAsync([childId], ct);
            if (child is not null && child.ParentId != parentId)
            {
                child.ParentId = parentId;
            }
        }
        await db.SaveChangesAsync(ct);
    }
}
