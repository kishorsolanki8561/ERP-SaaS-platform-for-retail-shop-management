using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Sequence;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Wallet.Seeds;

/// <summary>
/// Seeds permissions, menu items, DDL items, and sequence definition for the Wallet module.
/// Order = 44 — runs after IdentityDataSeeder (20), MenuDataSeeder (30), and BillingSystemSeeder (42).
/// </summary>
public sealed class WalletSystemSeeder(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    ILogger<WalletSystemSeeder> logger) : IDataSeeder
{
    public int Order => 44;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedMenuAsync(ct);
        await SeedDdlAsync(ct);
        await SeedSequenceDefinitionAsync(ct);
    }

    // ── Menu items ────────────────────────────────────────────────────────────

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems
                .FirstOrDefaultAsync(m => m.Code == "wallet", ct);

            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "wallet",
                    Label = "Wallet",
                    Kind = MenuItemKind.Group,
                    Icon = "pi pi-wallet",
                    SortOrder = 50,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
                logger.LogInformation("Seeded menu group: wallet");
            }

            var pages = new[]
            {
                ("wallet.balances",    "Customer Balances", "pi pi-chart-bar",   "/wallet/balances",    10, "Wallet.View"),
                ("wallet.transactions","Transactions",      "pi pi-list",         "/wallet/transactions",20, "Wallet.View"),
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
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "WalletSystemSeeder.SeedMenuAsync failed — rolled back");
            throw;
        }
    }

    // ── DDL ───────────────────────────────────────────────────────────────────

    private async Task SeedDdlAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            const string refTypeKey = Constants.DdlKeys.WalletReferenceType;
            var catalog = await platformDb.DdlCatalogs
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Key == refTypeKey, ct);

            if (catalog is null)
            {
                catalog = new DdlCatalog
                {
                    Key = refTypeKey,
                    Label = "Wallet Reference Type",
                    IsActive = true,
                };
                platformDb.DdlCatalogs.Add(catalog);
                await platformDb.SaveChangesAsync(ct);
            }

            var items = new[]
            {
                ("INVOICE", "Invoice",    10),
                ("MANUAL",  "Manual",     20),
                ("REFUND",  "Refund",     30),
            };

            foreach (var (code, label, sort) in items)
            {
                if (!catalog.Items.Any(i => i.Code == code))
                {
                    catalog.Items.Add(new DdlItem
                    {
                        Code = code, Label = label,
                        SortOrder = sort, IsActive = true,
                    });
                    logger.LogInformation("Seeded DDL item: {Key}/{Code}", refTypeKey, code);
                }
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "WalletSystemSeeder.SeedDdlAsync failed — rolled back");
            throw;
        }
    }

    // ── Sequence definition ───────────────────────────────────────────────────

    private async Task SeedSequenceDefinitionAsync(CancellationToken ct)
    {
        await using var tx = await tenantDb.Database.BeginTransactionAsync(ct);
        try
        {
            const string seqCode = Constants.SequenceCodes.PaymentReceipt;
            const long platformShopId = 0L;

            var exists = await tenantDb.SequenceDefinitions
                .IgnoreQueryFilters()
                .AnyAsync(s => s.Code == seqCode && s.ShopId == platformShopId, ct);

            if (!exists)
            {
                tenantDb.SequenceDefinitions.Add(new SequenceDefinition
                {
                    ShopId = platformShopId,
                    Code = seqCode,
                    Prefix = Constants.SequencePrefixes.PaymentReceipt,
                    Suffix = null,
                    PadLength = 5,
                    LastNumber = 0,
                    CreatedAtUtc = DateTime.UtcNow,
                });
                logger.LogInformation("Seeded sequence definition: {Code}", seqCode);
                await tenantDb.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "WalletSystemSeeder.SeedSequenceDefinitionAsync failed — rolled back");
            throw;
        }
    }
}
