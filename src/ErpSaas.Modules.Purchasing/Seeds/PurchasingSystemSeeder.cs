using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Sequence;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Purchasing.Seeds;

public sealed class PurchasingSystemSeeder(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    ILogger<PurchasingSystemSeeder> logger) : IDataSeeder
{
    public int Order => 55;

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
                ("Purchasing.View",                    "View suppliers, purchase orders and bills"),
                ("Purchasing.ManageSuppliers",         "Create and edit suppliers"),
                ("Purchasing.CreatePurchaseOrder",     "Create and send purchase orders"),
                ("Purchasing.ReceiveGoods",            "Receive goods against a purchase order"),
                ("Purchasing.ManageBills",             "Create, approve and pay vendor bills"),
                ("Purchasing.ManagePurchaseReturns",   "Create and manage purchase returns and debit notes"),
            };

            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "Purchasing", Label = label,
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
            logger.LogError(ex, "PurchasingSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "purchasing", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "purchasing", Label = "Purchasing",
                    Kind = MenuItemKind.Group, Icon = "pi pi-shopping-cart",
                    SortOrder = 40, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
            }

            var pages = new[]
            {
                ("purchasing.suppliers",      "Suppliers",         "pi pi-users",        "/purchasing/suppliers",       "Purchasing.View",                   10),
                ("purchasing.orders",         "Purchase Orders",   "pi pi-file-plus",    "/purchasing/orders",          "Purchasing.CreatePurchaseOrder",    20),
                ("purchasing.receive",        "Receive Goods",     "pi pi-truck",        "/purchasing/receive",         "Purchasing.ReceiveGoods",           30),
                ("purchasing.bills",          "Bills",             "pi pi-receipt",      "/purchasing/bills",           "Purchasing.ManageBills",            40),
                ("purchasing.returns",        "Purchase Returns",  "pi pi-undo",         "/purchasing/returns",         "Purchasing.ManagePurchaseReturns",  50),
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
            logger.LogError(ex, "PurchasingSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }

    private async Task SeedSequencesAsync(CancellationToken ct)
    {
        await using var tx = await tenantDb.Database.BeginTransactionAsync(ct);
        try
        {
            const long platformShopId = 0L;
            var sequences = new[]
            {
                (Constants.SequenceCodes.PurchaseOrder,  Constants.SequencePrefixes.PurchaseOrder),
                (Constants.SequenceCodes.Bill,           Constants.SequencePrefixes.Bill),
                (Constants.SequenceCodes.PurchaseReturn, Constants.SequencePrefixes.PurchaseReturn),
                (Constants.SequenceCodes.DebitNote,      Constants.SequencePrefixes.DebitNote),
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
                    logger.LogInformation("Seeding sequence: {Code}", code);
                }
            }
            await tenantDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "PurchasingSystemSeeder.SeedSequencesAsync failed");
            throw;
        }
    }
}
