using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Sequence;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Warranty.Seeds;

public sealed class WarrantySystemSeeder(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    ILogger<WarrantySystemSeeder> logger) : IDataSeeder
{
    public int Order => 70;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedMenuAsync(ct);
        await SeedSequencesAsync(ct);
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var perms = new[]
            {
                ("Warranty.View",         "View warranty registrations and claims"),
                ("Warranty.Manage",       "Register warranties for sold products"),
                ("Warranty.ManageClaims", "Create and resolve warranty claims"),
            };

            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "Warranty", Label = label,
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
            logger.LogError(ex, "WarrantySystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "warranty", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "warranty", Label = "Warranty",
                    Kind = MenuItemKind.Group, Icon = "pi pi-shield",
                    SortOrder = 50, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
            }

            var pages = new[]
            {
                ("warranty.registered", "Registered",   "pi pi-check-circle",  "/warranty/registered",  "Warranty.View",         10),
                ("warranty.expiring",   "Expiring Soon", "pi pi-clock",         "/warranty/expiring",    "Warranty.View",         20),
                ("warranty.claims",     "Claims",        "pi pi-exclamation-circle", "/warranty/claims", "Warranty.View",         30),
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
            logger.LogError(ex, "WarrantySystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }

    private async Task SeedSequencesAsync(CancellationToken ct)
    {
        await using var tx = await tenantDb.Database.BeginTransactionAsync(ct);
        try
        {
            const long platformShopId = 0L;
            if (!await tenantDb.SequenceDefinitions
                    .IgnoreQueryFilters()
                    .AnyAsync(s => s.Code == Constants.SequenceCodes.WarrantyClaim && s.ShopId == platformShopId, ct))
            {
                tenantDb.SequenceDefinitions.Add(new SequenceDefinition
                {
                    ShopId = platformShopId,
                    Code = Constants.SequenceCodes.WarrantyClaim,
                    Prefix = Constants.SequencePrefixes.WarrantyClaim,
                    PadLength = 6, LastNumber = 0, CreatedAtUtc = DateTime.UtcNow,
                });
                logger.LogInformation("Seeding sequence: {Code}", Constants.SequenceCodes.WarrantyClaim);
            }
            await tenantDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "WarrantySystemSeeder.SeedSequencesAsync failed");
            throw;
        }
    }
}
