using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Sequence;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Billing.Seeds;

/// <summary>
/// Seeds permissions, sequence definition, menu items, and DDL items
/// required by the Billing module.  Order = 42 so it runs after
/// IdentityDataSeeder (20), MenuDataSeeder (30), and DdlDataSeeder (10).
/// </summary>
public sealed class BillingSystemSeeder(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    ILogger<BillingSystemSeeder> logger) : IDataSeeder
{
    public int Order => 42;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedMenuAsync(ct);
        await SeedDdlAsync(ct);
        await SeedSequenceDefinitionAsync(ct);
    }

    // ── Permissions ───────────────────────────────────────────────────────────

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var perms = new[]
            {
                ("Billing.View",   "View invoices and billing records"),
                ("Billing.Create", "Create draft invoices and add lines"),
                ("Billing.Edit",   "Edit and finalize invoices"),
                ("Billing.Cancel", "Cancel invoices"),
            };

            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code,
                        Module = "Billing",
                        Label = label,
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
            logger.LogError(ex, "BillingSystemSeeder.SeedPermissionsAsync failed — rolled back");
            throw;
        }
    }

    // ── Menu items ────────────────────────────────────────────────────────────

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            // Ensure the "billing" group exists
            var group = await platformDb.MenuItems
                .FirstOrDefaultAsync(m => m.Code == "billing", ct);

            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "billing",
                    Label = "Billing",
                    Kind = MenuItemKind.Group,
                    Icon = "pi pi-file-invoice",
                    SortOrder = 40,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
                logger.LogInformation("Seeded menu group: billing");
            }

            // Invoices page
            if (!await platformDb.MenuItems.AnyAsync(m => m.Code == "billing.invoices", ct))
            {
                platformDb.MenuItems.Add(new MenuItem
                {
                    Code = "billing.invoices",
                    Label = "Invoices",
                    Kind = MenuItemKind.Page,
                    Icon = "pi pi-file-invoice-dollar",
                    Route = "/billing/invoices",
                    ParentId = group.Id,
                    SortOrder = 10,
                    RequiredPermission = "Billing.View",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                });
                logger.LogInformation("Seeded menu item: billing.invoices");
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "BillingSystemSeeder.SeedMenuAsync failed — rolled back");
            throw;
        }
    }

    // ── DDL ───────────────────────────────────────────────────────────────────

    private async Task SeedDdlAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            // INVOICE_STATUS is already partially seeded by DdlDataSeeder (Draft, Finalized,
            // Paid, Cancelled, Partially Paid). We ensure the exact set required by this module.
            const string catalogKey = Constants.DdlKeys.InvoiceStatus;
            var catalog = await platformDb.DdlCatalogs
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Key == catalogKey, ct);

            if (catalog is null)
            {
                catalog = new DdlCatalog
                {
                    Key = catalogKey,
                    Label = "Invoice Status",
                    IsActive = true,
                };
                platformDb.DdlCatalogs.Add(catalog);
                await platformDb.SaveChangesAsync(ct);
            }

            var requiredItems = new[]
            {
                ("DRAFT",      "Draft",      10),
                ("FINALIZED",  "Finalized",  20),
                ("CANCELLED",  "Cancelled",  30),
            };

            foreach (var (code, label, sort) in requiredItems)
            {
                if (!catalog.Items.Any(i => i.Code == code))
                {
                    catalog.Items.Add(new DdlItem
                    {
                        Code = code,
                        Label = label,
                        SortOrder = sort,
                        IsActive = true,
                    });
                    logger.LogInformation("Seeded DDL item: {Catalog}/{Code}", catalogKey, code);
                }
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "BillingSystemSeeder.SeedDdlAsync failed — rolled back");
            throw;
        }
    }

    // ── Sequence definition (platform default, ShopId = 0) ───────────────────

    private async Task SeedSequenceDefinitionAsync(CancellationToken ct)
    {
        await using var tx = await tenantDb.Database.BeginTransactionAsync(ct);
        try
        {
            const string seqCode = Constants.SequenceCodes.InvoiceRetail;
            const long platformShopId = 0L;

            // IgnoreQueryFilters so we can seed the ShopId=0 platform default
            // without being filtered by the tenant query filter.
            var exists = await tenantDb.SequenceDefinitions
                .IgnoreQueryFilters()
                .AnyAsync(s => s.Code == seqCode && s.ShopId == platformShopId, ct);

            if (!exists)
            {
                tenantDb.SequenceDefinitions.Add(new SequenceDefinition
                {
                    ShopId = platformShopId,
                    Code = seqCode,
                    Prefix = Constants.SequencePrefixes.InvoiceRetail,
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
            logger.LogError(ex, "BillingSystemSeeder.SeedSequenceDefinitionAsync failed — rolled back");
            throw;
        }
    }
}
