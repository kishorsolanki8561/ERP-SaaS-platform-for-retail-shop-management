using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Identity.Seeds;

public sealed class UsageSystemSeeder(
    PlatformDbContext platformDb,
    ILogger<UsageSystemSeeder> logger) : IDataSeeder
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
                ("Usage.View",        "View current usage dashboard and forecasts"),
                ("Usage.ViewHistory", "View historical usage data and event audit trail"),
            };

            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "Usage", Label = label,
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
            logger.LogError(ex, "UsageSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var adminGroup = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "admin", ct);
            if (adminGroup is null)
            {
                await tx.CommitAsync(ct);
                return;
            }

            if (!await platformDb.MenuItems.AnyAsync(m => m.Code == "admin.usage", ct))
            {
                platformDb.MenuItems.Add(new MenuItem
                {
                    Code = "admin.usage", Label = "Usage & Quotas",
                    Kind = MenuItemKind.Page, Icon = "pi pi-chart-pie",
                    Route = "/admin/usage", ParentId = adminGroup.Id,
                    SortOrder = 70, RequiredPermission = "Usage.View",
                    IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                });
                logger.LogInformation("Seeded menu item: admin.usage");
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "UsageSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }
}
