using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Crm.Seeds;

public sealed class CrmSystemSeeder(
    PlatformDbContext platformDb,
    ILogger<CrmSystemSeeder> logger) : IDataSeeder
{
    public int Order => 40;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            await SeedPermissionsAsync(ct);
            await SeedDdlCatalogsAsync(ct);
            await SeedMenuItemsAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "CrmSystemSeeder failed — rolled back");
            throw;
        }
    }

    // ── Permissions ───────────────────────────────────────────────────────────

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        var permissions = new[]
        {
            ("Crm.View",   "Crm", "View CRM records"),
            ("Crm.Create", "Crm", "Create customers"),
            ("Crm.Edit",   "Crm", "Edit / deactivate customers"),
            ("Crm.Manage", "Crm", "Manage customer groups and CRM settings"),
        };

        foreach (var (code, module, label) in permissions)
        {
            if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
            {
                platformDb.Permissions.Add(new Permission
                {
                    Code = code,
                    Module = module,
                    Label = label,
                    CreatedAtUtc = DateTime.UtcNow,
                });
                logger.LogInformation("Seeding permission: {Code}", code);
            }
        }

        await platformDb.SaveChangesAsync(ct);
    }

    // ── DDL Catalogs ──────────────────────────────────────────────────────────

    private async Task SeedDdlCatalogsAsync(CancellationToken ct)
    {
        await EnsureCatalogAsync(Constants.DdlKeys.CustomerType, "Customer Type",
            new[] { ("Retail", "Retail", 10), ("Wholesale", "Wholesale", 20) }, ct);
    }

    private async Task EnsureCatalogAsync(
        string key, string label,
        (string code, string itemLabel, int sort)[] items,
        CancellationToken ct)
    {
        var catalog = await platformDb.DdlCatalogs
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Key == key, ct);

        if (catalog is null)
        {
            catalog = new DdlCatalog { Key = key, Label = label, IsActive = true };
            platformDb.DdlCatalogs.Add(catalog);
            await platformDb.SaveChangesAsync(ct);
            logger.LogInformation("Seeding DDL catalog: {Key}", key);
        }

        foreach (var (code, itemLabel, sort) in items)
        {
            if (!catalog.Items.Any(i => i.Code == code))
            {
                platformDb.DdlItems.Add(new DdlItem
                {
                    CatalogId = catalog.Id,
                    Code = code,
                    Label = itemLabel,
                    SortOrder = sort,
                    IsActive = true,
                });
                logger.LogInformation("Seeding DDL item: {Key}/{Code}", key, code);
            }
        }

        await platformDb.SaveChangesAsync(ct);
    }

    // ── Menu Items ────────────────────────────────────────────────────────────

    private async Task SeedMenuItemsAsync(CancellationToken ct)
    {
        // Ensure the parent "crm" group exists
        var crmGroupId = await EnsureMenuItemAsync(
            code: "crm",
            label: "CRM",
            kind: MenuItemKind.Group,
            icon: "pi pi-users",
            route: null,
            sortOrder: 20,
            parentCode: null,
            requiredPermission: null,
            ct);

        // Seed child page
        await EnsureMenuItemAsync(
            code: "crm.customers",
            label: "Customers",
            kind: MenuItemKind.Page,
            icon: "pi pi-user",
            route: "/crm/customers",
            sortOrder: 10,
            parentCode: "crm",
            requiredPermission: "Crm.View",
            ct);
    }

    private async Task<long> EnsureMenuItemAsync(
        string code,
        string label,
        MenuItemKind kind,
        string? icon,
        string? route,
        int sortOrder,
        string? parentCode,
        string? requiredPermission,
        CancellationToken ct)
    {
        var existing = await platformDb.MenuItems
            .FirstOrDefaultAsync(m => m.Code == code, ct);

        if (existing is not null)
            return existing.Id;

        long? parentId = null;
        if (parentCode is not null)
        {
            parentId = await platformDb.MenuItems
                .Where(m => m.Code == parentCode)
                .Select(m => (long?)m.Id)
                .FirstOrDefaultAsync(ct);
        }

        var item = new MenuItem
        {
            Code = code,
            Label = label,
            Kind = kind,
            Icon = icon,
            Route = route,
            SortOrder = sortOrder,
            ParentId = parentId,
            RequiredPermission = requiredPermission,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        platformDb.MenuItems.Add(item);
        await platformDb.SaveChangesAsync(ct);
        logger.LogInformation("Seeded menu item: {Code}", code);
        return item.Id;
    }
}
