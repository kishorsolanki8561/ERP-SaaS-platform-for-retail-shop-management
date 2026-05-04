using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Verticals.Medical.Seeds;

public sealed class MedicalSystemSeeder(
    PlatformDbContext platformDb,
    ILogger<MedicalSystemSeeder> logger) : IDataSeeder
{
    public int Order => 120;

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
                ("Medical.Batch.View",         "View drug batches and expiry data"),
                ("Medical.Batch.Manage",       "Create and update drug batch records"),
                ("Medical.Prescription.Record","Record prescription-sale against a batch"),
            };

            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "Medical", Label = label,
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
            logger.LogError(ex, "MedicalSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "medical", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "medical", Label = "Medical",
                    Kind = MenuItemKind.Group, Icon = "pi pi-heart",
                    SortOrder = 57, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                    RequiredFeature = "Verticals.MedicalBatchExpiry",
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
            }

            var pages = new[]
            {
                ("medical.batches",    "Drug Batches",     "pi pi-list",       "/medical/batches",     "Medical.Batch.View",  10),
                ("medical.expiring",   "Expiring Soon",    "pi pi-clock",      "/medical/expiring",    "Medical.Batch.View",  20),
                ("medical.prescriptions","Prescriptions",  "pi pi-file",       "/medical/prescriptions","Medical.Prescription.Record", 30),
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
                        RequiredFeature = "Verticals.MedicalBatchExpiry",
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
            logger.LogError(ex, "MedicalSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }
}
