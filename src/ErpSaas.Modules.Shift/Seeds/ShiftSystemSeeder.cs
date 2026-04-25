using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Shift.Seeds;

public sealed class ShiftSystemSeeder(
    PlatformDbContext platformDb,
    ILogger<ShiftSystemSeeder> logger) : IDataSeeder
{
    public int Order => 46;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedMenuAsync(ct);
        await SeedPermissionsAsync(ct);
        await SeedDdlAsync(ct);
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "pos", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "pos",
                    Label = "Point of Sale",
                    Kind = MenuItemKind.Group,
                    Icon = "pi pi-desktop",
                    SortOrder = 15,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
                logger.LogInformation("Seeded menu group: pos");
            }

            var pages = new[]
            {
                ("pos.shifts",        "Shifts",         "pi pi-clock",         "/pos/shifts",         10, "Shift.View"),
                ("pos.open-shift",    "Open Shift",     "pi pi-play-circle",   "/pos/open-shift",     20, "Shift.Open"),
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
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var permissions = new[]
            {
                ("Shift.View",         "POS", "View Shifts"),
                ("Shift.Open",         "POS", "Open Shift"),
                ("Shift.Close",        "POS", "Close Shift"),
                ("Shift.ForceClose",   "POS", "Force Close Shift"),
                ("Shift.CashMovement", "POS", "Record Cash In / Out"),
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
                    logger.LogInformation("Seeded permission: {Code}", code);
                }
            }
            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task SeedDdlAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            const string key = Constants.DdlKeys.ShiftCashReason;
            if (!await platformDb.DdlCatalogs.AnyAsync(c => c.Key == key, ct))
            {
                var catalog = new DdlCatalog
                {
                    Key = key,
                    Label = "Shift Cash Reason",
                    IsActive = true,
                };
                platformDb.DdlCatalogs.Add(catalog);
                await platformDb.SaveChangesAsync(ct);

                var items = new[] { "Opening Float", "Cash Deposit", "Owner Withdrawal", "Petty Cash", "Other" };
                int sort = 10;
                foreach (var item in items)
                {
                    platformDb.DdlItems.Add(new DdlItem
                    {
                        CatalogId = catalog.Id,
                        Code = item.ToUpper().Replace(' ', '_'),
                        Label = item,
                        SortOrder = sort,
                        IsActive = true,
                    });
                    sort += 10;
                }
                await platformDb.SaveChangesAsync(ct);
                logger.LogInformation("Seeded DDL catalog: {Key}", key);
            }
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
