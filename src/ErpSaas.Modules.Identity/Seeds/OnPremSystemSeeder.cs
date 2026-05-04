using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Identity.Seeds;

public sealed class OnPremSystemSeeder(
    PlatformDbContext platformDb,
    ILogger<OnPremSystemSeeder> logger) : IDataSeeder
{
    public int Order => 130;

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
                ("OnPrem.View",   "OnPrem", "View on-prem deployments and replication logs"),
                ("OnPrem.Manage", "OnPrem", "Register, configure, and suspend on-prem deployments"),
            };

            foreach (var (code, module, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = module, Label = label,
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
            logger.LogError(ex, "OnPremSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var adminGroup = await platformDb.MenuItems
                .FirstOrDefaultAsync(m => m.Code == "admin", ct);

            var items = new[]
            {
                ("admin.onprem", "On-Prem Deployments", "pi pi-server",
                 "/admin/on-prem", "OnPrem.View", "sync.on_prem", adminGroup?.Id, 75),
            };

            foreach (var (code, label, icon, route, perm, feature, parentId, sort) in items)
            {
                if (!await platformDb.MenuItems.AnyAsync(m => m.Code == code, ct))
                {
                    platformDb.MenuItems.Add(new MenuItem
                    {
                        Code = code,
                        Label = label,
                        Icon = icon,
                        Route = route,
                        RequiredPermission = perm,
                        RequiredFeature = feature,
                        ParentId = parentId,
                        SortOrder = sort,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow,
                    });
                    logger.LogInformation("Seeding menu item: {Code}", code);
                }
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "OnPremSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }
}
