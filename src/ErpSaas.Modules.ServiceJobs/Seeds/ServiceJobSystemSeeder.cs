using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Sequence;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.ServiceJobs.Seeds;

public sealed class ServiceJobSystemSeeder(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    ILogger<ServiceJobSystemSeeder> logger) : IDataSeeder
{
    public int Order => 115;

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
                ("ServiceJob.View",     "View service jobs and job history"),
                ("ServiceJob.Create",   "Receive a new item for service"),
                ("ServiceJob.Diagnose", "Record diagnosis and estimate"),
                ("ServiceJob.Approve",  "Customer-approve the estimate; reject jobs"),
                ("ServiceJob.Progress", "Update job status, add parts and labor"),
                ("ServiceJob.Deliver",  "Mark job as delivered to customer"),
            };

            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "ServiceJobs", Label = label,
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
            logger.LogError(ex, "ServiceJobSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "service", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "service", Label = "Service",
                    Kind = MenuItemKind.Group, Icon = "pi pi-wrench",
                    SortOrder = 55, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
            }

            var pages = new[]
            {
                ("service.jobs",  "Service Jobs",  "pi pi-list",    "/service/jobs",  "ServiceJob.View", 10),
                ("service.new",   "New Job",       "pi pi-plus",    "/service/jobs/new", "ServiceJob.Create", 20),
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
            logger.LogError(ex, "ServiceJobSystemSeeder.SeedMenuAsync failed");
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
                    .AnyAsync(s => s.Code == Constants.SequenceCodes.ServiceJob && s.ShopId == platformShopId, ct))
            {
                tenantDb.SequenceDefinitions.Add(new SequenceDefinition
                {
                    ShopId = platformShopId,
                    Code = Constants.SequenceCodes.ServiceJob,
                    Prefix = Constants.SequencePrefixes.ServiceJob,
                    PadLength = 6, LastNumber = 0, CreatedAtUtc = DateTime.UtcNow,
                });
                logger.LogInformation("Seeding sequence: {Code}", Constants.SequenceCodes.ServiceJob);
            }
            await tenantDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "ServiceJobSystemSeeder.SeedSequencesAsync failed");
            throw;
        }
    }
}
