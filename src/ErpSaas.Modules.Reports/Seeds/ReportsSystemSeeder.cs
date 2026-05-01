using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Reports.Seeds;

public sealed class ReportsSystemSeeder(
    PlatformDbContext platformDb,
    ILogger<ReportsSystemSeeder> logger) : IDataSeeder
{
    public int Order => 65;

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
                ("Reports.ViewAccounting", "View accounting reports: Trial Balance, P&L, Balance Sheet, Day Book"),
                ("Reports.ViewGst",        "View GST reports: GSTR-1 B2B, HSN Summary"),
                ("Reports.ViewSales",      "View sales reports: Daily Sales, Monthly Sales, Customer-wise"),
                ("Reports.ViewInventory",  "View inventory reports: Stock Valuation, Low Stock, Stock Movement"),
                ("Reports.Export",         "Export any report to PDF, Excel or CSV"),
            };

            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "Reports", Label = label,
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
            logger.LogError(ex, "ReportsSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "reports", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "reports", Label = "Reports",
                    Kind = MenuItemKind.Group, Icon = "pi pi-chart-bar",
                    SortOrder = 70, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
            }

            var pages = new[]
            {
                ("reports.trial-balance", "Trial Balance",    "pi pi-table",      "/reports/trial-balance",  "Reports.ViewAccounting", 10),
                ("reports.profit-loss",   "Profit & Loss",    "pi pi-chart-line", "/reports/profit-loss",    "Reports.ViewAccounting", 20),
                ("reports.balance-sheet", "Balance Sheet",    "pi pi-list",       "/reports/balance-sheet",  "Reports.ViewAccounting", 30),
                ("reports.day-book",      "Day Book",         "pi pi-book",       "/reports/day-book",       "Reports.ViewAccounting", 40),
                ("reports.gstr1",         "GSTR-1 B2B",       "pi pi-file-pdf",   "/reports/gstr1",          "Reports.ViewGst",        50),
                ("reports.hsn-summary",   "HSN Summary",      "pi pi-barcode",    "/reports/hsn-summary",    "Reports.ViewGst",        60),
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
            logger.LogError(ex, "ReportsSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }
}
