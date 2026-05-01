using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Sequence;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.SalesReturns.Seeds;

public sealed class SalesReturnsSystemSeeder(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    ILogger<SalesReturnsSystemSeeder> logger) : IDataSeeder
{
    public int Order => 60;

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
                ("SalesReturns.View",    "View sales returns and credit notes"),
                ("SalesReturns.Create",  "Create sales returns"),
                ("SalesReturns.Approve", "Approve returns and issue credit notes"),
            };

            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "SalesReturns", Label = label, CreatedAtUtc = DateTime.UtcNow,
                    });
                }
            }
            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex) { await tx.RollbackAsync(ct); logger.LogError(ex, "SalesReturnsSystemSeeder.SeedPermissionsAsync failed"); throw; }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "sales-returns", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "sales-returns", Label = "Returns",
                    Kind = MenuItemKind.Group, Icon = "pi pi-replay",
                    SortOrder = 35, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
            }

            var pages = new[]
            {
                ("returns.sales-returns",  "Sales Returns",  "pi pi-undo",         "/returns/sales-returns",  "SalesReturns.View",   10),
                ("returns.credit-notes",   "Credit Notes",   "pi pi-file-minus",   "/returns/credit-notes",   "SalesReturns.View",   20),
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
        catch (Exception ex) { await tx.RollbackAsync(ct); logger.LogError(ex, "SalesReturnsSystemSeeder.SeedMenuAsync failed"); throw; }
    }

    private async Task SeedSequencesAsync(CancellationToken ct)
    {
        await using var tx = await tenantDb.Database.BeginTransactionAsync(ct);
        try
        {
            const long platformShopId = 0L;
            var sequences = new[]
            {
                (Constants.SequenceCodes.SalesReturn, Constants.SequencePrefixes.SalesReturn),
                (Constants.SequenceCodes.CreditNote,  Constants.SequencePrefixes.CreditNote),
            };

            foreach (var (code, prefix) in sequences)
            {
                var exists = await tenantDb.SequenceDefinitions
                    .IgnoreQueryFilters()
                    .AnyAsync(s => s.Code == code && s.ShopId == platformShopId, ct);

                if (!exists)
                {
                    tenantDb.SequenceDefinitions.Add(new SequenceDefinition
                    {
                        ShopId = platformShopId, Code = code, Prefix = prefix,
                        PadLength = 6, LastNumber = 0, CreatedAtUtc = DateTime.UtcNow,
                    });
                }
            }
            await tenantDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex) { await tx.RollbackAsync(ct); logger.LogError(ex, "SalesReturnsSystemSeeder.SeedSequencesAsync failed"); throw; }
    }
}
